using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 通用矩形Ghost：按 cellSize×size 自动缩放，随BuildController定位。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlacementGhost : MonoBehaviour
    {
        [LabelText("透明度")] public float alpha = 0.35f;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            var c = _sr.color; c.a = alpha; _sr.color = c;
        }

        /// <summary>按占格尺寸适配矩形大小（世界尺寸）。</summary>
        public void FitSize(int size)
        {
            var grid = GridSystem.Instance.unityGrid;
            Vector2 cell = grid.cellSize;
            Vector2 target = cell * size;
            Vector2 spriteSize = _sr.sprite.bounds.size;
            float sx = target.x / Mathf.Max(0.0001f, spriteSize.x);
            float sy = target.y / Mathf.Max(0.0001f, spriteSize.y);
            transform.localScale = new Vector3(sx, sy, 1f);
        }

        [Button("调试：适配2×2")]
        private void DebugFit2() => FitSize(2);
    }
}
