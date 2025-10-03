using System;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SSBX
{
    [Serializable]
    public class Route
    {
        public WarehouseBuilding source;
        public WarehouseBuilding target;

        [Tooltip("本路线允许运输的资源（留空=允许所有资源）。")]
        public List<ResourceType> whitelist = new List<ResourceType>();

#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public float lastComputedCostL = -1f;

#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public int lastThroughputQ = 0;
    }

    /// <summary>
    /// 运输系统：按路径代价计算Q与维护费M，并在回合结束时执行转运。
    /// </summary>
    public class TransportRouteSystem : MonoBehaviour
    {
        public static TransportRouteSystem Instance { get; private set; }

        [Header("路线列表（仓库↔仓库）")]
        public List<Route> routes = new List<Route>();

        [Header("数值（默认公式参数）")]
        public int[] baseThroughputByLevel = { 12, 20, 32, 48, 64 }; // Lv1..Lv5
        public int[] routeSlotsByLevel = { 1, 2, 3, 4, 5 };
        public float throughputDivisorOffset = 1f; // Q = floor(min(baseA,baseB) / (offset + L))
        public float maintenanceFactor = 0.5f;     // M = ceil(maintenanceFactor * L)
        public int minQPerTurn = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (TurnSystem.Instance != null)
                TurnSystem.Instance.OnBeforeTurnEnd += OnBeforeTurnEnd;
        }

        private void OnDestroy()
        {
            if (TurnSystem.Instance != null)
                TurnSystem.Instance.OnBeforeTurnEnd -= OnBeforeTurnEnd;
        }

        private void OnBeforeTurnEnd(int turn)
        {
            foreach (var r in routes)
            {
                if (r.source == null || r.target == null) continue;
                if (!r.source.isConstructed || !r.target.isConstructed) continue;

                // 1) 路径代价 L（从源origin到目标origin）
                float L;
                if (!TryComputeWarehouseToWarehouseCost(r.source, r.target, out L))
                    continue;

                r.lastComputedCostL = L;

                // 2) 计算吞吐上限（考虑仓库等级）
                int baseA = GetBaseThroughput(r.source);
                int baseB = GetBaseThroughput(r.target);
                int Q = Mathf.FloorToInt(Mathf.Min(baseA, baseB) / (throughputDivisorOffset + Mathf.Max(0f, L)));
                Q = Mathf.Max(0, Q);

                // 3) 计算维护费
                int M = Mathf.CeilToInt(maintenanceFactor * Mathf.Max(0f, L));
                if (M > 0) KingdomStats.Instance.SpendGold(M);

                // 4) 实际转运：按白名单或全部资源类型，按库存比例搬运
                int moved = MoveResources(r, Q);
                r.lastThroughputQ = moved;
            }
        }

        private int GetBaseThroughput(WarehouseBuilding wh)
        {
            int lv = Mathf.Clamp(wh.config.level, 1, 5); // 你可在BuildingConfig中添加 level 字段；此处默认存在
            return baseThroughputByLevel[Mathf.Clamp(lv - 1, 0, baseThroughputByLevel.Length - 1)];
        }

        //private bool TryComputeShortestCost(Vector3Int start, Vector3Int end, out float L)
        //{
        //    L = -1f;
        //    var grid = GridSystem.Instance;
        //    var dic = Pathfinder.ComputeReachable(grid, start, 99999f); // 足够大，近似全图
        //    if (dic.TryGetValue(end, out float c)) { L = c; return true; }
        //    return false;
        //}


        private bool TryComputeShortestCost(Vector3Int start, Vector3Int end, out float L)
        {
            L = -1f;

            // 调试：检查输入坐标
            Debug.Log($"计算路径: {start} -> {end}");

            var grid = GridSystem.Instance;
            if (grid == null)
            {
                Debug.LogError("GridSystem.Instance 为 null!");
                return false;
            }

            var dic = Pathfinder.ComputeReachable(grid, start, 99999f);

            // 调试：查看可达区域
            Debug.Log($"可达单元格数量: {dic.Count}");
            foreach (var kvp in dic)
            {
                if (kvp.Key == end)
                {
                    Debug.Log($"找到路径! 成本: {kvp.Value}");
                    L = kvp.Value;
                    return true;
                }
            }

            Debug.LogWarning($"无法从 {start} 到达 {end}");
            return false;
        }

        private bool TryComputeWarehouseToWarehouseCost(WarehouseBuilding src, WarehouseBuilding dst, out float L)
        {
            L = -1f;
            if (src == null || dst == null) return false;

            var grid = GridSystem.Instance;

            // 收集“源/目标”的相邻可通行格（8方向，去重）
            var srcAdj = CollectApproachNeighbors(src);
            var dstAdj = CollectApproachNeighbors(dst);
            if (srcAdj.Count == 0 || dstAdj.Count == 0) return false;

            float best = float.MaxValue;

            // 朴素法：从每个源邻接格跑一次可达，取到任意目标邻接格的最小代价
            foreach (var s in srcAdj)
            {
                var reachable = Pathfinder.ComputeReachable(grid, s, 999999f); // 近似无限
                foreach (var t in dstAdj)
                {
                    if (reachable.TryGetValue(t, out float c) && c < best)
                        best = c;
                }
                // 小优化：如果出现0代价（同格），可提前结束
                if (best <= 0f) break;
            }

            if (best < float.MaxValue) { L = best; return true; }
            return false;
        }

        // 收集“建筑占用格”的8邻接中所有可通行格（不含被占用/障碍/越界）
        private HashSet<Vector3Int> CollectApproachNeighbors(Building b)
        {
            var set = new HashSet<Vector3Int>();
            var grid = GridSystem.Instance;
            foreach (var oc in b.OccupiedCells)
            {
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var n = new Vector3Int(oc.x + dx, oc.y + dy, 0);
                        if (!grid.IsInside(n)) continue;
                        if (grid.IsBlocked(n)) continue; // 建筑/障碍占用格不可通行
                                                         // 有路/无路皆可通行，代价由 Pathfinder/StepCost 决定（路0.5，无路1）
                        set.Add(n);
                    }
            }
            return set;
        }






        /// <summary>把Q单位的资源从source搬到target，返回实际搬运数量。</summary>
        private int MoveResources(Route r, int Q)
        {
            if (Q <= 0) return 0;

            // 选择资源池（白名单或全部）
            var types = r.whitelist != null && r.whitelist.Count > 0
                        ? r.whitelist
                        : new List<ResourceType>((ResourceType[])Enum.GetValues(typeof(ResourceType)));

            // 简化：轮询类型搬运直到Q耗尽
            int moved = 0;
            for (int pass = 0; pass < 2 && moved < Q; pass++) // 2圈防止单一资源断档
            {
                foreach (var t in types)
                {
                    if (moved >= Q) break;
                    int have = r.source.inventory.Get(t);
                    if (have <= 0) continue;

                    int take = Mathf.Min(have, Q - moved);
                    if (take > 0)
                    {
                        r.source.inventory.TryConsume(t, take);
                        r.target.inventory.Add(t, take);
                        moved += take;
                    }
                }
            }
            return moved;
        }


        public (float L, int Q, int M) PredictStats(WarehouseBuilding a, WarehouseBuilding b)
        {
            if (a == null || b == null) return (-1, 0, 0);
            if (!TryComputeWarehouseToWarehouseCost(a, b, out float L)) return (-1, 0, 0);

            int baseA = GetBaseThroughput(a);
            int baseB = GetBaseThroughput(b);
            int Q = Mathf.FloorToInt(Mathf.Min(baseA, baseB) / (throughputDivisorOffset + Mathf.Max(0f, L)));
            Q = Mathf.Max(minQPerTurn, Q);
            int M = Mathf.CeilToInt(maintenanceFactor * Mathf.Max(0f, L));
            return (L, Q, M);
        }

        // 新建路线
        public Route AddRoute(WarehouseBuilding a, WarehouseBuilding b)
        {
            var r = new Route { source = a, target = b };
            routes.Add(r);
            return r;
        }

        // 删除路线
        public void RemoveRoute(Route r)
        {
            routes.Remove(r);
        }
    }
}
