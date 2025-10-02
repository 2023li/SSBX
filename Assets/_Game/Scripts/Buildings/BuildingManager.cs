using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>建筑管理：合法性校验、实例化、注册索引。</summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        private readonly List<Building> _buildings = new List<Building>();

        [LabelText("调试日志")] public bool verbose;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool CanPlace(BuildingConfig cfg, Vector3Int originCell)
        {
            return GridIndex.Instance.IsAreaFree(cfg, originCell);
        }

        /// <summary>放置建筑（已校验）。</summary>
        public Building Place(Building prefab, BuildingConfig cfg, Vector3Int originCell)
        {
            var go = Instantiate(prefab.gameObject);
            var b = go.GetComponent<Building>();
            b.config = cfg;
            b.Init(new Vector3Int(originCell.x, originCell.y, 0));

            // 父物体摆在“区域中心”
            var center = GridSystem.Instance.GetAreaCenterWorld(originCell, Mathf.Max(1, cfg.size));
            go.transform.position = new Vector3(center.x, center.y, 0f);

            // 注册占格：索引 + 网格阻塞
            GridIndex.Instance.Register(b);
            foreach (var c in b.OccupiedCells)
                GridSystem.Instance.SetBlocked(c, true);

            _buildings.Add(b);
            if (verbose) Debug.Log($"[BuildingManager] 放置 {cfg.displayName} at {originCell}，占格={b.OccupiedCells.Count}");
            return b;
        }

        /// <summary>拆除建筑。</summary>
        public void Remove(Building b)
        {
            if (b == null) return;
            GridIndex.Instance.Unregister(b);
            foreach (var c in b.OccupiedCells)
                GridSystem.Instance.SetBlocked(c, false);
            _buildings.Remove(b);
            Destroy(b.gameObject);
        }
    }
}
