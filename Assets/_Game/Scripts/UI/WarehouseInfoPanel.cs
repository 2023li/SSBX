using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    public class WarehouseInfoPanel : BuildingPanelBase
    {
        [LabelText("标题")] public TextMeshProUGUI txtTitle;
        [LabelText("正文")] public TextMeshProUGUI txtBody;
        [LabelText("关闭按钮")] public Button btnClose;
        [LabelText("最多打印物资条目")] public int maxLines = 40;

        private void Awake()
        {
            if (btnClose) btnClose.onClick.AddListener(() => UIManager.Instance.CloseTopPanel());
        }

        protected override void OnOpen(Building b)
        {
            var w = b as WarehouseBuilding;
            var g = b as GranaryBuilding;
            var inv = w ? w.inventory : (g ? g.inventory : null);

            if (txtTitle) txtTitle.text = b.config.displayName;
            if (inv == null) { if (txtBody) txtBody.text = "（无库存）"; return; }

            var sb = new StringBuilder();
            int printed = 0;
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
            {
                int v = inv.Get(t);
                if (v > 0) { sb.AppendLine($"{t}：{v}"); printed++; if (printed >= maxLines) break; }
            }
            if (printed == 0) sb.AppendLine("（空）");
            if (txtBody) txtBody.text = sb.ToString();
        }
    }
}
