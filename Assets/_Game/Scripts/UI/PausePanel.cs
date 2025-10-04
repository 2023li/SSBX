using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 暂停面板：ESC 打开/关闭。暂停时默认冻结时间，可配置是否锁定光标。
    /// </summary>
    public class PausePanel : BuildingPanelBase
    {
        [LabelText("标题文本")] public TextMeshProUGUI txtTitle;
        [LabelText("按钮：继续")] public Button btnResume;
        [LabelText("按钮：设置(预留)")] public Button btnSettings;
        [LabelText("按钮：退出游戏")] public Button btnQuit;

        [Title("暂停选项")]
        [LabelText("暂停时冻结 Time.timeScale")] public bool freezeTimeScale = true;
        [LabelText("暂停时锁定光标(不建议)")] public bool lockCursorOnPause = false;

        private float _lastTimeScale = 1f;

        private void Awake()
        {
            if (btnResume) btnResume.onClick.AddListener(() => PauseManager.Instance.Resume());
            if (btnSettings) btnSettings.onClick.AddListener(OnClickSettings);
            if (btnQuit) btnQuit.onClick.AddListener(OnClickQuit);
        }

        protected override void OnOpen(Building _)
        {
            gameObject.SetActive(true);
            if (txtTitle) txtTitle.text = "暂停";

            // 应用暂停效果
            if (freezeTimeScale)
            {
                _lastTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (lockCursorOnPause)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true; // 锁定但可见（一般仍不建议锁定，否则影响点击）
            }
        }

        public override void Close()
        {
            // 恢复暂停效果
            if (freezeTimeScale)
                Time.timeScale = _lastTimeScale;

            if (lockCursorOnPause)
                Cursor.lockState = CursorLockMode.None;

            base.Close();
        }

        private void OnClickSettings()
        {
            // 预留：打开设置面板（后续里程碑接入）
            Debug.Log("[PausePanel] 设置面板功能待接入。");
        }

        private void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
