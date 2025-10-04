using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 地块影响系统（新版）：
    /// - 医疗/治安/美化都采用“取最高等级”，不再叠加数值
    /// - 支持多环配置：每个环 (radius, level)，覆盖即在该半径内生效
    /// - 半径范围采用“菱形(Manhattan)距离”：|dx|+|dy| <= r
    /// </summary>
    public class AreaEffectSystem : MonoBehaviour
    {
        public static AreaEffectSystem Instance { get; private set; }

        [LabelText("调试日志")] public bool verbose = false;

        /// <summary>可根据游戏需要调整最大等级上限（数组大小为 MAX_LEVEL+1，索引1..MAX_LEVEL有效）</summary>
        private const int MAX_LEVEL = 5;

        /// <summary>每个格子的来源计数，用于快速求“当前最高等级”</summary>
        [System.Serializable]
        private class CellAgg
        {
            public int[] med = new int[MAX_LEVEL + 1];
            public int[] sec = new int[MAX_LEVEL + 1];
            public int[] beauty = new int[MAX_LEVEL + 1];
        }

        // 聚合表：每个格子 -> 计数结构
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

        /// <summary>
        /// 核心：对某建筑的影响做 +1 或 -1（建造/拆除）。
        /// 仅基于 config 的三组“环”列表；不再使用任何旧字段兜底。
        /// </summary>
        private void ApplyBuilding(Building b, int sign)
        {
            var cfg = b.config;

            var medRings = (cfg.medicalRings != null && cfg.medicalRings.Count > 0) ? cfg.medicalRings : null;
            var secRings = (cfg.securityRings != null && cfg.securityRings.Count > 0) ? cfg.securityRings : null;
            var beaRings = (cfg.beautifyRings != null && cfg.beautifyRings.Count > 0) ? cfg.beautifyRings : null;

            // 1) 医疗：计数法 → 回写时取最高
            if (medRings != null)
            {
                foreach (var ring in medRings)
                {
                    if (ring.level <= 0 || ring.radius <= 0) continue;
                    FillDiamond(b.originCell, ring.radius, (cellAgg) =>
                    {
                        SafeInc(cellAgg.med, ring.level, sign);
                    });
                }
            }

            // 2) 治安：计数法 → 回写时取最高
            if (secRings != null)
            {
                foreach (var ring in secRings)
                {
                    if (ring.level <= 0 || ring.radius <= 0) continue;
                    FillDiamond(b.originCell, ring.radius, (cellAgg) =>
                    {
                        SafeInc(cellAgg.sec, ring.level, sign);
                    });
                }
            }

            // 3) 美化：现在也“取最高”（计数法），不再叠加
            if (beaRings != null)
            {
                foreach (var ring in beaRings)
                {
                    if (ring.level <= 0 || ring.radius <= 0) continue;
                    FillDiamond(b.originCell, ring.radius, (cellAgg) =>
                    {
                        SafeInc(cellAgg.beauty, ring.level, sign);
                    });
                }
            }

            // 4) 回写：用三类环的最大半径覆盖一次（性能足够）
            int maxRadius = GetMaxRadius(medRings, secRings, beaRings);
            if (maxRadius > 0)
                UpdateGridEffectiveValuesAround(b.originCell, maxRadius);
        }

        /// <summary>数组防越界 + 增减计数</summary>
        private static void SafeInc(int[] arr, int level, int delta)
        {
            if (arr == null) return;
            if (level <= 0) return;
            if (level >= arr.Length) level = arr.Length - 1; // clamp
            arr[level] += delta;
            if (arr[level] < 0) arr[level] = 0; // 防止出现负计数
        }

        /// <summary>三组环，取最大半径</summary>
        private static int GetMaxRadius(List<LevelRing> a, List<LevelRing> b, List<LevelRing> c)
        {
            int r = 0;
            if (a != null) foreach (var x in a) if (x.radius > r) r = x.radius;
            if (b != null) foreach (var x in b) if (x.radius > r) r = x.radius;
            if (c != null) foreach (var x in c) if (x.radius > r) r = x.radius;
            return r;
        }

        /// <summary>
        /// 在以 center 为中心、半径 r 的“菱形(Manhattan)”范围内，获取/创建 CellAgg 并执行累积。
        /// 约束：仅对网格内（GridSystem.IsInside）的格处理。
        /// </summary>
        private void FillDiamond(Vector3Int center, int r, System.Action<CellAgg> accumulate)
        {
            var grid = GridSystem.Instance;
            for (int dx = -r; dx <= r; dx++)
            {
                int remain = r - Mathf.Abs(dx);
                for (int dy = -remain; dy <= remain; dy++)
                {
                    var c = new Vector3Int(center.x + dx, center.y + dy, 0);
                    if (!grid.IsInside(c)) continue;

                    if (!_agg.TryGetValue(c, out var agg)) { agg = new CellAgg(); _agg[c] = agg; }
                    accumulate?.Invoke(agg);
                }
            }
        }

        /// <summary>
        /// 把 _agg 的计数结果写回 GridSystem 的有效值（医疗/治安/美化取最高等级）。
        /// 仅对“受影响范围”进行回写。
        /// </summary>
        private void UpdateGridEffectiveValuesAround(Vector3Int center, int radius)
        {
            var grid = GridSystem.Instance;
            if (radius <= 0) return;

            for (int dx = -radius; dx <= radius; dx++)
            {
                int remain = radius - Mathf.Abs(dx);
                for (int dy = -remain; dy <= remain; dy++)
                {
                    var cell = new Vector3Int(center.x + dx, center.y + dy, 0);
                    if (!grid.IsInside(cell)) continue;

                    var agg = TryGetAgg(cell);

                    int medLevel = HighestPositiveLevel(agg?.med);
                    int secLevel = HighestPositiveLevel(agg?.sec);
                    int beaLevel = HighestPositiveLevel(agg?.beauty);

                    // === 回写到你的网格数据（按你的接口命名，这里用 SetMedical/SetSecurity/SetBeautify）===
                    grid.SetMedical(cell, (short)medLevel);
                    grid.SetSecurity(cell, (short)secLevel);
                    grid.SetBeautify(cell, (short)beaLevel);

                    if (verbose && (dx == 0 && dy == 0))
                        Debug.Log($"[AreaEffect] 更新 {cell}: 医{medLevel} 治{secLevel} 美{beaLevel}");
                }
            }
        }

        /// <summary>最高的“计数>0”的等级（1..MAX_LEVEL）；不存在则返回0。</summary>
        private static int HighestPositiveLevel(int[] counter)
        {
            if (counter == null) return 0;
            for (int lv = counter.Length - 1; lv >= 1; lv--)
                if (counter[lv] > 0) return lv;
            return 0;
        }

        /// <summary>读取某格的聚合对象；不存在返回 null。</summary>
        private CellAgg TryGetAgg(Vector3Int cell)
        {
            _agg.TryGetValue(cell, out var agg);
            return agg;
        }

        // ================= 调试工具 =================

        [Button("重算全图(保险)")]
        public void RebuildAll()
        {
            _agg.Clear();
            var grid = GridSystem.Instance;

            // 1) 清空网格上的有效值
            for (int x = grid.minCell.x; x <= grid.maxCell.x; x++)
            {
                for (int y = grid.minCell.y; y <= grid.maxCell.y; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (!grid.IsInside(c)) continue;
                    grid.SetMedical(c, 0);
                    grid.SetSecurity(c, 0);
                    grid.SetBeautify(c, 0);
                }
            }

            // 2) 重新应用所有“已完工”建筑
            foreach (var b in GameObject.FindObjectsOfType<Building>())
            {
                if (b != null && b.isConstructed && b.config != null)
                    ApplyBuilding(b, +1);
            }

        }
    }
}
