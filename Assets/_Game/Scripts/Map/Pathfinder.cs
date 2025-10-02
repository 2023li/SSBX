using System.Collections.Generic;
using UnityEngine;

namespace SSBX
{
    /// <summary>
    /// 简易Dijkstra：用于“可达范围”和“拉取物资”的最小代价。
    /// 注意：地图不大时足够用；后续可优化为堆。
    /// </summary>
    public static class Pathfinder
    {
        public static Dictionary<Vector3Int, float> ComputeReachable(GridSystem grid, Vector3Int start, float movePoints)
        {
            var cost = new Dictionary<Vector3Int, float>();
            var open = new List<Vector3Int>();

            cost[start] = 0f;
            open.Add(start);

            while (open.Count > 0)
            {
                // 取当前open中代价最小的
                int bestIdx = 0;
                float bestCost = cost[open[0]];
                for (int i = 1; i < open.Count; i++)
                {
                    float c = cost[open[i]];
                    if (c < bestCost) { bestCost = c; bestIdx = i; }
                }
                var cur = open[bestIdx];
                open.RemoveAt(bestIdx);

                foreach (var n in grid.GetNeighbors8(cur))
                {
                    float step = grid.GetStepCost(n);
                    float newCost = bestCost + step;
                    if (newCost > movePoints) continue;

                    if (!cost.ContainsKey(n) || newCost < cost[n])
                    {
                        cost[n] = newCost;
                        if (!open.Contains(n)) open.Add(n);
                    }
                }
            }
            return cost;
        }
    }
}
