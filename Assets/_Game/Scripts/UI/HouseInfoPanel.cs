using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    public class HouseInfoPanel : BuildingPanelBase
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
            var h = b as HouseBuilding;
            if (txtTitle) txtTitle.text = h ? $"{h.config.displayName}（Lv{h.level}）" : b.config.displayName;

            var sb = new StringBuilder();
            if (h)
            {
                var g = GridSystem.Instance;
                int employable = Mathf.FloorToInt(h.curPopulation * h.employmentPercent * 0.01f);

                sb.AppendLine($"• 人口：{h.curPopulation}/{h.maxPopulation}");
                sb.AppendLine($"• 就业：{h.employed}/{employable}（失业：{h.unemployed}）");
                sb.AppendLine($"• 幸福阈值：Lv1>{h.happyLv1}% Lv2>{h.happyLv2}% Lv3>{h.happyLv3}% Lv4>{h.happyLv4}%");
                sb.AppendLine($"• 地块：医疗Lv={g.GetMedical(h.originCell)}｜治安Lv={g.GetSecurity(h.originCell)}｜美化={g.GetBeautify(h.originCell)}");
                sb.AppendLine($"• 取料移动力：{h.fetchMovePoints}｜就业通勤L：{h.employmentMovePoints}");
                sb.AppendLine($"• 奢侈候选：{string.Join("，", h.luxuryCandidates)}（Lv4需≥{h.luxKindsNeededLv4}种）");
            }
            if (txtBody) txtBody.text = sb.ToString();
        }
    }
}
