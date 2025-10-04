#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Cinemachine;

namespace SSBX
{
    public class SSBXSceneSetupWizard : EditorWindow
    {
        // —— 可自定义名称（与你当前场景命名一致即可） ——
        private string goMapGridName = "MapGrid";
        private string tmGroundName = "groundTilemap";
        private string tmRoadName = "roadTilemap";
        private string tmOverlayHLName = "Overlay_Highlight";
        private string tmOverlayHMName = "Overlay_Heatmap";
        private string tmBlockName = "blockTilemap";
        private string boundaryKeyword = "地图边界"; // 按名称关键字匹配 PolygonCollider2D
        private string canvasKeyword = "MainCanvas";

        // 选项
        private bool attachPanZoomToVCam = true;
        private bool preferConfiner2D = true;
        private bool createGameRootIfMissing = true;
        private bool routerUseAddressables = false; // 先关闭，保留手动路由

        [MenuItem("Tools/SSBX/场景体检与一键创建")]
        public static void Open()
        {
            var win = GetWindow<SSBXSceneSetupWizard>("SSBX 场景体检");
            win.minSize = new Vector2(420, 480);
        }

        private void OnGUI()
        {
            GUILayout.Label("命名约定（可按需修改以适配你的场景）", EditorStyles.boldLabel);
            goMapGridName = EditorGUILayout.TextField("MapGrid 名称", goMapGridName);
            tmGroundName = EditorGUILayout.TextField("地面 Tilemap", tmGroundName);
            tmRoadName = EditorGUILayout.TextField("道路 Tilemap", tmRoadName);
            tmOverlayHLName = EditorGUILayout.TextField("Overlay_Highlight", tmOverlayHLName);
            tmOverlayHMName = EditorGUILayout.TextField("Overlay_Heatmap", tmOverlayHMName);
            tmBlockName = EditorGUILayout.TextField("障碍 Tilemap", tmBlockName);
            boundaryKeyword = EditorGUILayout.TextField("地图边界 关键字", boundaryKeyword);
            canvasKeyword = EditorGUILayout.TextField("MainCanvas 关键字", canvasKeyword);

            EditorGUILayout.Space();
            GUILayout.Label("选项", EditorStyles.boldLabel);
            attachPanZoomToVCam = EditorGUILayout.Toggle("VCam 挂平移缩放控制", attachPanZoomToVCam);
            preferConfiner2D = EditorGUILayout.Toggle("优先 CinemachineConfiner2D", preferConfiner2D);
            createGameRootIfMissing = EditorGUILayout.Toggle("缺失则创建 Game 根", createGameRootIfMissing);
            routerUseAddressables = EditorGUILayout.Toggle("BuildingInfoRouter 用 Addressables", routerUseAddressables);

            EditorGUILayout.Space();
            if (GUILayout.Button("扫描并报告"))
                ScanReport();

            EditorGUILayout.Space();
            if (GUILayout.Button("创建 / 绑定所有缺失（可撤销）"))
                CreateAndBindAll();
        }

