using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 格子→建筑 的查询索引，提供 O(1) 的占用判定与定位。
    /// 由 BuildingManager 在放置/拆除时维护。
    /// </summary>
    public class GridIndex : MonoBehaviour
    {
        public static GridIndex Instance { get; private set; }

        // 每个格子只允许有一个占用建筑；大建筑会注册多个格子
        private readonly Dictionary<Vector3Int, Building> _cellToBuilding = new Dictionary<Vector3Int, Building>();

        [LabelText("调试打印")] public bool verbose;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>区域是否完全空闲（不越界、不在道路上、不被占用）。</summary>
        public bool IsAreaFree(BuildingConfig cfg, Vector3Int originCell)
        {
            var grid = GridSystem.Instance;
            int s = Mathf.Max(1, cfg.size);
            for (int dx = 0; dx < s; dx++)
                for (int dy = 0; dy < s; dy++)
                {
                    var c = new Vector3Int(originCell.x + dx, originCell.y + dy, 0);
                    if (!grid.IsInside(c)) return false;
                    if (grid.IsBlocked(c)) return false; // 建筑/障碍
                    if (grid.HasRoad(c)) return false;    // 道路禁止放置
                    if (_cellToBuilding.ContainsKey(c)) return false; // 已被占用
                }
            return true;
        }

        /// <summary>注册一座建筑的占格。</summary>
        public void Register(Building b)
        {
            foreach (var c in b.OccupiedCells)
            {
                if (_cellToBuilding.ContainsKey(c))
                    Debug.LogWarning($"[GridIndex] 注册冲突：{c} 已被 {_cellToBuilding[c].name} 占用");
                _cellToBuilding[c] = b;
            }
            if (verbose) Debug.Log($"[GridIndex] 注册 {b.name} 占用格数={b.OccupiedCells.Count}");
        }

        /// <summary>注销一座建筑的占格。</summary>
        public void Unregister(Building b)
        {
            foreach (var c in b.OccupiedCells)
            {
                if (_cellToBuilding.TryGetValue(c, out var bb) && bb == b)
                    _cellToBuilding.Remove(c);
            }
            if (verbose) Debug.Log($"[GridIndex] 注销 {b.name}");
        }

        /// <summary>该格是否被建筑占用（返回建筑或 null）。</summary>
        public Building GetBuildingAt(Vector3Int cell)
        {
            _cellToBuilding.TryGetValue(new Vector3Int(cell.x, cell.y, 0), out var b);
            return b;
        }

        [Button("调试：打印占用统计")]
        private void Debug_PrintCount()
        {
            Debug.Log($"[GridIndex] 当前登记格数={_cellToBuilding.Count}");
        }
    }
}
