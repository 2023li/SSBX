using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using Sirenix.OdinInspector;
using Cinemachine;

namespace SSBX
{
    /// <summary>
    /// 启动器V2：适配最简场景，一键完成“发现→挂载→绑定→初始化”。
    /// - 绑定 VirtualCamera 的 Confiner2D 边界（地图边界PolygonCollider2D）
    /// - 为 VirtualCamera 挂 CinemachinePanZoomController（PC+移动端手势）
    /// - 在 MapGrid 上挂 GridSystem 并自动绑定子Tilemap
    /// - 确保核心系统存在（GridIndex/Turn/Kingdom/AreaEffect/Employment/Transport/UIManager/InfoRouter/Heatmap）
    /// - 初始化顺序：Addressables(可选) → BakeCells → RebuildAll → AssignAll → 刷新热力层
    /// </summary>
    public class GameBootstrapV2 : MonoBehaviour
    {
        [Title("场景对象（自动发现，不用手填）")]
        [ReadOnly, LabelText("Main Camera")] public Camera mainCamera;
        [ReadOnly, LabelText("Virtual Camera")] public CinemachineVirtualCamera vcam;
        [ReadOnly, LabelText("Confiner2D")] public CinemachineConfiner2D confiner2D;
        [ReadOnly, LabelText("地图边界")] public PolygonCollider2D mapBoundary;
        [ReadOnly, LabelText("MapGrid")] public GameObject mapGridGO;
        [ReadOnly, LabelText("MainCanvas")] public Canvas mainCanvasRef;

        [Title("Tilemap（自动绑定）")]
        [ReadOnly, LabelText("Ground")] public Tilemap groundTilemap;
        [ReadOnly, LabelText("Road")] public Tilemap roadTilemap;
        [ReadOnly, LabelText("Overlay_Highlight")] public Tilemap overlayHighlight;
        [ReadOnly, LabelText("Overlay_Heatmap")] public Tilemap overlayHeatmap;
        [ReadOnly, LabelText("Block")] public Tilemap blockTilemap;

        [Title("初始化步骤")]
        [LabelText("启动时初始化 Addressables")] public bool initAddressables = false;
        [LabelText("启动时Bake格子")] public bool bakeCellsOnStart = true;
        [LabelText("启动时重算地块影响")] public bool rebuildAreaEffectsOnStart = true;
        [LabelText("启动时分配就业")] public bool assignJobsOnStart = true;
        [LabelText("启动时刷新热力层")] public bool rebuildHeatmapOnStart = true;

        [Title("相机控制")]
        [LabelText("为VCam自动挂平移缩放控制")] public bool attachPanZoomToVcam = true;
        [LabelText("优先使用Confiner2D钳制")] public bool preferConfiner2D = true;

        [Title("日志")]
        [LabelText("打印绑定报告")] public bool verbose = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 1) 场景发现
            FindSceneObjects();

            // 2) 绑定 CinemachineConfiner2D 的边界
            BindConfiner2D();

            // 3) 为 VCam 安装 CinemachinePanZoomController（如果缺失）
            AttachPanZoomIfNeeded();

            // 4) 在 MapGrid 上挂 GridSystem 并绑定 Tilemaps
            BindGridSystemOnMapGrid();

            // 5) 确保核心系统存在（若缺失就创建）
            EnsureCoreSystems();

            // 6) 绑定 UI 管理（UIManager + BuildingInfoRouter）
            EnsureUIStack();

