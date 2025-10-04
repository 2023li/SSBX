using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SSBX
{
    public enum Faction { Player, Enemy }

    /// <summary>
    /// 建筑基类：占格、在建/完工、回合钩子，支持阵营标记（用于“清除敌对建筑”）。
    /// </summary>
    public class Building : MonoBehaviour
    {
        [ShowInInspector][ReadOnly]public BuildingConfig config;

        [Header("状态")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public Vector3Int originCell;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public bool isConstructed;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public int buildProgress;

        [Header("阵营")]
        public Faction owner = Faction.Player;

        protected GridSystem grid => GridSystem.Instance;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        public List<Vector3Int> OccupiedCells { get; private set; } = new List<Vector3Int>();

        private bool _constructedEventFired;

        protected virtual void OnEnable()
        {
            if (TurnSystem.Instance != null)
            {
                TurnSystem.Instance.OnBeforeTurnEnd += OnBeforeTurnEnd;
                TurnSystem.Instance.OnTurnBegan += OnTurnBegan;
            }
        }

        protected virtual void OnDisable()
        {
            if (TurnSystem.Instance != null)
            {
                TurnSystem.Instance.OnBeforeTurnEnd -= OnBeforeTurnEnd;
                TurnSystem.Instance.OnTurnBegan -= OnTurnBegan;
            }
        }

        protected virtual void OnDestroy()
        {
            GameEvents.RaiseBuildingDestroyed(this);
            SetBlocked(false);
        }

        public void Init(Vector3Int origin)
        {
            originCell = origin;
            ComputeOccupiedCells();
            SetBlocked(true);
        }

        protected void ComputeOccupiedCells()
        {
            OccupiedCells.Clear();
            int s = Mathf.Max(1, config.size);
            for (int dx = 0; dx < s; dx++)
                for (int dy = 0; dy < s; dy++)
                    OccupiedCells.Add(new Vector3Int(originCell.x + dx, originCell.y + dy, 0));
        }

        protected void SetBlocked(bool blocked)
        {
            foreach (var c in OccupiedCells)
                grid.SetBlocked(c, blocked);
        }

        /// <summary>回合结束前：推进在建、扣维护费；首次完工触发事件。</summary>
        protected virtual void OnBeforeTurnEnd(int turn)
        {
            if (!isConstructed)
            {
                buildProgress++;
                if (buildProgress >= config.buildTurns)
                {
                    isConstructed = true;
                    if (!_constructedEventFired)
                    {
                        _constructedEventFired = true;
                        GameEvents.RaiseBuildingConstructed(this);
                    }
                }
            }

            if (config.maintenanceGold > 0 && isConstructed)
                KingdomStats.Instance.SpendGold(Mathf.RoundToInt(config.maintenanceGold));
        }

        protected virtual void OnTurnBegan(int turn) { }
    }
}