        // ———————————— 扫描与报告 ————————————
        private void ScanReport()
        {
            var mainCam = Camera.main;
            var vcam = FindObjectOfType<CinemachineVirtualCamera>();
            var confiner = vcam ? vcam.GetComponent<CinemachineConfiner2D>() : null;
            var mapGrid = GameObject.Find(goMapGridName);
            var canvas = FindCanvasByKeyword(canvasKeyword);
            var boundary = FindBoundaryCollider(boundaryKeyword);

            var ground = FindChildTilemap(mapGrid, tmGroundName);
            var road = FindChildTilemap(mapGrid, tmRoadName);
            var ovHL = FindChildTilemap(mapGrid, tmOverlayHLName);
            var ovHM = FindChildTilemap(mapGrid, tmOverlayHMName);
            var block = FindChildTilemap(mapGrid, tmBlockName);

            var gs = mapGrid ? mapGrid.GetComponent<GridSystem>() : null;

            Debug.Log(
                $"[SSBX/Scan]\n" +
                $"- MainCamera: {(mainCam ? mainCam.name : "(无)")}\n" +
                $"- VirtualCamera: {(vcam ? vcam.name : "(无)")}\n" +
                $"- Confiner2D: {(confiner ? "有" : "无")}\n" +
                $"- 地图边界(PolygonCollider2D): {(boundary ? boundary.name : "(未找到)")}\n" +
                $"- MapGrid: {(mapGrid ? mapGrid.name : "(未找到)")}\n" +
                $"  ├ {tmGroundName}: {(ground ? ground.name : "(未找到)")}\n" +
                $"  ├ {tmRoadName}: {(road ? road.name : "(未找到)")}\n" +
                $"  ├ {tmOverlayHLName}: {(ovHL ? ovHL.name : "(未找到)")}\n" +
                $"  ├ {tmOverlayHMName}: {(ovHM ? ovHM.name : "(未找到)")}\n" +
                $"  └ {tmBlockName}: {(block ? block.name : "(未找到)")}\n" +
                $"- GridSystem(on MapGrid): {(gs ? "有" : "无")}\n" +
                $"- Canvas: {(canvas ? canvas.name : "(未找到)")}\n" +
                $"- UIManager: {(FindObjectOfType<UIManager>() ? "有" : "无")}\n" +
                $"- BuildingInfoRouter: {(FindObjectOfType<BuildingInfoRouter>() ? "有" : "无")}\n" +
                $"- HeatmapController: {(FindObjectOfType<HeatmapController>() ? "有" : "无")}\n" +
                $"- GridIndex: {(FindObjectOfType<GridIndex>() ? "有" : "无")}\n" +
                $"- BuildingManager: {(FindObjectOfType<BuildingManager>() ? "有" : "无")}\n" +
                $"- TurnSystem: {(FindObjectOfType<TurnSystem>() ? "有" : "无")}\n" +
                $"- KingdomStats: {(FindObjectOfType<KingdomStats>() ? "有" : "无")}\n" +
                $"- AreaEffectSystem: {(FindObjectOfType<AreaEffectSystem>() ? "有" : "无")}\n" +
                $"- EmploymentSystem: {(FindObjectOfType<EmploymentSystem>() ? "有" : "无")}\n" +
                $"- TransportRouteSystem: {(FindObjectOfType<TransportRouteSystem>() ? "有" : "无")}\n" +
                $"- GameInputRouter: {(FindObjectOfType<GameInputRouter>() ? "有" : "无")}\n"
            );
        }

        // ———————————— 一键创建/绑定（编辑器） ————————————
        private void CreateAndBindAll()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            // Game 根
            var gameRoot = GameObject.Find("Game");
            if (gameRoot == null && createGameRootIfMissing)
            {
                gameRoot = new GameObject("Game");
                Undo.RegisterCreatedObjectUndo(gameRoot, "Create Game Root");
            }

            // MainCamera
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                camGo.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
                mainCam = cam;
            }
            if (mainCam.GetComponent<CinemachineBrain>() == null)
                Undo.AddComponent<CinemachineBrain>(mainCam.gameObject);

            // VCam
            var vcam = FindObjectOfType<CinemachineVirtualCamera>();
            if (vcam == null)
            {
                var go = new GameObject("Virtual Camera");
                vcam = Undo.AddComponent<CinemachineVirtualCamera>(go);
                vcam.m_Lens.Orthographic = true;
                if (gameRoot) go.transform.SetParent(gameRoot.transform);
            }

