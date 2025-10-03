using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 地块影响系统（新版）：医疗/治安按“最高值”，美化仍累加。
    /// 支持多环配置：每个环给定(半径, 等级)，覆盖即在该半径内生效。
    /// </summary>
    public class AreaEffectSystem : MonoBehaviour
    {
        public static AreaEffectSystem Instance { get; private set; }

        [LabelText("调试日志")] public bool verbose;

        // 每个格的“来源计数” -> 用于快速得到“当前最高等级”
        // levels数组：索引1..3使用；值=有多少来源在此格提供该等级。
        private class CellAgg
        {
            public int[] med = new int[4]; // 医疗等级计数
            public int[] sec = new int[4]; // 治安等级计数
            public int beautify;           // 美化累计（仍叠加）
        }
        private readonly Dictionary<Vector3Int, CellAgg> _agg = new Dictionary<Vector3Int, CellAgg>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameEvents.OnBuildingConstructed += OnBuildingConstructed;
            GameEvents.OnBuildingDestroyed += OnBuildingDestroyed;
        }

        private void OnDestroy()
        {
            GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
            GameEvents.OnBuildingDestroyed -= OnBuildingDestroyed;
        }

        private void OnBuildingConstructed(Building b)
        {
            if (b == null || b.config == null) return;
            ApplyBuilding(b, +1);
        }

        private void OnBuildingDestroyed(Building b)
        {
            if (b == null || b.config == null) return;
            ApplyBuilding(b, -1);
        }

        /// <summary>核心：对某建筑的影响做+1或-1（构建/拆除）。</summary>
        private void ApplyBuilding(Building b, int sign)
        {
            var cfg = b.config;
            var grid = GridSystem.Instance;

            // 1) 医疗/治安：多环 → 最高值（用计数法维护）
            var medRings = (cfg.medicalRings != null && cfg.medicalRings.Count > 0)
                          ? cfg.medicalRings
                          : (cfg.medicalLevel > 0 && cfg.serviceRadius > 0)
                                ? new List<LevelRing> { new LevelRing { radius = cfg.serviceRadius, level = cfg.medicalLevel } }
                                : null;

            var secRings = (cfg.securityRings != null && cfg.securityRings.Count > 0)
                          ? cfg.securityRings
                          : (cfg.securityBonus > 0 && cfg.serviceRadius > 0)
                                ? new List<LevelRing> { new LevelRing { radius = cfg.serviceRadius, level = cfg.securityBonus } }
                                : null;

            if (medRings != null)
                foreach (var ring in medRings)
                    if (ring.level > 0 && ring.radius > 0)
                        ApplyRing(b.originCell, ring.radius, (cellAgg) => cellAgg.med[ring.level] += sign);

            if (secRings != null)
                foreach (var ring in secRings)
                    if (ring.level > 0 && ring.radius > 0)
                        ApplyRing(b.originCell, ring.radius, (cellAgg) => cellAgg.sec[ring.level] += sign);

            // 2) 美化：叠加
            if (cfg.beautifyBonus != 0)
            {
                int r = Mathf.Max(cfg.serviceRadius, 0);
                if (r > 0)
                {
                    ApplyRing(b.originCell, r, (cellAgg) => cellAgg.beautify += sign * cfg.beautifyBonus);
                }
            }

            // 3) 将聚合结果回写到 GridSystem（有效医疗/治安=最高等级；美化=累加）
            //    只需更新“受影响区域”，为简单起见：用最大可能半径一次更新（性能足够）。
            int maxRadius = GetMaxRadius(medRings, secRings, cfg.serviceRadius);
            UpdateGridEffectiveValuesAround(b.originCell, maxRadius);
        }

        private int GetMaxRadius(List<LevelRing> med, List<LevelRing> sec, int fallback)
        {
            int m = 0;
            if (med != null) foreach (var r in med) m = Mathf.Max(m, r.radius);
            if (sec != null) foreach (var r in sec) m = Mathf.Max(m, r.radius);
            m = Mathf.Max(m, fallback);
            return m;
        }

        /// <summary>在以center为圆心、Chebyshev半径r内，对每个格执行accumulator。</summary>
        private void ApplyRing(Vector3Int center, int r, System.Action<CellAgg> accumulator)
        {
            var grid = GridSystem.Instance;
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > r) continue;
                    var c = new Vector3Int(center.x + dx, center.y + dy, 0);
                    if (!grid.IsInside(c)) continue;

                    if (!_agg.TryGetValue(c, out var agg)) { agg = new CellAgg(); _agg[c] = agg; }
                    accumulator(agg);
                }
        }

        /// <summary>把 _agg 的计数结果写回 GridSystem 的有效值。</summary>
        private void UpdateGridEffectiveValuesAround(Vector3Int center, int r)
        {
            var grid = GridSystem.Instance;
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > r) continue;
                    var c = new Vector3Int(center.x + dx, center.y + dy, 0);
                    if (!grid.IsInside(c)) continue;

                    if (_agg.TryGetValue(c, out var agg))
                    {
                        short med = HighestNonZero(agg.med);
                        short sec = HighestNonZero(agg.sec);
                        grid.SetMedical(c, med);
                        grid.SetSecurity(c, sec);
                        grid.SetBeautify(c, (short)Mathf.Clamp(agg.beautify, short.MinValue, short.MaxValue));
                    }
                    else
                    {
                        // 无影响来源则清零
                        grid.SetMedical(c, 0);
                        grid.SetSecurity(c, 0);
                        // 美化不清零：只有在所有来源撤销后 _agg 才会没有该格
                        grid.SetBeautify(c, 0);
                    }
                }
        }

        private short HighestNonZero(int[] arr)
        {
            for (int lv = 3; lv >= 1; lv--) if (arr[lv] > 0) return (short)lv;
            return 0;
        }

        // —— 调试按钮 ——
        [Button("重算全图(保险)")]
        private void RebuildAll()
        {
            _agg.Clear();
            var grid = GridSystem.Instance;

            // 清空Grid上的视图值
            for (int x = grid.minCell.x; x <= grid.maxCell.x; x++)
                for (int y = grid.minCell.y; y <= grid.maxCell.y; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (!grid.IsInside(c)) continue;
                    grid.SetMedical(c, 0);
                    grid.SetSecurity(c, 0);
                    grid.SetBeautify(c, 0);
                }

            // 重新应用所有已完工建筑
            foreach (var b in GameObject.FindObjectsOfType<Building>())
                if (b.isConstructed) ApplyBuilding(b, +1);

            Debug.Log("[AreaEffect] 全图重算完成");
        }

        [Button("打印格影响(输入坐标)")]
        private void Debug_PrintCell(Vector3Int c)
        {
            var g = GridSystem.Instance;
            Debug.Log($"格{c}: 医疗={g.GetMedical(c)} 治安={g.GetSecurity(c)} 美化={g.GetBeautify(c)}");
        }
    }
}
