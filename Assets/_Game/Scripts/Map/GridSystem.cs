using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SSBX
{
    public class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        [Header("Tilemaps")]
        public Grid unityGrid;
        public Tilemap groundTilemap;
        public Tilemap roadTilemap;
        public Tilemap blockTilemap;

        [Header("尺寸")]
        public Vector3Int minCell;
        public Vector3Int maxCell;

        private Dictionary<Vector3Int, CellData> _cells = new Dictionary<Vector3Int, CellData>();

        private void Awake()
        {
            if (Instance != this && Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BakeCells();
        }

        public void BakeCells()
        {
            _cells.Clear();
            for (int x = minCell.x; x <= maxCell.x; x++)
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    bool hasGround = groundTilemap && groundTilemap.HasTile(c);
                    if (!hasGround) continue;

                    var data = new CellData
                    {
                        hasRoad = roadTilemap && roadTilemap.HasTile(c),
                        blocked = blockTilemap && blockTilemap.HasTile(c),
                        beautify = 0,
                        security = 0,
                        medical = 0
                    };
                    _cells[c] = data;
                }
        }

        public bool TryGetCell(Vector3 worldPos, out Vector3Int cell)
        {
            var c = unityGrid.WorldToCell(worldPos);
            c.z = 0;
            cell = c;
            return _cells.ContainsKey(cell);
        }

        public bool IsInside(Vector3Int c) => _cells.ContainsKey(c);
        public bool IsBlocked(Vector3Int c) => IsInside(c) && _cells[c].blocked;
        public bool HasRoad(Vector3Int c) => IsInside(c) && _cells[c].hasRoad;

        public void SetBlocked(Vector3Int c, bool blocked)
        {
            if (!IsInside(c)) return;
            var d = _cells[c]; d.blocked = blocked; _cells[c] = d;
        }

        public float GetStepCost(Vector3Int c) => (IsInside(c) && _cells[c].hasRoad) ? 0.5f : 1f;

        public IEnumerable<Vector3Int> GetNeighbors8(Vector3Int c)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = new Vector3Int(c.x + dx, c.y + dy, 0);
                    if (!IsInside(n)) continue;
                    if (IsBlocked(n)) continue;
                    yield return n;
                }
        }

        /// <summary>单元格中心（使用 Tilemap 的 API），z=0。</summary>
        public Vector3 GetCellCenterWorld(Vector3Int c)
        {
            Vector3 pos = groundTilemap ? groundTilemap.GetCellCenterWorld(new Vector3Int(c.x, c.y, 0))
                                        : unityGrid.CellToWorld(new Vector3Int(c.x, c.y, 0));
            pos.z = 0f;
            return pos;
        }

        /// <summary>多格区域几何中心（origin为左下角，size=占格尺寸）。</summary>
        public Vector3 GetAreaCenterWorld(Vector3Int origin, int size)
        {
            var a = GetCellCenterWorld(origin);
            var b = GetCellCenterWorld(origin + new Vector3Int(size - 1, size - 1, 0));
            var center = (a + b) * 0.5f;
            center.z = 0f;
            return center;
        }


        public short GetMedical(Vector3Int c) => IsInside(c) ? _cells[c].medical : (short)0;
        public short GetSecurity(Vector3Int c) => IsInside(c) ? _cells[c].security : (short)0;
        public short GetBeautify(Vector3Int c) => IsInside(c) ? _cells[c].beautify : (short)0;

        public void AddMedical(Vector3Int c, short v)
        {
            if (!IsInside(c)) return;
            var d = _cells[c]; d.medical = (short)Mathf.Max(short.MinValue, Mathf.Min(short.MaxValue, d.medical + v)); _cells[c] = d;
        }
        public void AddSecurity(Vector3Int c, short v)
        {
            if (!IsInside(c)) return;
            var d = _cells[c]; d.security = (short)Mathf.Max(short.MinValue, Mathf.Min(short.MaxValue, d.security + v)); _cells[c] = d;
        }
        public void AddBeautify(Vector3Int c, short v)
        {
            if (!IsInside(c)) return;
            var d = _cells[c]; d.beautify = (short)Mathf.Max(short.MinValue, Mathf.Min(short.MaxValue, d.beautify + v)); _cells[c] = d;
        }

    }

    public struct CellData
    {
        public bool hasRoad;
        public bool blocked;
        public short beautify;
        public short security;
        public short medical;
    }
}
