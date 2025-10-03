using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    public enum FoodSourceMode { GranaryOnly, GranaryOrWarehouse }

    /// <summary>
    /// 居民房：Lv1~Lv4容量规则、取料、就业统计。诊断日志可详解“为何达不到容量/供给不满足”。:contentReference[oaicite:3]{index=3}
    /// </summary>
    public class HouseBuilding : Building
    {
        [BoxGroup("人口参数")]
        [LabelText("等级")] public int level = 1;
        [BoxGroup("人口参数")]
        [LabelText("最大人口")] public int maxPopulation = 8;
        [BoxGroup("人口参数"), ReadOnly]
        [LabelText("当前人口")] public int curPopulation = 0;
        [BoxGroup("人口参数")]
        [LabelText("填充/回合")] public int fillPerTurn = 2;
        [BoxGroup("人口参数")]
        [LabelText("流失/回合")] public int drainPerTurn = 1;
        [BoxGroup("人口参数")]
        [LabelText("人均食耗(一级)")] public float foodPerCapita = 0.5f;

        [BoxGroup("幸福阈值")]
        [LabelText("Lv1阈%"), Range(0, 100)] public int happyLv1 = 20;
        [BoxGroup("幸福阈值")]
        [LabelText("Lv2阈%"), Range(0, 100)] public int happyLv2 = 40;
        [BoxGroup("幸福阈值")]
        [LabelText("Lv3阈%"), Range(0, 100)] public int happyLv3 = 60;
        [BoxGroup("幸福阈值")]
        [LabelText("Lv4阈%"), Range(0, 100)] public int happyLv4 = 80;

        [BoxGroup("取料")]
        [LabelText("取料移动力"), Range(0f, 50f)] public float fetchMovePoints = 3f;
        [BoxGroup("取料")]
        [LabelText("来源模式")] public FoodSourceMode foodSourceMode = FoodSourceMode.GranaryOnly;

        [BoxGroup("就业")]
        [LabelText("就业通勤移动力"), Min(0f)] public float employmentMovePoints = 12f;
        [BoxGroup("就业")]
        [LabelText("劳动年龄比例%"), Range(0, 100)] public float employmentPercent = 70f;
        [BoxGroup("就业"), ReadOnly]
        [LabelText("已就业人口")] public int employed = 0;
        [BoxGroup("就业"), ReadOnly]
        [LabelText("失业人口")] public int unemployed = 0;

        [BoxGroup("奢侈品")]
        [LabelText("奢侈候选资源")]
        public List<ResourceType> luxuryCandidates = new List<ResourceType>
        {
            ResourceType.Wine, ResourceType.Tea, ResourceType.Honey, ResourceType.Pastry,
            ResourceType.Clothes, ResourceType.Furniture, ResourceType.Gems
        };
        [BoxGroup("奢侈品")]
        [LabelText("Lv4所需奢侈种类数"), Min(0)] public int luxKindsNeededLv4 = 2;

        [BoxGroup("调试")]
        [LabelText("详细日志")] public bool verboseLog = true;

        private static readonly ResourceType[] Tier1Foods = { ResourceType.Barley, ResourceType.Rice, ResourceType.Corn };
        private static readonly ResourceType[] Tier2Foods = { ResourceType.Chicken, ResourceType.Duck, ResourceType.Fish, ResourceType.Mutton, ResourceType.Milk, ResourceType.Bread };

        protected override void OnBeforeTurnEnd(int turn)
        {
            base.OnBeforeTurnEnd(turn);
            if (!isConstructed) return;

            // 1) 容量人口
            int capacity = EvaluateCapacity();

            // 2) 人口涨落
            int allowed = Mathf.Min(maxPopulation, capacity);
            if (curPopulation < allowed) curPopulation = Mathf.Min(allowed, curPopulation + fillPerTurn);
            else if (curPopulation > allowed) curPopulation = Mathf.Max(0, curPopulation - drainPerTurn);

            // 3) 一级食物消耗（Lv2/3/4暂用同一参数，Inspector可改）
            int need = Mathf.CeilToInt(curPopulation * foodPerCapita);
            if (need > 0 && TryFindFoodProviderWithAnyTier1(out var provider, out var chosenFood))
            {
                int remain = need;
                if (chosenFood.HasValue)
                {
                    var t = chosenFood.Value;
                    int have = provider.Get(t);
                    int take = Mathf.Min(have, remain);
                    if (take > 0) { provider.TryProvide(t, take); remain -= take; }
                }
                for (int i = 0; i < Tier1Foods.Length && remain > 0; i++)
                {
                    var t = Tier1Foods[i];
                    int have = provider.Get(t);
                    int take = Mathf.Min(have, remain);
                    if (take > 0) { provider.TryProvide(t, take); remain -= take; }
                }
                if (verboseLog) Debug.Log($"[House] 食耗：需={need} 还差={remain}");
            }
        }

        private int EvaluateCapacity()
        {
            int capacity = 0;
            var grid = GridSystem.Instance;
            int med = grid.GetMedical(originCell);
            int beaut = grid.GetBeautify(originCell);
            int happy = KingdomStats.Instance.happiness;

            CountAvailableFoodsInRange(out int c1, out int c2);
            int luxKinds = CountAvailableKindsInRange(luxuryCandidates);

            if (level == 1)
            {
                if (c1 >= 1) capacity += 6;
                if (happy > happyLv1) capacity += 2;
            }
            else if (level == 2)
            {
                if (c1 >= 2) capacity += 8;
                if (c2 >= 2) capacity += 4;
                if (med >= 1) capacity += 3;
                if (happy > happyLv2) capacity += 2;
            }
            else if (level == 3)
            {
                if (c2 >= 3) capacity += 10;
                if (beaut > 3) capacity += 3;
                if (med >= 2) capacity += 3;
                if (happy > happyLv3) capacity += 4;
            }
            else if (level == 4)
            {
                if (c1 >= 1) capacity += 10;                   // 文档：至少1种一级 +10
                if (luxKinds >= Mathf.Max(0, luxKindsNeededLv4)) capacity += 5; // ≥2种奢侈品 +5
                if (med >= 3) capacity += 3;                   // 医疗≥3 +3
                if (happy > happyLv4) capacity += 5;           // 幸福>80% +5
            }
            return capacity;
        }

        private void CountAvailableFoodsInRange(out int countTier1, out int countTier2)
        {
            countTier1 = 0; countTier2 = 0;
            var set1 = new HashSet<ResourceType>();
            var set2 = new HashSet<ResourceType>();

            float mp = fetchMovePoints > 0 ? fetchMovePoints : (config != null ? config.movePointsToFetch : 3f);
            var reachable = Pathfinder.ComputeReachable(GridSystem.Instance, originCell, mp);

            var granaries = GameObject.FindObjectsOfType<GranaryBuilding>();
            WarehouseBuilding[] warehouses = null;
            if (foodSourceMode == FoodSourceMode.GranaryOrWarehouse)
                warehouses = GameObject.FindObjectsOfType<WarehouseBuilding>();

            void Acc(IFoodProvider p)
            {
                if (!p.IsConstructed) return;
                if (!TryComputeApproachCostToProvider(p, reachable, out _)) return;
                foreach (var t in Tier1Foods) if (p.Get(t) > 0) set1.Add(t);
                foreach (var t in Tier2Foods) if (p.Get(t) > 0) set2.Add(t);
            }
            foreach (var g in granaries) Acc(g);
            if (warehouses != null) foreach (var w in warehouses) Acc(w);

            countTier1 = set1.Count; countTier2 = set2.Count;
        }

        private int CountAvailableKindsInRange(List<ResourceType> kinds)
        {
            if (kinds == null || kinds.Count == 0) return 0;

            var set = new HashSet<ResourceType>();
            float mp = fetchMovePoints > 0 ? fetchMovePoints : (config != null ? config.movePointsToFetch : 3f);
            var reachable = Pathfinder.ComputeReachable(GridSystem.Instance, originCell, mp);

            var granaries = GameObject.FindObjectsOfType<GranaryBuilding>();
            WarehouseBuilding[] warehouses = null;
            if (foodSourceMode == FoodSourceMode.GranaryOrWarehouse)
                warehouses = GameObject.FindObjectsOfType<WarehouseBuilding>();

            void Acc(IFoodProvider p)
            {
                if (!p.IsConstructed) return;
                if (!TryComputeApproachCostToProvider(p, reachable, out _)) return;
                foreach (var t in kinds) if (p.Get(t) > 0) set.Add(t);
            }
            foreach (var g in granaries) Acc(g);
            if (warehouses != null) foreach (var w in warehouses) Acc(w);

            return set.Count;
        }

        private bool TryFindFoodProviderWithAnyTier1(out IFoodProvider provider, out ResourceType? chosen)
        {
            provider = null; chosen = null;
            float mp = fetchMovePoints > 0 ? fetchMovePoints : (config != null ? config.movePointsToFetch : 3f);
            var reachable = Pathfinder.ComputeReachable(GridSystem.Instance, originCell, mp);

            float bestCost = float.MaxValue;
            var granaries = GameObject.FindObjectsOfType<GranaryBuilding>();
            var warehouses = foodSourceMode == FoodSourceMode.GranaryOrWarehouse
                                ? GameObject.FindObjectsOfType<WarehouseBuilding>() : null;

            foreach (var g in granaries) EvaluateProvider(g, ref provider, ref chosen, ref bestCost, reachable);
            if (warehouses != null) foreach (var w in warehouses) EvaluateProvider(w, ref provider, ref chosen, ref bestCost, reachable);

            if (provider == null && verboseLog) Debug.Log("[House] 没找到提供者（检查移动力/邻接通行/库存）");
            return provider != null;
        }

        private void EvaluateProvider(IFoodProvider p, ref IFoodProvider best, ref ResourceType? chosen, ref float bestCost,
                                      Dictionary<Vector3Int, float> reachable)
        {
            if (!p.IsConstructed) return;
            if (TryComputeApproachCostToProvider(p, reachable, out float L))
            {
                foreach (var f in Tier1Foods)
                {
                    if (p.Get(f) > 0 && L < bestCost)
                    {
                        best = p; chosen = f; bestCost = L;
                    }
                }
            }
        }

        private bool TryComputeApproachCostToProvider(IFoodProvider provider,
            Dictionary<Vector3Int, float> reachable, out float bestCost)
        {
            bestCost = float.MaxValue;
            var pb = provider as Building;
            var grid = GridSystem.Instance;
            foreach (var oc in pb.OccupiedCells)
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

        // —— 调试按钮 ——
        [Button("诊断：统计食物/奢侈可用种类")]
        private void Debug_CountKinds()
        {
            CountAvailableFoodsInRange(out int c1, out int c2);
            int lux = CountAvailableKindsInRange(luxuryCandidates);
            Debug.Log($"[House] 可达：一级={c1} 二级={c2} 奢侈={lux}");
        }
    }
}