            if (verbose) Debug.Log("[Bootstrap] Awake 完成，进入 Start 初始化…");
        }

        private async void Start()
        {
            // 7) 初始化 Addressables（可选）
            if (initAddressables && AddressableService.Instance != null)
                await AddressableService.Instance.InitializeAsync();

            // 8) 初始化顺序：Bake → RebuildArea → AssignJobs → Heatmap
            if (bakeCellsOnStart) GridSystem.Instance?.BakeCells();
            if (rebuildAreaEffectsOnStart) AreaEffectSystem.Instance?.RebuildAll();
            if (assignJobsOnStart) EmploymentSystem.Instance?.AssignAll();
            if (rebuildHeatmapOnStart)
            {
                var heat = FindObjectOfType<HeatmapController>();
                heat?.Rebuild();
            }

            if (verbose) Report();
        }

        // ================== 场景发现 & 绑定 ==================

        [Button("一键发现/绑定(编辑器可点)")]
        public void FindSceneObjects()
        {
            mainCamera = Camera.main;
            vcam = FindObjectOfType<CinemachineVirtualCamera>();
            confiner2D = vcam ? vcam.GetComponent<CinemachineConfiner2D>() : null;

            // 地图边界：名字叫“地图边界”的 PolygonCollider2D
            var allPC2D = FindObjectsOfType<PolygonCollider2D>();
            foreach (var pc in allPC2D)
                if (pc.gameObject.name.Contains("地图边界")) { mapBoundary = pc; break; }

            // MapGrid（含子Tilemap）
            mapGridGO = GameObject.Find("MapGrid");
            if (mapGridGO != null)
            {
                groundTilemap = FindChildTilemap(mapGridGO, "groundTilemap");
                roadTilemap = FindChildTilemap(mapGridGO, "roadTilemap");
                overlayHighlight = FindChildTilemap(mapGridGO, "Overlay_Highlight");
                overlayHeatmap = FindChildTilemap(mapGridGO, "Overlay_Heatmap");
                blockTilemap = FindChildTilemap(mapGridGO, "blockTilemap");
            }

            // MainCanvas
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
                if (c.gameObject.name.Contains("MainCanvas")) { mainCanvasRef = c; break; }
            if (mainCanvasRef == null && canvases.Length > 0) mainCanvasRef = canvases[0];
        }

        private Tilemap FindChildTilemap(GameObject parent, string childName)
        {
            var tr = parent.transform.Find(childName);
            if (tr) return tr.GetComponent<Tilemap>();
            // 兜底：遍历
            foreach (var tm in parent.GetComponentsInChildren<Tilemap>(true))
                if (tm.gameObject.name == childName) return tm;
            return null;
        }

        private void BindConfiner2D()
        {
            if (vcam == null) return;

            // 确保 Confiner2D 存在
            if (confiner2D == null)
                confiner2D = vcam.gameObject.AddComponent<CinemachineConfiner2D>();

            // 绑定地图边界
            if (mapBoundary != null)
            {
                confiner2D.m_BoundingShape2D = mapBoundary;
                confiner2D.m_Damping = 0f;
                if (verbose) Debug.Log("[Bootstrap] Confiner2D 已绑定 地图边界");
            }
            else
            {
                if (verbose) Debug.LogWarning("[Bootstrap] 找不到“地图边界(PolygonCollider2D)”，Confiner2D 将无效。");
            }
        }

        private void AttachPanZoomIfNeeded()
        {
            if (!attachPanZoomToVcam || vcam == null) return;
            var ctrl = vcam.GetComponent<CinemachinePanZoomController>();
            if (ctrl == null)
            {
                ctrl = vcam.gameObject.AddComponent<CinemachinePanZoomController>();
                ctrl.preferConfiner2D = preferConfiner2D;
                ctrl.ignoreWhenPointerOverUI = true;
                ctrl.disableWhenAnyPanelOpen = true;
                ctrl.minOrthoSize = 3f;
                ctrl.maxOrthoSize = 20f;
                if (verbose) Debug.Log("[Bootstrap] 已为 VCam 添加 CinemachinePanZoomController");
            }
        }

        private void BindGridSystemOnMapGrid()
        {
            if (mapGridGO == null)
            {
                Debug.LogError("[Bootstrap] 场景缺少 MapGrid 对象，无法绑定 GridSystem。");
                return;
            }

            var unityGrid = mapGridGO.GetComponent<UnityEngine.Grid>();
            if (unityGrid == null)
            {
                Debug.LogError("[Bootstrap] MapGrid 上缺少 UnityEngine.Grid 组件。");
                return;
            }

            // 确保 GridSystem 挂在 MapGrid 上
            var gs = mapGridGO.GetComponent<GridSystem>();
            if (gs == null) gs = mapGridGO.AddComponent<GridSystem>();

            // === 这里假设你的 GridSystem 暴露这些字段/属性（与我们之前脚本一致）===
            gs.unityGrid = unityGrid;
            gs.groundTilemap = groundTilemap;
            gs.roadTilemap = roadTilemap;
            gs.overlayHighlight = overlayHighlight;
            gs.overlayHeatmap = overlayHeatmap;
            gs.blockTilemap = blockTilemap;

            if (verbose) Debug.Log("[Bootstrap] GridSystem 已绑定 MapGrid 与子Tilemap");
        }

       
        private void EnsureCoreSystems()
        {
            EnsureSingleton<GridIndex>("GridIndex");
            EnsureSingleton<BuildingManager>("BuildingManager");
            EnsureSingleton<TurnSystem>("TurnSystem");
            EnsureSingleton<KingdomStats>("KingdomStats");
            EnsureSingleton<AreaEffectSystem>("AreaEffectSystem");
            EnsureSingleton<EmploymentSystem>("EmploymentSystem");
            EnsureSingleton<TransportRouteSystem>("TransportRouteSystem");

            // HeatmapController：需要 Overlay_Heatmap Tilemap
            var heat = FindObjectOfType<HeatmapController>();
            if (heat == null)
            {
                var go = new GameObject("HeatmapController");
                go.transform.SetParent(transform);
                heat = go.AddComponent<HeatmapController>();
            }
            heat.overlayTilemap = overlayHeatmap;
        }

        private void EnsureUIStack()
        {
            // UIManager
            var ui = FindObjectOfType<UIManager>();
            if (ui == null)
            {
                var go = new GameObject("UIManager");
                go.transform.SetParent(transform);
                ui = go.AddComponent<UIManager>();
            }
            ui.canvas = mainCanvasRef;

            // BuildingInfoRouter（默认先用“手动模式”以兼容你现在未上 Addressables 的情况）
            var router = FindObjectOfType<BuildingInfoRouter>();
            if (router == null)
            {
                router = ui.gameObject.AddComponent<BuildingInfoRouter>();
                router.useAddressables = false; // 先手动，等你接入 Addressables 再切换
            }
            // 这里不强绑定具体面板Prefab；当你准备好面板后，可在 Inspector 配置
        }

        private void EnsureSingleton<T>(string name) where T : MonoBehaviour
        {
            var inst = FindObjectOfType<T>();
            if (inst == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform);
                go.AddComponent<T>();
            }
        }

        [Button("打印绑定报告")]
        public void Report()
        {
            Debug.Log($"[Bootstrap/Report] " +
                      $"\n MainCamera={(mainCamera ? mainCamera.name : "(无)")} " +
                      $"\n VCam={(vcam ? vcam.name : "(无)")}, Confiner2D={(confiner2D ? "是" : "否")} " +
                      $"\n MapGrid={(mapGridGO ? mapGridGO.name : "(无)")}" +
                      $"\n  ├ groundTilemap={(groundTilemap ? groundTilemap.name : "(无)")}" +
                      $"\n  ├ roadTilemap={(roadTilemap ? roadTilemap.name : "(无)")}" +
                      $"\n  ├ Overlay_Highlight={(overlayHighlight ? overlayHighlight.name : "(无)")}" +
                      $"\n  ├ Overlay_Heatmap={(overlayHeatmap ? overlayHeatmap.name : "(无)")}" +
                      $"\n  └ blockTilemap={(blockTilemap ? blockTilemap.name : "(无)")}" +
                      $"\n Canvas={(mainCanvasRef ? mainCanvasRef.name : "(无)")}");
        }
    }
}
