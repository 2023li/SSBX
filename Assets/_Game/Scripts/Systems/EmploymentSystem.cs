using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 就业系统：回合开始分配岗位；回合结束按全局就业率影响幸福度。
    /// 规则参考文档：可达范围、不同建筑通勤容忍、距离优先>薪资优先>随机。:contentReference[oaicite:2]{index=2}
    /// </summary>
    public class EmploymentSystem : MonoBehaviour
    {
        public static EmploymentSystem Instance { get; private set; }

        [LabelText("调试日志")] public bool verbose = true;

        [BoxGroup("幸福度联动")]
        [LabelText("就业→幸福曲线")]
        public AnimationCurve employmentToHappinessDelta = new AnimationCurve(
            new Keyframe(0.00f, -8f),   // 极低就业 → 大幅-幸福
            new Keyframe(0.40f, -3f),
            new Keyframe(0.70f, 0f),   // 中等就业 → 稳定
            new Keyframe(0.85f, +2f),
            new Keyframe(0.95f, +3f)); // 很高就业 → 小幅+幸福

        [BoxGroup("幸福度联动")]
        [LabelText("用工紧张阈值")]
        [Range(0.80f, 1.0f)]
        public float overEmploymentThreshold = 0.95f;

        [BoxGroup("幸福度联动")]
        [LabelText("用工紧张提示")] public bool logTightnessWarning = true;

        [BoxGroup("只读")]
        [ReadOnly, LabelText("全局就业率(%)")] public float employmentRatePercent;
        [ReadOnly, LabelText("本回合Δ幸福")] public float deltaHappinessThisTurn;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (TurnSystem.Instance != null)
            {
                TurnSystem.Instance.OnTurnBegan += OnTurnBegan;
                TurnSystem.Instance.OnBeforeTurnEnd += OnBeforeTurnEnd;
            }
        }

        private void OnDestroy()
        {
            if (TurnSystem.Instance != null)
            {
                TurnSystem.Instance.OnTurnBegan -= OnTurnBegan;
                TurnSystem.Instance.OnBeforeTurnEnd -= OnBeforeTurnEnd;
            }
        }

        private void OnTurnBegan(int turn)
        {
            AssignAll(); // 开始回合先分配一次
        }

        private void OnBeforeTurnEnd(int turn)
        {
            // 回合结束，计算“全局就业率→Δ幸福”，并应用到国库
            float rate = ComputeGlobalEmploymentRate(); // 0..1
            employmentRatePercent = Mathf.Round(rate * 1000f) / 10f; // 小数1位
            float delta = employmentToHappinessDelta.Evaluate(rate);
            deltaHappinessThisTurn = delta;

            var ks = KingdomStats.Instance;
            ks.happiness = Mathf.Clamp(ks.happiness + Mathf.RoundToInt(delta), 0, 100);

            if (logTightnessWarning && rate >= overEmploymentThreshold)
                Debug.LogWarning("[Employment] 用工紧张：全局就业率过高，可能影响生产效率/补员。");

            if (verbose)
                Debug.Log($"[Employment] 全局就业率={employmentRatePercent:0.0}% → Δ幸福={delta:+0;-0;0}");
        }

        [Button("分配所有就业")]
        public void AssignAll()
        {
            var houses = GameObject.FindObjectsOfType<HouseBuilding>();
            var providers = GameObject.FindObjectsOfType<JobProvider>();

            foreach (var p in providers) p.occupied = 0;
            foreach (var h in houses) { h.employed = 0; h.unemployed = 0; }

            foreach (var h in houses)
                AssignForHouse(h, providers);
        }

        [Button("仅为选中居民房分配")]
        public void AssignSelected()
        {
            var h = GameObject.FindObjectOfType<HouseBuilding>();
            if (h != null) AssignForHouse(h, GameObject.FindObjectsOfType<JobProvider>());
        }

        public void AssignForHouse(HouseBuilding house, JobProvider[] allProviders = null)
        {
            if (house == null || !house.isConstructed) return;
            int employable = Mathf.FloorToInt(house.curPopulation * house.employmentPercent * 0.01f);
            if (employable <= 0) return;

            if (allProviders == null) allProviders = GameObject.FindObjectsOfType<JobProvider>();
            var grid = GridSystem.Instance;

            // 可达集合（使用“就业通勤移动力”）
            var reachable = Pathfinder.ComputeReachable(grid, house.originCell, house.employmentMovePoints);

            // 候选 Provider：完工 + 有空位 + 到其邻接可通行格的接近代价(L)在双向容忍内
            var candidates = new List<(JobProvider p, float cost)>();
            foreach (var p in allProviders)
            {
                var b = p.GetComponent<Building>();
                if (b == null || !b.isConstructed || p.FreeSlots <= 0) continue;

                if (TryComputeApproachCostToBuilding(b, reachable, out float L))
                {
                    if (L <= house.employmentMovePoints && L <= p.commuteMaxCost)
                        candidates.Add((p, L));
                }
            }

            // 排序：距离近(L小) > priority高 > 随机
            var rnd = new System.Random();
            var sorted = candidates.OrderBy(c => c.cost)
                                   .ThenByDescending(c => c.p.priority)
                                   .ThenBy(_ => rnd.Next()).ToList();

            int remain = employable;
            foreach (var (p, L) in sorted)
            {
                if (remain <= 0) break;
                int take = Mathf.Min(p.FreeSlots, remain);
                p.occupied += take;
                house.employed += take;
                remain -= take;

                if (verbose)
                    Debug.Log($"[Employment] {house.name} → {p.name} 分配 {take} 人 (L={L:0.##}, 优先级={p.priority})");
            }

            house.unemployed = Mathf.Max(0, employable - house.employed);
            if (verbose)
                Debug.Log($"[Employment] {house.name}：可就业={employable} 已就业={house.employed} 失业={house.unemployed}");
        }

        public float ComputeGlobalEmploymentRate()
        {
            var houses = GameObject.FindObjectsOfType<HouseBuilding>();
            int totalEmployable = 0, totalEmployed = 0;

            foreach (var h in houses)
            {
                if (!h.isConstructed) continue;
                int employable = Mathf.FloorToInt(h.curPopulation * h.employmentPercent * 0.01f);
                totalEmployable += employable;
                totalEmployed += Mathf.Min(employable, h.employed);
            }
            if (totalEmployable <= 0) return 0f;
            return Mathf.Clamp01(totalEmployed / (float)totalEmployable);
        }

        private bool TryComputeApproachCostToBuilding(Building b, Dictionary<Vector3Int, float> reachable, out float bestCost)
        {
            bestCost = float.MaxValue;
            var grid = GridSystem.Instance;
            foreach (var oc in b.OccupiedCells)
            {
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector3Int(oc.x + dx, oc.y + dy, 0);
                        if (!grid.IsInside(n) || grid.IsBlocked(n)) continue;
                        if (reachable.TryGetValue(n, out float c) && c < bestCost) bestCost = c;
                    }
            }
            return bestCost < float.MaxValue;
        }
    }
}
