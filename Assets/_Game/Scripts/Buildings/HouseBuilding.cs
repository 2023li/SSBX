using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    public enum FoodSourceMode { GranaryOnly, GranaryOrWarehouse }

    /// <summary>
    /// 居民房Lv1：人口与食物消耗，默认只从粮仓拉取食物（可切换兼容模式）。
    /// 诊断日志将详细说明“找不到提供者”的具体原因。
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
        [LabelText("幸福阈值%"), Range(0, 100)] public int happinessThreshold = 20;

        [BoxGroup("人口参数")]
        [LabelText("人均食耗(一级)")] public float foodPerCapita = 0.5f;

        [BoxGroup("取料")]
        [LabelText("取料移动力"), Range(0f, 50f)] public float fetchMovePoints = 3f;

        [BoxGroup("取料")]
        [LabelText("来源模式")] public FoodSourceMode foodSourceMode = FoodSourceMode.GranaryOnly;

        [BoxGroup("调试")]
        [LabelText("详细日志")] public bool verboseLog = true;

        private static readonly ResourceType[] Tier1Foods = { ResourceType.Barley, ResourceType.Rice, ResourceType.Corn };

        protected override void OnBeforeTurnEnd(int turn)
        {
            base.OnBeforeTurnEnd(turn);
            if (!isConstructed) return;

            // 1) 容量人口：满足≥1种一级食物 +6；幸福>20% +2（文档规则）:contentReference[oaicite:1]{index=1}
            int capacity = 0;
            IFoodProvider provider = null; ResourceType? chosenFood = null;

            if (TryFindFoodProviderWithAnyTier1(out provider, out chosenFood))
                capacity += 6;

            if (KingdomStats.Instance.happiness > happinessThreshold)
                capacity += 2;

            int allowed = Mathf.Min(maxPopulation, capacity);

            // 2) 人口涨落（结算规则）:contentReference[oaicite:2]{index=2}
            if (curPopulation < allowed) curPopulation = Mathf.Min(allowed, curPopulation + fillPerTurn);
            else if (curPopulation > allowed) curPopulation = Mathf.Max(0, curPopulation - drainPerTurn);

            // 3) 食物消耗（按一级食物0.5/人/回合）:contentReference[oaicite:3]{index=3}
            int need = Mathf.CeilToInt(curPopulation * foodPerCapita);
            if (need > 0 && provider != null)
            {
                int remain = need;

                // 先用选中的食物
                if (chosenFood.HasValue)
                {
                    var t = chosenFood.Value;
                    int have = provider.Get(t);
                    int take = Mathf.Min(have, remain);
                    if (take > 0) { provider.TryProvide(t, take); remain -= take; }
                }
                // 不足则尝试其他一级食物
                for (int i = 0; i < Tier1Foods.Length && remain > 0; i++)
                {
                    var t = Tier1Foods[i];
                    int have = provider.Get(t);
                    int take = Mathf.Min(have, remain);
                    if (take > 0) { provider.TryProvide(t, take); remain -= take; }
                }

                if (verboseLog)
                {
                    Debug.Log($"[House] 消耗完毕：需求={need}，余缺={remain}，提供者={(provider as Building)?.config?.displayName ?? provider.ToString()}");
                }
            }
        }

        /// <summary>
        /// 查找“可达”的食物提供者：定义为“能到达提供者建筑的任意相邻可通行格”。
        /// 选择准则：接近代价L最小，且库存中有≥1种一级食物。
        /// </summary>
        private bool TryFindFoodProviderWithAnyTier1(out IFoodProvider provider, out ResourceType? chosen)
        {
            provider = null; chosen = null;

            var sb = new StringBuilder();
            if (verboseLog) sb.AppendLine("===== 住房取食诊断开始 =====");

            // 基础检查
            var grid = GridSystem.Instance;
            if (grid == null)
            {
                Debug.LogError("[House] GridSystem.Instance 为空，无法寻路。");
                return false;
            }

            Vector3Int start = new Vector3Int(originCell.x, originCell.y, 0);
            float mp = fetchMovePoints > 0 ? fetchMovePoints : (config != null ? config.movePointsToFetch : 3f);

            // 可达范围（只包含可通行格：道路或非道路都可通行；建筑/障碍不可通行）:contentReference[oaicite:4]{index=4}
            var reachable = Pathfinder.ComputeReachable(grid, start, mp);
            if (verboseLog) sb.AppendLine($"起点={start}，移动力={mp}，可达格数量={reachable.Count}");

            if (reachable.Count == 0)
            {
                if (verboseLog) sb.AppendLine("原因：可达范围为空（移动力过低或周边被阻塞）。");
                Debug.Log(sb.ToString());
                return false;
            }

            // 候选提供者集合
            var candidateProviders = new List<IFoodProvider>();
            var granaries = GameObject.FindObjectsOfType<GranaryBuilding>();
            for (int i = 0; i < granaries.Length; i++) candidateProviders.Add(granaries[i]);

            if (foodSourceMode == FoodSourceMode.GranaryOrWarehouse)
            {
                var warehouses = GameObject.FindObjectsOfType<WarehouseBuilding>();
                for (int i = 0; i < warehouses.Length; i++) candidateProviders.Add(warehouses[i]);
            }

            if (verboseLog)
            {
                sb.AppendLine($"模式={foodSourceMode}，候选提供者数量={candidateProviders.Count}（粮仓={granaries.Length}，仓库={(foodSourceMode == FoodSourceMode.GranaryOrWarehouse ? GameObject.FindObjectsOfType<WarehouseBuilding>().Length : 0)}）");
            }

            if (candidateProviders.Count == 0)
            {
                if (verboseLog)
                {
                    sb.AppendLine("原因：场景中没有符合模式的提供者（粮仓/仓库）。");
                    Debug.Log(sb.ToString());
                    return false;
                }
            }

            // 逐个评估候选
            float bestCost = float.MaxValue;
            IFoodProvider bestProvider = null; ResourceType? bestFood = null;

            int constructedCount = 0, withFoodCount = 0, reachableProviderCount = 0;

            foreach (var p in candidateProviders)
            {
                var pb = p as Building;
                if (pb == null) continue;

                // 1) 完工检查
                bool okConstructed = p.IsConstructed;
                if (okConstructed) constructedCount++;

                // 2) 食物存量检查
                bool hasAnyTier1 = false; ResourceType? firstFood = null;
                var foodCountsStr = new StringBuilder();
                for (int i = 0; i < Tier1Foods.Length; i++)
                {
                    var t = Tier1Foods[i];
                    int v = p.Get(t);
                    if (i > 0) foodCountsStr.Append("，");
                    foodCountsStr.Append($"{t}:{v}");
                    if (!hasAnyTier1 && v > 0) { hasAnyTier1 = true; firstFood = t; }
                }
                if (hasAnyTier1) withFoodCount++;

                // 3) 计算“邻接接近代价 L”
                float approachCost;
                Vector3Int bestApproachCell;
                int neighborTotal, neighborUnblocked;
                bool reachableToProvider = TryComputeApproachCostToProvider(p, reachable, out approachCost, out bestApproachCell, out neighborTotal, out neighborUnblocked);

                if (reachableToProvider) reachableProviderCount++;

                if (verboseLog)
                {
                    sb.AppendLine(
                        $"提供者[{pb.config?.displayName ?? pb.name}] 位置={p.OriginCell} 大小={pb.config?.size} " +
                        $"完工={okConstructed} 邻接格(总/可通行)={neighborTotal}/{neighborUnblocked} " +
                        $"接近代价L={(reachableToProvider ? approachCost.ToString("0.##") : "不可达")} " +
                        $"库存(一级)={foodCountsStr}");
                }

                // 4) 选择：必须完工、可达、且有 ≥1 种一级食物
                if (okConstructed && reachableToProvider && hasAnyTier1)
                {
                    if (approachCost < bestCost)
                    {
                        bestCost = approachCost;
                        bestProvider = p;
                        bestFood = firstFood;
                    }
                }
            }

            // 失败时汇总“原因”
            if (bestProvider == null)
            {
                if (constructedCount == 0) sb.AppendLine("原因：没有完工的提供者。");
                else if (reachableProviderCount == 0) sb.AppendLine("原因：有提供者但全部在可达范围之外（或四周邻接格均被阻塞）。");
                else if (withFoodCount == 0) sb.AppendLine("原因：有可达提供者，但均没有任何一级食物库存。");
                else sb.AppendLine("原因：综合条件筛选后无可用提供者（请检查具体条目日志）。");

                Debug.Log(sb.ToString());
                return false;
            }

            // 成功：写入并打印
            provider = bestProvider; chosen = bestFood;

            if (verboseLog)
            {
                var bp = provider as Building;
                sb.AppendLine($"✅ 选定提供者：[{bp.config?.displayName ?? bp.name}] at {provider.OriginCell}，最小接近代价L={bestCost:0.##}，优先食物={chosen}");
                sb.AppendLine("===== 住房取食诊断结束 =====");
                Debug.Log(sb.ToString());
            }
            return true;
        }

        /// <summary>
        /// 计算到“提供者建筑”的最小接近代价：在其占用的每个格周围的8邻接中，
        /// 找到一个“既可通行、又在reachable集合中”的格，取 reachable[该格] 的最小值作为 L。
        /// </summary>
        private bool TryComputeApproachCostToProvider(
            IFoodProvider provider,
            Dictionary<Vector3Int, float> reachable,
            out float bestCost,
            out Vector3Int bestApproachCell,
            out int neighborTotal,
            out int neighborUnblocked)
        {
            bestCost = float.MaxValue;
            bestApproachCell = default;
            neighborTotal = 0;
            neighborUnblocked = 0;

            var pb = provider as Building;
            if (pb == null || pb.OccupiedCells == null || pb.OccupiedCells.Count == 0)
                return false;

            var grid = GridSystem.Instance;
            // 遍历提供者“占用的每个格”的8邻接
            for (int i = 0; i < pb.OccupiedCells.Count; i++)
            {
                var oc = pb.OccupiedCells[i];

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // 跳过自身
                        var n = new Vector3Int(oc.x + dx, oc.y + dy, 0);

                        if (!grid.IsInside(n)) continue;
                        neighborTotal++;

                        if (grid.IsBlocked(n)) continue; // 邻接格被建筑/障碍占用，不能站人
                        neighborUnblocked++;

                        // 只有“可通行且在可达集合内”的格有效
                        if (reachable.TryGetValue(n, out float c))
                        {
                            if (c < bestCost)
                            {
                                bestCost = c;
                                bestApproachCell = n;
                            }
                        }
                    }
            }
            return bestCost < float.MaxValue;
        }

        // ====== 调试按钮（直接在 Inspector 上点） ======

        [Button("诊断：测试寻找提供者")]
        private void Debug_TestFindProvider()
        {
            TryFindFoodProviderWithAnyTier1(out var p, out var chosen);
        }

        [Button("诊断：打印可达范围(计数)")]
        private void Debug_DumpReachableCount()
        {
            var grid = GridSystem.Instance;
            if (grid == null) { Debug.LogWarning("GridSystem 未就绪"); return; }
            float mp = fetchMovePoints > 0 ? fetchMovePoints : (config != null ? config.movePointsToFetch : 3f);
            var dic = Pathfinder.ComputeReachable(grid, originCell, mp);
            Debug.Log($"[House] 可达格数量={dic.Count}，移动力={mp}，起点={originCell}");
        }
    }
}