            // Confiner2D + 地图边界
            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null) confiner = Undo.AddComponent<CinemachineConfiner2D>(vcam.gameObject);
            var boundary = FindBoundaryCollider(boundaryKeyword);
            confiner.m_BoundingShape2D = boundary;

            // VCam 控制器
            if (attachPanZoomToVCam && vcam.GetComponent<CinemachinePanZoomController>() == null)
            {
                var ctrl = Undo.AddComponent<CinemachinePanZoomController>(vcam.gameObject);
                ctrl.preferConfiner2D = preferConfiner2D;
                ctrl.ignoreWhenPointerOverUI = true;
                ctrl.disableWhenAnyPanelOpen = true;
            }

            // Canvas
            var canvas = FindCanvasByKeyword(canvasKeyword);
            if (canvas == null)
            {
                var go = new GameObject("MainCanvas");
                canvas = Undo.AddComponent<Canvas>(go);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.AddComponent<UnityEngine.UI.CanvasScaler>(go);
                Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(go);
                if (gameRoot) go.transform.SetParent(gameRoot.transform);
            }
            // EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(es);
                Undo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(es);
                if (gameRoot) es.transform.SetParent(gameRoot.transform);
            }

            // MapGrid + 子Tilemap
            var mapGrid = GameObject.Find(goMapGridName);
            if (mapGrid == null)
            {
                mapGrid = new GameObject(goMapGridName);
                Undo.RegisterCreatedObjectUndo(mapGrid, "Create MapGrid");
                Undo.AddComponent<UnityEngine.Grid>(mapGrid);
                if (gameRoot) mapGrid.transform.SetParent(gameRoot.transform);
            }
            var ground = EnsureChildTilemap(mapGrid, tmGroundName);
            var road = EnsureChildTilemap(mapGrid, tmRoadName);
            var ovHL = EnsureChildTilemap(mapGrid, tmOverlayHLName);
            var ovHM = EnsureChildTilemap(mapGrid, tmOverlayHMName);
            var block = EnsureChildTilemap(mapGrid, tmBlockName);

            // GridSystem (绑定引用)
            var gs = mapGrid.GetComponent<GridSystem>();
            if (gs == null) gs = Undo.AddComponent<GridSystem>(mapGrid);
            gs.unityGrid = mapGrid.GetComponent<UnityEngine.Grid>();
            gs.groundTilemap = ground;
            gs.roadTilemap = road;
            gs.overlayHighlight = ovHL;
            gs.overlayHeatmap = ovHM;
            gs.blockTilemap = block;

            // HeatmapController
            var heat = FindObjectOfType<HeatmapController>();
            if (heat == null)
            {
                var go = new GameObject("HeatmapController");
                if (gameRoot) go.transform.SetParent(gameRoot.transform);
                heat = Undo.AddComponent<HeatmapController>(go);
            }
            heat.overlayTilemap = ovHM;

            // —— 核心系统（挂在 Game 根下） ——
            EnsureSystemOnChild<GridIndex>(gameRoot, "GridIndex");
            EnsureSystemOnChild<BuildingManager>(gameRoot, "BuildingManager");
            EnsureSystemOnChild<TurnSystem>(gameRoot, "TurnSystem");
            EnsureSystemOnChild<KingdomStats>(gameRoot, "KingdomStats");
            EnsureSystemOnChild<AreaEffectSystem>(gameRoot, "AreaEffectSystem");
            EnsureSystemOnChild<EmploymentSystem>(gameRoot, "EmploymentSystem");
            EnsureSystemOnChild<TransportRouteSystem>(gameRoot, "TransportRouteSystem");
            EnsureSystemOnChild<GameInputRouter>(gameRoot, "GameInputRouter");

            // —— UI 栈：UIManager + BuildingInfoRouter（可手动配置路由） ——
            var ui = FindObjectOfType<UIManager>();
            if (ui == null)
            {
                var go = new GameObject("UIManager");
                if (gameRoot) go.transform.SetParent(gameRoot.transform);
                ui = Undo.AddComponent<UIManager>(go);
                ui.canvas = canvas;
            }
            var router = FindObjectOfType<BuildingInfoRouter>();
            if (router == null)
            {
                router = Undo.AddComponent<BuildingInfoRouter>(ui.gameObject);
                router.useAddressables = routerUseAddressables; // 先 false，之后你可切换
            }

            // 标脏（保存提示）
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[SSBX/Setup] 已创建并绑定所有缺失系统。你现在可以在编辑器中手动配置 UIManager/Router 等参数。");
            Undo.CollapseUndoOperations(group);
        }

        // —— 工具函数 ——
        private Canvas FindCanvasByKeyword(string key)
        {
            var canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
                if (c.gameObject.name.Contains(key)) return c;
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private PolygonCollider2D FindBoundaryCollider(string keyword)
        {
            var all = GameObject.FindObjectsOfType<PolygonCollider2D>();
            foreach (var p in all)
                if (p.gameObject.name.Contains(keyword)) return p;
            return null;
        }

        private Tilemap FindChildTilemap(GameObject parent, string childName)
        {
            if (parent == null) return null;
            var tr = parent.transform.Find(childName);
            if (tr) return tr.GetComponent<Tilemap>();
            foreach (var tm in parent.GetComponentsInChildren<Tilemap>(true))
                if (tm.gameObject.name == childName) return tm;
            return null;
        }

        private Tilemap EnsureChildTilemap(GameObject parent, string childName)
        {
            var tm = FindChildTilemap(parent, childName);
            if (tm != null) return tm;
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform);
            tm = Undo.AddComponent<Tilemap>(go);
            Undo.AddComponent<TilemapRenderer>(go);
            return tm;
        }

        private T EnsureSystemOnChild<T>(GameObject parent, string name) where T : MonoBehaviour
        {
            var inst = GameObject.FindObjectOfType<T>();
            if (inst != null) return inst;

            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent.transform);
            inst = Undo.AddComponent<T>(go);
            return inst;
        }
    }
}
#endif
