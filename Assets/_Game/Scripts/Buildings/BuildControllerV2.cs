using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// BuildControllerV2（兼容输入路由 + 高亮瓦片规则）
    /// - 进入放置时 Push 到 GameInputRouter 的输入层栈；ESC/Back 优先取消/退出放置
    /// - 鼠标悬停 UI 或有 UI 面板打开时不处理建造输入
    /// - 合法=绿色；整体不合法=浅红；具体不合法单元格=深红；不可在道路/障碍/越界/占用上放置
    /// - 不依赖 Ghost，可选后续再加
    /// </summary>
    public class BuildControllerV2 : MonoBehaviour, IInputLayer
    {
        [Title("基础")]
        [LabelText("默认启用热键(B)进入放置")] public bool enableHotkeyToggle = true;
        [LabelText("热键(进入/退出放置)")] public Key toggleKey = Key.B;
        [LabelText("左键确认 / 右键取消")] public bool mouseConfirmCancel = true;

        [Title("放置对象（测试可直接在Inspector指定）")]
        [LabelText("预制体(含 Building)")] public Building selectedPrefab;
        [LabelText("配置(BC_中文)")] public BuildingConfig selectedConfig;

        [Title("高亮绘制用 Tile（按你的标准）")]
        [LabelText("合法(绿色)")] public TileBase tileValidGreen;
        [LabelText("整体不合法(浅红)")] public TileBase tileInvalidLightRed;
        [LabelText("具体不合法(深红)")] public TileBase tileInvalidDeepRed;

        [Title("调试")]
        [LabelText("打印详细日志")] public bool verbose = false;

        // —— 运行时状态 ——
        [ReadOnly, LabelText("放置中")] public bool placing;
        [ReadOnly, LabelText("当前原点格")] public Vector3Int currentOrigin;
        [ReadOnly, LabelText("当前是否合法")] public bool currentValid;

        [SerializeField] private GridSystem _grid;
        private Tilemap _overlay;       // Overlay_Highlight
        private Tilemap _road;          // roadTilemap（判定不可放置）
        private Tilemap _block;         // blockTilemap（障碍）
        private HashSet<Vector3Int> _lastPaint = new HashSet<Vector3Int>(128);
        private List<Vector3Int> _invalidCells = new List<Vector3Int>(16);

        private void Awake()
        {
           
            if (_grid == null)
            {
                _grid = GridSystem.Instance;
                if (_grid == null)
                {
                    Debug.LogError("[BuildControllerV2] 缺少 GridSystem");
                    enabled = false; return;
                }
            }
            _overlay = _grid.overlayHighlight;
            _road = _grid.roadTilemap;
            _block = _grid.blockTilemap;
            if (_overlay == null)
                Debug.LogWarning("[BuildControllerV2] overlayHighlight 未绑定，无法显示高亮。");

            ClearOverlay();
        }

        private void OnDisable() => ClearOverlay();

        private void Update()
        {
            // 1) 热键进入/退出
            if (enableHotkeyToggle && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                if (!placing) TryEnterWithInspectorSelection();
                else ExitPlaceMode();
            }

            if (!placing) return;

            // 2) UI 打开或指针在 UI 上 → 不处理放置点击（但仍更新预览）
            bool blockInput = (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
                              || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());

            // 3) 根据鼠标位置推导原点格（Z=0）
            var world = ScreenToWorldOnZ0(Mouse.current?.position.ReadValue() ?? Vector2.zero);
            var cell = _grid.unityGrid.WorldToCell(world);
            cell.z = 0;

            // 4) 计算/绘制高亮
            currentOrigin = cell;
            currentValid = ValidateAt(cell, selectedConfig ? selectedConfig.size : 1, _invalidCells);
            PaintPreview(cell, selectedConfig ? selectedConfig.size : 1, currentValid, _invalidCells);

            if (blockInput) return; // 不响应点击

            // 5) 左键确认 / 右键取消
            if (mouseConfirmCancel && Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) OnClickConfirm();
                if (Mouse.current.rightButton.wasPressedThisFrame) OnClickCancel();
            }
        }

        // —— 外部接口：由建造菜单调用 ——
        [Button("进入放置（用Inspector指定的Prefab+Config）")]
        public void TryEnterWithInspectorSelection()
        {
            if (selectedPrefab == null || selectedConfig == null)
            {
                Debug.LogWarning("[BuildControllerV2] 请选择 selectedPrefab + selectedConfig 再进入放置。");
                return;
            }
            EnterPlaceMode(selectedPrefab, selectedConfig);
        }

        public void EnterPlaceMode(Building prefab, BuildingConfig config)
        {
            selectedPrefab = prefab;
            selectedConfig = config;
            placing = true;
            GameInputRouter.Instance?.Push(this);
            if (verbose) Debug.Log($"[BuildControllerV2] 进入放置：{config.displayName}({config.id})");
        }

        private void ExitPlaceMode()
        {
            placing = false;
            GameInputRouter.Instance?.Pop(this);
            ClearOverlay();
            if (verbose) Debug.Log("[BuildControllerV2] 退出放置");
        }

        // —— IInputLayer：ESC/Back —— 
        public bool OnBack()
        {
            if (!placing) return false;
            // 若未来有“待确认条”，这里先取消；当前直接退出
            ExitPlaceMode();
            return true;
        }

        // —— 点击确认/取消 ——
        private void OnClickConfirm()
        {
            if (!placing) return;
            if (selectedPrefab == null || selectedConfig == null)
            {
                Debug.LogWarning("[BuildControllerV2] 未选择 Prefab/Config。");
                return;
            }
            if (!currentValid)
            {
                Debug.LogWarning("[BuildControllerV2] 放置位置不合法。");
                return;
            }

            var placed = BuildingManager.Instance.Place(selectedPrefab, selectedConfig, currentOrigin);
            if (placed != null)
            {
                // 强制世界 Z=0（保险）
                var wp = placed.transform.position; wp.z = 0f; placed.transform.position = wp;
                if (verbose) Debug.Log($"[BuildControllerV2] 已放置：{selectedConfig.displayName} at {currentOrigin}");
            }
            else
            {
                Debug.LogWarning("[BuildControllerV2] Place() 失败，检查 BuildingManager 实现。");
            }

            ClearOverlay();
            // 保持在放置模式（批量放置）；如需一次即退，可改为 ExitPlaceMode();
        }

        private void OnClickCancel()
        {
            if (!placing) return;
            ExitPlaceMode();
        }

        // —— 位置验证 —— 
        /// <summary>
        /// 返回是否全部合法；invalidCells 输出具体非法格（越界/障碍/已有建筑/道路）
        /// </summary>
        private bool ValidateAt(Vector3Int origin, int size, List<Vector3Int> invalidCellsOut)
        {
            invalidCellsOut.Clear();
            bool ok = true;

            for (int dx = 0; dx < size; dx++)
                for (int dy = 0; dy < size; dy++)
                {
                    var c = new Vector3Int(origin.x + dx, origin.y + dy, 0);

                    // 越界
                    if (!_grid.IsInside(c)) { invalidCellsOut.Add(c); ok = false; continue; }

                    // 障碍/占用
                    if (_block != null && _block.HasTile(c)) { invalidCellsOut.Add(c); ok = false; continue; }
                    if (GridIndex.Instance.GetBuildingAt(c) != null) { invalidCellsOut.Add(c); ok = false; continue; }

                    // 不可放置在道路
                    if (_road != null && _road.HasTile(c)) { invalidCellsOut.Add(c); ok = false; continue; }
                }
            return ok;
        }

        // —— 绘制高亮 —— 
        private void PaintPreview(Vector3Int origin, int size, bool valid, List<Vector3Int> invalidCells)
        {
            if (_overlay == null) return;

            // 先清掉上一次
            if (_lastPaint.Count > 0)
            {
                foreach (var c in _lastPaint) _overlay.SetTile(c, null);
                _lastPaint.Clear();
            }

            // 覆盖整块（绿色 或 浅红）
            TileBase fill = valid ? tileValidGreen : tileInvalidLightRed;
            for (int dx = 0; dx < size; dx++)
                for (int dy = 0; dy < size; dy++)
                {
                    var c = new Vector3Int(origin.x + dx, origin.y + dy, 0);
                    _overlay.SetTile(c, fill);
                    _lastPaint.Add(c);
                }

            // 针对具体不合法格，覆盖深红
            if (!valid && invalidCells != null && invalidCells.Count > 0)
            {
                foreach (var bad in invalidCells)
                {
                    if (_grid.IsInside(bad))
                    {
                        _overlay.SetTile(bad, tileInvalidDeepRed);
                        _lastPaint.Add(bad);
                    }
                }
            }
        }

        private void ClearOverlay()
        {
            if (_overlay == null) return;
            if (_lastPaint.Count == 0) { _overlay.ClearAllTiles(); return; }
            foreach (var c in _lastPaint) _overlay.SetTile(c, null);
            _lastPaint.Clear();
        }

        // —— 工具 —— 
        private Vector3 ScreenToWorldOnZ0(Vector2 screenPos)
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            var sp = new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z);
            var w = cam.ScreenToWorldPoint(sp);
            w.z = 0f;
            return w;
        }

        // —— 诊断 —— 
        [Button("诊断：当前原点格是否合法")]
        private void Debug_CheckHere()
        {
            var cell = currentOrigin;
            var ok = ValidateAt(cell, selectedConfig ? selectedConfig.size : 1, _invalidCells);
            Debug.Log(ok ? "[BuildControllerV2] 合法" : $"[BuildControllerV2] 不合法：{_invalidCells.Count} 个非法格");
        }
    }
}
