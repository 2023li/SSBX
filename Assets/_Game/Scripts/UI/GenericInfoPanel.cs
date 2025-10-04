using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    public class GenericInfoPanel : BuildingPanelBase
    {
        [LabelText("标题")] public TextMeshProUGUI txtTitle;
        [LabelText("正文")] public TextMeshProUGUI txtBody;
        [LabelText("关闭按钮")] public Button btnClose;

        private void Awake()
        {
            if (btnClose) btnClose.onClick.AddListener(() => UIManager.Instance.CloseTopPanel());
        }

        protected override void OnOpen(Building b)
        {
            if (txtTitle) txtTitle.text = b.config ? b.config.displayName : b.name;
            var sb = new StringBuilder();
            if (b.config)
            {
                sb.AppendLine($"• 占格：{b.config.size}×{b.config.size}");
                sb.AppendLine($"• 维护费/回合：{b.config.maintenanceGold}");
                if (b.config.jobSlots > 0) sb.AppendLine($"• 岗位上限：{b.config.jobSlots}");
            }
            if (txtBody) txtBody.text = sb.ToString();
        }
    }
}
