using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace SSBX
{
    public enum HeatmapMode { None, Medical, Security, Beautify }

    /// <summary>热力层控制：用不同Tile显示医疗/治安/美化。</summary>
    public class HeatmapController : MonoBehaviour
    {
        [LabelText("热力层Tilemap")] public Tilemap overlayTilemap;

        [BoxGroup("医疗Tiles")][LabelText("1级")] public TileBase med1;
        [BoxGroup("医疗Tiles")][LabelText("2级")] public TileBase med2;
        [BoxGroup("医疗Tiles")][LabelText("3级")] public TileBase med3;

        [BoxGroup("治安Tiles")][LabelText("1级")] public TileBase sec1;
        [BoxGroup("治安Tiles")][LabelText("2级")] public TileBase sec2;
        [BoxGroup("治安Tiles")][LabelText("3级(预留)")] public TileBase sec3;

        [BoxGroup("美化Tiles(0~5)")]
        public TileBase beaut0, beaut1, beaut2, beaut3, beaut4, beaut5;

        [BoxGroup("美化分档阈值"), InfoBox("美化值按阈值分档：≤t1→0档，≤t2→1档，…", InfoMessageType.None)]
        public int t1 = 0, t2 = 2, t3 = 5, t4 = 9, t5 = 14, t6 = 20;

        [ReadOnly, LabelText("当前模式")] public HeatmapMode current = HeatmapMode.None;

        [Button("显示：无")] public void ShowNone() { current = HeatmapMode.None; Rebuild(); }
        [Button("显示：医疗")] public void ShowMedical() { current = HeatmapMode.Medical; Rebuild(); }
        [Button("显示：治安")] public void ShowSecurity() { current = HeatmapMode.Security; Rebuild(); }
        [Button("显示：美化")] public void ShowBeautify() { current = HeatmapMode.Beautify; Rebuild(); }

        [Button("重建热力图")]
        public void Rebuild()
        {
            var g = GridSystem.Instance;
            overlayTilemap.ClearAllTiles();

            for (int x = g.minCell.x; x <= g.maxCell.x; x++)
                for (int y = g.minCell.y; y <= g.maxCell.y; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (!g.IsInside(c)) continue;

                    TileBase tile = null;
                    switch (current)
                    {
                        case HeatmapMode.Medical:
                            switch (g.GetMedical(c))
                            {
                                case 1: tile = med1; break;
                                case 2: tile = med2; break;
                                case 3: tile = med3; break;
                            }
                            break;
                        case HeatmapMode.Security:
                            switch (g.GetSecurity(c))
                            {
                                case 1: tile = sec1; break;
                                case 2: tile = sec2; break;
                                case 3: tile = sec3; break;
                            }
                            break;
                        case HeatmapMode.Beautify:
                            int b = g.GetBeautify(c);
                            if (b <= t1) tile = beaut0;
                            else if (b <= t2) tile = beaut1;
                            else if (b <= t3) tile = beaut2;
                            else if (b <= t4) tile = beaut3;
                            else if (b <= t5) tile = beaut4;
                            else tile = beaut5;
                            break;
                    }
                    if (tile != null) overlayTilemap.SetTile(c, tile);
                }
        }
    }
}
