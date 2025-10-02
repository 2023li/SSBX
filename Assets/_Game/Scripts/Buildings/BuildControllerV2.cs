using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;

namespace SSBX
{
    /// <summary>
    /// 建造控制V2：放置模式→待确认模式，支持PC与移动端。
    /// Ghost与区域高亮由本脚本驱动。
    /// </summary>
    public class BuildControllerV2 : MonoBehaviour
    {
        [Header("引用")]
        [LabelText("高亮器")] public PlacementHighlighter highlighter;
        [LabelText("Ghost预制体")] public PlacementGhost ghostPrefab;
        [LabelText("确认条预制体")] public RectTransform confirmBarPrefab;
        [LabelText("确认条Canvas")] public Canvas canvas;

        [Header("状态(只读)"), ReadOnly]
        [LabelText("是否放置中")] public bool placing;
        [ReadOnly, LabelText("是否待确认")] public bool waitingConfirm;
        [ReadOnly, LabelText("悬停格")] public Vector3Int hoverCell;
        [ReadOnly, LabelText("确认格")] public Vector3Int confirmCell;

        [Header("当前条目")]
        [LabelText("建筑预制体")] public Building buildingPrefab;
        [LabelText("建筑配置")] public BuildingConfig buildingConfig;

        private Camera _cam;
        private PlacementGhost _ghost;
        private RectTransform _confirmBarRT;

        private void Start()
        {
            _cam = Camera.main;
        }

        // === 外部入口 ===
        [Button("进入放置模式（测试）")]
        public void EnterPlaceMode()
        {
            if (buildingPrefab == null || buildingConfig == null)
            {
                Debug.LogWarning("[Build] 未指定 prefab/config");
                return;
            }
            placing = true;
            waitingConfirm = false;

            // 生成或启用Ghost
            if (_ghost == null) _ghost = Instantiate(ghostPrefab);
            _ghost.gameObject.SetActive(true);
            _ghost.FitSize(Mathf.Max(1, buildingConfig.size));

            // 清高亮 & 隐确认UI
            highlighter.Clear();
            HideConfirmBar();
        }

        private void Update()
        {
            if (!placing) return;

            // 统一指针世界坐标（PC鼠标 / 移动触控）
            Vector3 world;
            if (!TryGetPointerWorld(out world)) return;
            world.z = 0f;

            // 定位到格
            if (!GridSystem.Instance.TryGetCell(world, out hoverCell)) return;

            if (!waitingConfirm)
            {
                // 放置模式：Ghost 跟随 + 实时合法性
                var origin = new Vector3Int(hoverCell.x, hoverCell.y, 0);
                var size = Mathf.Max(1, buildingConfig.size);
                var valid = BuildingManager.Instance.CanPlace(buildingConfig, origin);

                // Ghost摆到区域中心
                var center = GridSystem.Instance.GetAreaCenterWorld(origin, size);
                _ghost.transform.position = center;

                // 高亮
                highlighter.HighlightArea(origin, size, valid);

                // 交互：PC左键/触控松手 → 进入待确认
                if (IsConfirmTriggered())
                {
                    if (!valid) { Debug.Log("此处不合法"); return; }
                    waitingConfirm = true;
                    confirmCell = origin;
                    ShowConfirmBarAt(center);
                }
            }
        }

        // === 确认/取消 ===
        public void OnClickConfirm()
        {
            if (!placing || !waitingConfirm) return;
            // 再次校验
            if (!BuildingManager.Instance.CanPlace(buildingConfig, confirmCell))
            {
                Debug.Log("确认失败：位置已变更或不合法");
                return;
            }
            var b = BuildingManager.Instance.Place(buildingPrefab, buildingConfig, confirmCell);
            ExitPlaceMode();
            Debug.Log($"建造：{b.config.displayName} at {confirmCell}");
        }

        public void OnClickCancel()
        {
            if (!placing) return;
            waitingConfirm = false;
            HideConfirmBar();
        }

        private void ExitPlaceMode()
        {
            placing = false;
            waitingConfirm = false;
            highlighter.Clear();
            HideConfirmBar();
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        // === 工具：确认触发/定位UI/指针坐标 ===
        private bool IsConfirmTriggered()
        {
            // PC：鼠标左键Down；移动：主触点从“接触中”→“离开”的短点按
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch.tapCount.ReadValue() > 0 && ts.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended)
                return true;

            return false;
        }

        private bool TryGetPointerWorld(out Vector3 world)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var sp = mouse.position.ReadValue();
                var screen = new Vector3(sp.x, sp.y, -_cam.transform.position.z);
                world = _cam.ScreenToWorldPoint(screen);
                return true;
            }
            var ts = Touchscreen.current;
            if (ts != null)
            {
                var p = ts.primaryTouch.position.ReadValue();
                var screen = new Vector3(p.x, p.y, -_cam.transform.position.z);
                world = _cam.ScreenToWorldPoint(screen);
                return true;
            }
            world = default;
            return false;
        }

        private void ShowConfirmBarAt(Vector3 worldPos)
        {
            if (_confirmBarRT == null)
            {
                _confirmBarRT = Instantiate(confirmBarPrefab, canvas.transform);
                // 绑定按钮事件
                var buttons = _confirmBarRT.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn.name.Contains("Confirm")) btn.onClick.AddListener(OnClickConfirm);
                    else if (btn.name.Contains("Cancel")) btn.onClick.AddListener(OnClickCancel);
                }
            }

            _confirmBarRT.gameObject.SetActive(true);
            var cg = _confirmBarRT.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }

            // 世界→屏幕→UI坐标
            var sp = _cam.WorldToScreenPoint(worldPos + new Vector3(0, GridSystem.Instance.unityGrid.cellSize.y * 0.6f, 0));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), sp, canvas.worldCamera, out var lp);
            _confirmBarRT.anchoredPosition = lp;
        }

        private void HideConfirmBar()
        {
            if (_confirmBarRT == null) return;
            var cg = _confirmBarRT.GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
            _confirmBarRT.gameObject.SetActive(false);
        }
    }
}
