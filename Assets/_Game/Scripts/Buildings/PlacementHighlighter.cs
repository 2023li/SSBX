using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>用不同瓦片高亮：绿=合法；浅红=整体非法；深红=具体非法单元格。</summary>
    public class PlacementHighlighter : MonoBehaviour
    {
        [LabelText("高亮Tilemap")] public Tilemap overlayTilemap;

        [BoxGroup("瓦片"), LabelText("绿色(合法)")] public TileBase tileGreen;
        [BoxGroup("瓦片"), LabelText("浅红(整体非法)")] public TileBase tileLightRed;
        [BoxGroup("瓦片"), LabelText("深红(具体非法)")] public TileBase tileDeepRed;

        private readonly List<Vector3Int> _lastCells = new List<Vector3Int>();

        [Button("清空")]
        public void Clear()
        {
            foreach (var c in _lastCells) overlayTilemap.SetTile(c, null);
            _lastCells.Clear();
        }

        /// <summary>
        /// 以“逐格结果”绘制高亮：合法→全绿；整体非法→非法格深红，其余浅红。
        /// </summary>
        public void HighlightAreaDetailed(Vector3Int origin, int size,
            HashSet<Vector3Int> invalidCells, bool overallValid)
        {
            Clear();
            for (int dx = 0; dx < size; dx++)
                for (int dy = 0; dy < size; dy++)
                {
                    var c = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                    TileBase tile;
                    if (overallValid) tile = tileGreen;
                    else tile = invalidCells.Contains(c) ? tileDeepRed : tileLightRed;

                    overlayTilemap.SetTile(c, tile);
                    _lastCells.Add(c);
                }
        }
    }
}
