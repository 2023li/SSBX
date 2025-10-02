using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 基于 Tilemap 的区域高亮：绿色=合法，红色=不合法。
    /// </summary>
    public class PlacementHighlighter : MonoBehaviour
    {
        [LabelText("高亮Tilemap")] public Tilemap overlayTilemap;
        [LabelText("合法Tile")] public TileBase validTile;
        [LabelText("不合法Tile")] public TileBase invalidTile;

        private readonly List<Vector3Int> _lastCells = new List<Vector3Int>();

        [Button("清空")]
        public void Clear()
        {
            foreach (var c in _lastCells)
            {
                overlayTilemap.SetTile(c, null);
            }
            _lastCells.Clear();
        }

        /// <summary>高亮一块区域。</summary>
        public void HighlightArea(Vector3Int origin, int size, bool valid)
        {
            Debug.Log(11);
            // 先清再画
            Clear();
            var tile = valid ? validTile : invalidTile;
            for (int dx = 0; dx < size; dx++)
                for (int dy = 0; dy < size; dy++)
                {
                    var c = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                    overlayTilemap.SetTile(c, tile);
                    _lastCells.Add(c);
                }
        }

      

    }
}
