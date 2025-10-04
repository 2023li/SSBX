using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Cinemachine;
using UnityEngine.InputSystem.Controls;

namespace SSBX
{
    /// <summary>
    /// Cinemachine 友好的相机控制：PC(WASD/边缘/滚轮) + 移动端(拖拽/双指缩放)。
    /// - 自由机位：移动 vcam.transform
    /// - 跟随机位：优先改 Body 偏移（Transposer/FramingTransposer）
    /// - 缩放：改 vcam.m_Lens.OrthographicSize（必要时也同步主相机）
    /// - 钳制：优先使用 Confiner2D；否则按 GridSystem 边界手动钳制
    /// </summary>
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CinemachinePanZoomController : MonoBehaviour
    {
        [Title("PC 输入")]
        [LabelText("允许边缘滚动")] public bool enableEdgePan = true;
        [LabelText("边缘厚度(px)")] public int edgeThickness = 14;
        [LabelText("WASD速度(世界单位/秒)")] public float keyboardPanSpeed = 12f;
        [LabelText("边缘速度(世界单位/秒)")] public float edgePanSpeed = 10f;

        [Title("移动端输入")]
        [LabelText("拖拽速度系数")] public float touchPanSpeed = 1.0f;
        [LabelText("双指缩放系数")] public float pinchZoomSpeed = 0.02f;

        [Title("缩放")]
        [LabelText("滚轮缩放速度")] public float wheelZoomSpeed = 12f;
        [LabelText("最小正交尺寸")] public float minOrthoSize = 3f;
        [LabelText("最大正交尺寸")] public float maxOrthoSize = 20f;
        [LabelText("缩放平滑")] public float zoomSmooth = 10f;

        [Title("UI/模式联动")]
        [LabelText("指针在UI上时忽略输入")] public bool ignoreWhenPointerOverUI = true;
        [LabelText("有面板打开时禁用相机")] public bool disableWhenAnyPanelOpen = true;

        [Title("边界钳制")]
        [LabelText("优先使用Confiner2D")] public bool preferConfiner2D = true;
        [LabelText("使用Grid手动钳制(外延格)")] public int clampMarginCells = 2;

        [Title("跟随模式")]
        [LabelText("有Follow时用偏移而非移动vcam")] public bool useFollowOffsetWhenHasFollow = true;

        private CinemachineVirtualCamera _vcam;
        private CinemachineConfiner2D _confiner2D;
        private Camera _mainCam;
        private GridSystem _grid;

        private float _targetOrtho;

        private void Awake()
        {
            _vcam = GetComponent<CinemachineVirtualCamera>();
            _confiner2D = GetComponent<CinemachineConfiner2D>();
            _mainCam = Camera.main;
            _grid = GridSystem.Instance;

            // 初始化缩放目标：取 vcam 的镜头正交尺寸（若主相机是正交也一并读）
            _targetOrtho = _vcam.m_Lens.OrthographicSize;
            if (_mainCam != null && _mainCam.orthographic)
                _targetOrtho = _mainCam.orthographicSize;
        }

        private void Update()
        {
            if (_vcam == null || _grid == null) return;

            // UI 打开时禁用
            if (disableWhenAnyPanelOpen && UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
            {
                SmoothZoom(); ManualClampIfNeeded();
                return;
            }

            // 指针悬停 UI 时：仅允许滚轮缩放（可按需禁用）
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (ignoreWhenPointerOverUI && overUI)
            {
                HandleWheelZoom();
                SmoothZoom(); ManualClampIfNeeded();
                return;
            }

            // —— 桌面输入：WASD/边缘/滚轮 ——
            HandleDesktopPan();
            HandleWheelZoom();

            // —— 移动端输入：一指拖拽、双指捏合 ——
            HandleTouchGestures();

            SmoothZoom();
            ManualClampIfNeeded();
        }

        // =================== 输入处理 ===================

        private void HandleDesktopPan()
        {
            var kb = Keyboard.current;
            Vector2 vKey = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vKey.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vKey.y -= 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) vKey.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) vKey.x += 1;
                if (vKey.sqrMagnitude > 1) vKey.Normalize();
            }

