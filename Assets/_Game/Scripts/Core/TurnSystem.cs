using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSBX
{
    /// <summary>
    /// 回合系统（支持阻塞）。当存在阻塞时，“结束回合”按钮应禁用。
    /// </summary>
    public class TurnSystem : MonoBehaviour
    {
        public static TurnSystem Instance { get; private set; }

        public int CurrentTurn { get; private set; } = 1;

        // 阻塞：例如 战争中、未处理事件等
        private readonly Dictionary<object, string> _blockers = new Dictionary<object, string>();

        public event Action<int> OnTurnBegan;      // 传入当前回合编号
        public event Action<int> OnBeforeTurnEnd;  // 回合结束前（用于建筑/单位结算）
        public event Action<int> OnTurnEnded;      // 回合结束后（用于刷新UI等）

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsBlocked => _blockers.Count > 0;

        /// <summary>注册回合结束阻塞。owner可用任意对象标识。</summary>
        public void AddBlocker(object owner, string reason)
        {
            if (!_blockers.ContainsKey(owner))
                _blockers.Add(owner, reason);
        }

        /// <summary>移除阻塞。</summary>
        public void RemoveBlocker(object owner)
        {
            if (_blockers.ContainsKey(owner))
                _blockers.Remove(owner);
        }

        /// <summary>尝试结束当前回合。</summary>
        public void RequestEndTurn()
        {
            if (IsBlocked)
            {
                Debug.LogWarning("当前存在回合结束阻塞，无法结束回合。");
                return;
            }
            DoEndTurn();
        }

        private void DoEndTurn()
        {
            OnBeforeTurnEnd?.Invoke(CurrentTurn);

            CurrentTurn++;
            OnTurnEnded?.Invoke(CurrentTurn - 1);
            OnTurnBegan?.Invoke(CurrentTurn);
        }
    }
}
