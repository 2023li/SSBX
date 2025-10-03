using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    [System.Flags]
    public enum CellInvalidReason
    {
        None = 0,
        OutOfBounds = 1 << 0,
        Blocked = 1 << 1,
        Road = 1 << 2,
        Occupied = 1 << 3
    }

    public class GridIndex : MonoBehaviour
    {
        public static GridIndex Instance { get; private set; }
        private readonly Dictionary<Vector3Int, Building> _cellToBuilding = new Dictionary<Vector3Int, Building>();

        [LabelText("调试打印")] public bool verbose;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsAreaFree(BuildingConfig cfg, Vector3Int originCell)
        {
            var invalid = new List<(Vector3Int, CellInvalidReason)>();
            return ValidateArea(cfg, originCell, out invalid);
        }

        /// <summary>逐格校验：返回是否整体合法，并给出每个不合法格与原因。</summary>
        public bool ValidateArea(BuildingConfig cfg, Vector3Int originCell,
            out List<(Vector3Int cell, CellInvalidReason reason)> invalidCells)
        {
            invalidCells = new List<(Vector3Int, CellInvalidReason)>();
            var grid = GridSystem.Instance;
            int s = Mathf.Max(1, cfg.size);

            for (int dx = 0; dx < s; dx++)
                for (int dy = 0; dy < s; dy++)
                {
                    var c = new Vector3Int(originCell.x + dx, originCell.y + dy, 0);
                    var reason = CellInvalidReason.None;

                    if (!grid.IsInside(c)) reason |= CellInvalidReason.OutOfBounds;
                    else
                    {
                        if (grid.IsBlocked(c)) reason |= CellInvalidReason.Blocked;
                        if (grid.HasRoad(c)) reason |= CellInvalidReason.Road;
                        if (_cellToBuilding.ContainsKey(c)) reason |= CellInvalidReason.Occupied;
                    }

                    if (reason != CellInvalidReason.None)
                        invalidCells.Add((c, reason));
                }
            return invalidCells.Count == 0;
        }

        public void Register(Building b)
        {
            foreach (var c in b.OccupiedCells)
            {
                if (_cellToBuilding.ContainsKey(c))
                    Debug.LogWarning($"[GridIndex] 冲突：{c} 已被 {_cellToBuilding[c].name} 占用");
                _cellToBuilding[c] = b;
            }
        }

        public void Unregister(Building b)
        {
            foreach (var c in b.OccupiedCells)
                if (_cellToBuilding.TryGetValue(c, out var bb) && bb == b) _cellToBuilding.Remove(c);
        }

        public Building GetBuildingAt(Vector3Int cell)
        {
            _cellToBuilding.TryGetValue(new Vector3Int(cell.x, cell.y, 0), out var b);
            return b;
        }
    }
}
