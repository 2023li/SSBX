using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// UI总管（唯一UI输入层）：维护面板栈；ESC/Back关闭顶层面板；提供事件。
    /// 不在 Update() 轮询按键，按键由 GameInputRouter 分发到 OnBack()。
    /// </summary>
    public class UIManager : MonoBehaviour, IInputLayer
    {
        public static UIManager Instance { get; private set; }

        [LabelText("主Canvas")] public Canvas canvas;
        [LabelText("信息路由器")] public BuildingInfoRouter infoRouter;

        [ReadOnly, LabelText("打开中的面板数")] public int openCount;

        // —— 事件：供 PauseManager 等订阅 —— 
        public event Action<BuildingPanelBase> PanelOpened;
        public event Action<BuildingPanelBase> PanelClosed;

        private readonly Stack<BuildingPanelBase> _stack = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsAnyPanelOpen => _stack.Count > 0;

        /// <summary>按建筑打开信息面板（支持地址路由/手动映射）。</summary>
        public async void OpenInfo(Building b)
        {
            if (canvas == null || infoRouter == null || b == null) return;

            var panel = await infoRouter.CreatePanelAsync(b, canvas.transform);
            if (panel == null) return;

            _stack.Push(panel);
            openCount = _stack.Count;
            panel.Open(b);

            PanelOpened?.Invoke(panel);

            if (_stack.Count == 1) GameInputRouter.Instance?.Push(this);
        }

        /// <summary>通用打开任意面板预制体（用于暂停面板等“非建筑”UI）。</summary>
        public void OpenPanel(BuildingPanelBase panelPrefab)
        {
            if (canvas == null || panelPrefab == null) return;

            var panel = Instantiate(panelPrefab, canvas.transform);
            _stack.Push(panel);
            openCount = _stack.Count;
            panel.Open(null);

            PanelOpened?.Invoke(panel);

            if (_stack.Count == 1) GameInputRouter.Instance?.Push(this);
        }

        [Button("关闭顶层面板")]
        public void CloseTopPanel()
        {
            if (_stack.Count == 0) return;

            var top = _stack.Pop();
            openCount = _stack.Count;

            PanelClosed?.Invoke(top);

            if (top != null)
            {
                top.Close();
                Destroy(top.gameObject);
            }

            if (_stack.Count == 0) GameInputRouter.Instance?.Pop(this);
        }

        [Button("关闭所有面板")]
        public void CloseAll()
        {
            while (_stack.Count > 0) CloseTopPanel();
        }

        // —— IInputLayer：ESC/Back → 关闭当前顶层面板 —— 
        public bool OnBack()
        {
            if (_stack.Count > 0) { CloseTopPanel(); return true; }
            return false;
        }
    }
}
