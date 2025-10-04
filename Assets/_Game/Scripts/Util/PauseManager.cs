using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 暂停管理器：作为常驻输入层（栈底）。无其它层时 ESC → 打开/关闭暂停。
    /// 统一管理暂停状态与 Time.timeScale；暂停面板本身不改时间。
    /// </summary>
    public class PauseManager : MonoBehaviour, IInputLayer
    {
        public static PauseManager Instance { get; private set; }

        [LabelText("暂停面板Prefab")] public PausePanel pausePanelPrefab;

        [ReadOnly, LabelText("当前是否暂停")] public bool isPaused;
        private float _lastTimeScale = 1f;
        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 将自己作为基础输入层压栈
            GameInputRouter.Instance?.Push(this);
        }

        private void Start()
        {
            TrySubscribeUIEvents();
        }

        private void TrySubscribeUIEvents()
        {
            if (_subscribed) return;
            if (UIManager.Instance == null) return;

            UIManager.Instance.PanelClosed += OnAnyPanelClosed;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_subscribed && UIManager.Instance != null)
                UIManager.Instance.PanelClosed -= OnAnyPanelClosed;
        }

        // —— IInputLayer：当没有更高层时，ESC 由我处理 —— 
        public bool OnBack()
        {
            if (!isPaused) OpenPause();
            else Resume();
            return true;
        }

        [Button("打开暂停面板")]
        public void OpenPause()
        {
            if (isPaused) return;
            if (pausePanelPrefab == null) { Debug.LogWarning("[PauseManager] 未设置 pausePanelPrefab。"); return; }

            // 冻结时间（暂停逻辑只在此处做）
            _lastTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isPaused = true;

            UIManager.Instance.OpenPanel(pausePanelPrefab);
        }

        [Button("继续游戏")]
        public void Resume()
        {
            if (!isPaused) return;

            // 关闭顶层面板（应为暂停面板），实际恢复在 OnAnyPanelClosed 统一执行
            if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
                UIManager.Instance.CloseTopPanel();
            else
                RestoreFromPause(); // 兜底：没有UI但处于暂停标记，直接恢复
        }

        private void OnAnyPanelClosed(BuildingPanelBase panel)
        {
            // 若关闭的是暂停面板，且标记为暂停中→恢复
            if (panel is PausePanel && isPaused)
            {
                RestoreFromPause();
            }
        }

        private void RestoreFromPause()
        {
            isPaused = false;
            Time.timeScale = _lastTimeScale;
        }
    }
}
