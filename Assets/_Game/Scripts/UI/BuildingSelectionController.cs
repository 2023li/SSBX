using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 建筑拾取控制：鼠标左键点格→选中建筑→显示悬浮按钮条（信息）。
    /// </summary>
    public class BuildingSelectionController : MonoBehaviour
    {
        [LabelText("选择条预制体")] public SelectionBarUI selectionBarPrefab;
        [LabelText("Canvas")] public Canvas canvas;

        [ReadOnly, LabelText("当前选中")] public Building selected;

        private SelectionBarUI _bar;
        private Camera _cam;

        private void Awake() { _cam = Camera.main; }

        private void Update()
        {
            if (Mouse.current == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryPick();
        }

        private void TryPick()
        {
            var mp = Mouse.current.position.ReadValue();
            var screen = new Vector3(mp.x, mp.y, -_cam.transform.position.z);
            var world = _cam.ScreenToWorldPoint(screen); world.z = 0f;

            if (!GridSystem.Instance.TryGetCell(world, out var cell)) { Deselect(); return; }
            var b = GridIndex.Instance.GetBuildingAt(cell);
            if (b != null) Select(b); else Deselect();
        }

        private void Select(Building b)
        {
            selected = b;
            if (_bar == null) _bar = Instantiate(selectionBarPrefab, canvas.transform);
            _bar.Bind(this);
            _bar.ShowAt(WorldAnchor(b), b);
        }

        public void Deselect()
        {
            selected = null;
            if (_bar != null) _bar.Hide();
        }

        private Vector3 WorldAnchor(Building b)
        {
            return GridSystem.Instance.GetAreaCenterWorld(b.originCell, Mathf.Max(1, b.config.size));
        }
    }
}
