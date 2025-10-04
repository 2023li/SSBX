using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 暂停面板：纯UI。不要直接改 Time.timeScale，由 PauseManager 统一管理。
    /// </summary>
    public class PausePanel : BuildingPanelBase
    {
        [LabelText("标题文本")] public TextMeshProUGUI txtTitle;
        [LabelText("按钮：继续")] public Button btnResume;
        [LabelText("按钮：设置(预留)")] public Button btnSettings;
        [LabelText("按钮：退出游戏")] public Button btnQuit;

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
        }

        private void OnClickSettings()
        {
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
