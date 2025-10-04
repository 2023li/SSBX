using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    public class UIManager : MonoBehaviour, IInputLayer
    {
        public static UIManager Instance { get; private set; }

        [LabelText("主Canvas")] public Canvas canvas;
        [LabelText("信息路由器")] public BuildingInfoRouter infoRouter;

        [ReadOnly, LabelText("打开中的面板数")] public int openCount;

        private readonly Stack<BuildingPanelBase> _stack = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsAnyPanelOpen => _stack.Count > 0;

        public async void OpenInfo(Building b)
        {
            if (canvas == null || infoRouter == null || b == null) return;

            var panel = await infoRouter.CreatePanelAsync(b, canvas.transform);
            if (panel == null) return;

            _stack.Push(panel);
            openCount = _stack.Count;
            panel.Open(b);

            if (_stack.Count == 1) GameInputRouter.Instance?.Push(this);
        }


        public void OpenPanel(BuildingPanelBase panelPrefab)
        {
            if (canvas == null || panelPrefab == null) return;

            var panel = Instantiate(panelPrefab, canvas.transform);
            _stack.Push(panel);
            openCount = _stack.Count;

            panel.Open(null); // PausePanel会忽略传入的 Building

            // 第一个面板：注册为输入层顶
            if (_stack.Count == 1) GameInputRouter.Instance?.Push(this);
        }


        [Button("关闭顶层面板")]
        public void CloseTopPanel()
        {
            if (_stack.Count == 0) return;
            var top = _stack.Pop();
            if (top != null) { top.Close(); Destroy(top.gameObject); }
            openCount = _stack.Count;

            // 若栈清空，注销输入层
            if (_stack.Count == 0) GameInputRouter.Instance?.Pop(this);
        }

        [Button("关闭所有面板")]
        public void CloseAll()
        {
            while (_stack.Count > 0) CloseTopPanel();
        }

        // IInputLayer 实现：ESC/Back → 关闭最上层面板
        public bool OnBack()
        {
            if (_stack.Count > 0) { CloseTopPanel(); return true; }
            return false;
        }
    }
}