            Vector2 vEdge = Vector2.zero;
            if (enableEdgePan && Mouse.current != null)
            {
                var mp = Mouse.current.position.ReadValue();
                if (mp.x <= edgeThickness) vEdge.x -= 1;
                else if (mp.x >= Screen.width - edgeThickness) vEdge.x += 1;
                if (mp.y <= edgeThickness) vEdge.y -= 1;
                else if (mp.y >= Screen.height - edgeThickness) vEdge.y += 1;
                if (vEdge.sqrMagnitude > 1) vEdge.Normalize();
            }

            Vector3 delta = new Vector3(vKey.x, vKey.y, 0f) * keyboardPanSpeed * Time.deltaTime
                          + new Vector3(vEdge.x, vEdge.y, 0f) * edgePanSpeed * Time.deltaTime;
            ApplyPan(delta);
        }

        private void HandleWheelZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetOrtho -= scroll * 0.01f * wheelZoomSpeed * (_targetOrtho * 0.5f + 1f);
                _targetOrtho = Mathf.Clamp(_targetOrtho, minOrthoSize, maxOrthoSize);
            }
        }

        private void HandleTouchGestures()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;

            int active = 0;
            foreach (var t in ts.touches) if (t.press.isPressed) active++;

            if (active == 1)
            {
                // 一指拖拽：像素 → 世界单位（正交下 2*size/屏高）
                var touch = ts.primaryTouch;
                if (touch.press.isPressed)
                {
                    Vector2 deltaPx = touch.delta.ReadValue();
                    float unitsPerPixel = (GetCurrentOrthoSize() * 2f) / Mathf.Max(1, Screen.height);
                    Vector3 move = new Vector3(-deltaPx.x * unitsPerPixel * touchPanSpeed,
                                               -deltaPx.y * unitsPerPixel * touchPanSpeed, 0f);
                    ApplyPan(move);
                }
            }
            else if (active >= 2)
            {
                // 双指捏合缩放
                TouchControl t0 = null, t1 = null;
                foreach (var t in ts.touches) { if (t.press.isPressed) { if (t0 == null) t0 = t; else if (t1 == null) { t1 = t; break; } } }
                if (t0 != null && t1 != null)
                {
                    Vector2 p0 = t0.position.ReadValue();
                    Vector2 p1 = t1.position.ReadValue();
                    Vector2 prev0 = p0 - t0.delta.ReadValue();
                    Vector2 prev1 = p1 - t1.delta.ReadValue();

                    float prevDist = (prev0 - prev1).magnitude;
                    float curDist = (p0 - p1).magnitude;
                    float delta = curDist - prevDist; // >0 放大，<0 缩小

                    _targetOrtho -= delta * pinchZoomSpeed;
                    _targetOrtho = Mathf.Clamp(_targetOrtho, minOrthoSize, maxOrthoSize);
                }
            }
        }

        // 把平移应用到 vcam（自由机位：改transform；跟随机位：优先改 Body 偏移）
        private void ApplyPan(Vector3 deltaWorld)
        {
            if (_vcam.Follow == null || !useFollowOffsetWhenHasFollow)
            {
                // 自由机位：移动 vcam 世界坐标（仅改XY）
                var p = transform.position;
                p += new Vector3(deltaWorld.x, deltaWorld.y, 0f);
                transform.position = p;
                return;
            }

            // 跟随机位：优先改 Body 偏移
            var transposer = _vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                var off = transposer.m_FollowOffset;
                off.x += deltaWorld.x; off.y += deltaWorld.y;
                transposer.m_FollowOffset = off;
                return;
            }
            var framing = _vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                var off = framing.m_TrackedObjectOffset;
                off += new Vector3(deltaWorld.x, deltaWorld.y,0);
                framing.m_TrackedObjectOffset = off;
                return;
            }

            // 兜底：若 Body 不可用，则移动 vcam 世界坐标
            var pp = transform.position;
            pp += new Vector3(deltaWorld.x, deltaWorld.y, 0f);
            transform.position = pp;
        }

        // 平滑把 vcam 镜头尺寸逼近 target（并可同步主相机，避免一帧闪烁）
        private void SmoothZoom()
        {
            float current = GetCurrentOrthoSize();
            float next = Mathf.Lerp(current, _targetOrtho, 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime));
            SetCurrentOrthoSize(next);
        }

        private float GetCurrentOrthoSize()
        {
            // 以 vcam Lens 为主；若主相机正交也同步读取
            float sz = _vcam.m_Lens.OrthographicSize;
            if (_mainCam != null && _mainCam.orthographic) sz = _mainCam.orthographicSize;
            return sz;
        }

        private void SetCurrentOrthoSize(float v)
        {
            _vcam.m_Lens.OrthographicSize = v;
            if (_mainCam != null && _mainCam.orthographic) _mainCam.orthographicSize = v;
        }

        // =================== 边界钳制 ===================

        private void ManualClampIfNeeded()
        {
            // 若存在 Confiner2D 且选择优先使用，则不做手动钳制（避免重复约束）
            if (preferConfiner2D && _confiner2D != null && _confiner2D.enabled) return;

            // 计算“相机中心”的世界坐标（自由机位=vcam位置；跟随机位=Follow位置+偏移）
            Vector3 center = GetCameraCenterWorld();
            float halfH = GetCurrentOrthoSize();
            float aspect = _mainCam ? _mainCam.aspect : (Screen.width / Mathf.Max(1f, (float)Screen.height));
            float halfW = halfH * aspect;

            // Grid 边界转世界坐标
            Vector3 minW = _grid.GetCellCenterWorld(new Vector3Int(_grid.minCell.x, _grid.minCell.y, 0));
            Vector3 maxW = _grid.GetCellCenterWorld(new Vector3Int(_grid.maxCell.x, _grid.maxCell.y, 0));
            Vector2 cell = _grid.unityGrid.cellSize;

            float marginX = cell.x * clampMarginCells;
            float marginY = cell.y * clampMarginCells;

            float minX = minW.x - marginX + halfW;
            float maxX = maxW.x + marginX - halfW;
            float minY = minW.y - marginY + halfH;
            float maxY = maxW.y + marginY - halfH;

            center.x = Mathf.Clamp(center.x, minX, maxX);
            center.y = Mathf.Clamp(center.y, minY, maxY);

            SetCameraCenterWorld(center);
        }

        private Vector3 GetCameraCenterWorld()
        {
            if (_vcam.Follow == null || !useFollowOffsetWhenHasFollow)
                return new Vector3(transform.position.x, transform.position.y, 0f);

            // 以 Follow + 偏移估算当前相机中心
            Vector3 basePos = _vcam.Follow.position;
            var transposer = _vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                var off = transposer.m_FollowOffset;
                return new Vector3(basePos.x + off.x, basePos.y + off.y, 0f);
            }
            var framing = _vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                var off = framing.m_TrackedObjectOffset;
                return new Vector3(basePos.x + off.x, basePos.y + off.y, 0f);
            }
            return new Vector3(transform.position.x, transform.position.y, 0f);
        }

        private void SetCameraCenterWorld(Vector3 center)
        {
            if (_vcam.Follow == null || !useFollowOffsetWhenHasFollow)
            {
                var p = transform.position;
                p.x = center.x; p.y = center.y;
                transform.position = p;
                return;
            }

            Vector3 basePos = _vcam.Follow.position;
            var transposer = _vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                var off = transposer.m_FollowOffset;
                off.x = center.x - basePos.x;
                off.y = center.y - basePos.y;
                transposer.m_FollowOffset = off;
                return;
            }
            var framing = _vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                var off = framing.m_TrackedObjectOffset;
                off.x = center.x - basePos.x;
                off.y = center.y - basePos.y;
                framing.m_TrackedObjectOffset = off;
                return;
            }

            var p2 = transform.position;
            p2.x = center.x; p2.y = center.y;
            transform.position = p2;
        }
    }
}
