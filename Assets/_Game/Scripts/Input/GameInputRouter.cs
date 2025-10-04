using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

namespace SSBX
{
    public interface IInputLayer
    {
        // 返回 true 表示已消费该“后退”输入
        bool OnBack();
        // 可按需扩展：bool OnConfirm(); bool OnCancel(); 等
    }

    /// <summary>
    /// 全局输入路由：集中轮询 ESC/Back 等输入，将事件派发给“输入层栈”顶层。
    /// </summary>
    [DefaultExecutionOrder(-10000)]  
    public class GameInputRouter : MonoBehaviour
    {
        public static GameInputRouter Instance { get; private set; }

        [ReadOnly, LabelText("层数")] public int layerCount;
        private readonly Stack<IInputLayer> _layers = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // 统一处理 ESC / Android 返回键
            bool esc = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                       || Input.GetKeyDown(KeyCode.Escape);
            if (!esc) return;

            if (_layers.Count == 0)
            {
                Debug.Log(0);
                return;
             
            }

            var top = _layers.Peek();
            if (top != null)
            {
                bool consumed = top.OnBack();
                // 未消费则可考虑向下传递（通常不需要）
            }
        }

        [Button("Push 示例(调试)")]
        public void Debug_PushSelf() => Push(this as IInputLayer); // 仅示例

        public void Push(IInputLayer layer)
        {
            if (layer == null) return;
            _layers.Push(layer);
            layerCount = _layers.Count;
        }

        public void Pop(IInputLayer layer)
        {
            // 安全弹栈：只允许“最上层”自己弹出
            if (_layers.Count == 0) return;
            if (!ReferenceEquals(_layers.Peek(), layer))
            {
                Debug.LogWarning("[InputRouter] 尝试弹出非顶层输入层，被忽略。");
                return;
            }
            _layers.Pop();
            layerCount = _layers.Count;
        }

        public bool HasAnyLayer => _layers.Count > 0;
        public IInputLayer Top => _layers.Count > 0 ? _layers.Peek() : null;
    }
}
