using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 暂停管理器：常驻输入层。没有其它输入层时，ESC 打开/关闭暂停面板。
    /// </summary>
    public class PauseManager : MonoBehaviour, IInputLayer
    {
        public static PauseManager Instance { get; private set; }

        [LabelText("暂停面板Prefab")] public PausePanel pausePanelPrefab;

        [ReadOnly, LabelText("当前是否暂停")] public bool isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 将自己作为“基础输入层”压栈（最底层）
            GameInputRouter.Instance?.Push(this);
        }

        /// <summary>ESC/Back：没有其它层时由我处理。</summary>
        public bool OnBack()
        {
            if (!isPaused) OpenPause();
            else Resume();
            return true; // 始终消费
        }

        [Button("打开暂停面板")]
        public void OpenPause()
        {
            if (isPaused) return;
            if (pausePanelPrefab == null)
            {
                Debug.LogWarning("[PauseManager] 未设置 pausePanelPrefab。");
                return;
            }
            UIManager.Instance.OpenPanel(pausePanelPrefab);
            isPaused = true;
        }

        [Button("继续游戏")]
        public void Resume()
        {
            if (!isPaused) return;
            UIManager.Instance.CloseTopPanel(); // 会触发 PausePanel.Close() 恢复时间
            isPaused = false;
        }
    }
}
