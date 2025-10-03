using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 就业面板：显示全局就业率与列表明细，支持“重新分配”与刷新。
    /// </summary>
    public class EmploymentPanelUI : MonoBehaviour
    {
        [LabelText("标题文本")] public TextMeshProUGUI txtHeader;
        [LabelText("列表文本(简版)")] public TextMeshProUGUI txtList;
        [LabelText("按钮:重新分配")] public Button btnReassign;
        [LabelText("按钮:刷新")] public Button btnRefresh;

        private void Awake()
        {
            if (btnReassign) btnReassign.onClick.AddListener(OnClickReassign);
            if (btnRefresh) btnRefresh.onClick.AddListener(RefreshNow);
        }

        private void OnEnable() => RefreshNow();

        [Button("刷新")]
        public void RefreshNow()
        {
            var emp = EmploymentSystem.Instance;
            if (emp == null) return;

            float rate = emp.ComputeGlobalEmploymentRate();
            string tight = rate >= emp.overEmploymentThreshold ? "（用工紧张）" : "";

            if (txtHeader)
                txtHeader.text = $"全局就业率：{rate * 100f:0.0}% {tight}｜Δ幸福/回合≈{emp.employmentToHappinessDelta.Evaluate(rate):+0;-0;0}";

            // 简版列表（文本）：居民房与岗位提供者
            var sb = new StringBuilder();
            var houses = GameObject.FindObjectsOfType<HouseBuilding>();
            foreach (var h in houses)
            {
                int employable = Mathf.FloorToInt(h.curPopulation * h.employmentPercent * 0.01f);
                sb.AppendLine($"[居民房]{h.name}  就业:{h.employed}/{employable}  人口:{h.curPopulation}");
            }
            var providers = GameObject.FindObjectsOfType<JobProvider>();
            foreach (var p in providers)
            {
                sb.AppendLine($"[岗位]{p.name}  占用:{p.occupied}/{p.jobSlots}  级别:{p.priority}  通勤L≤{p.commuteMaxCost}");
            }
            if (txtList) txtList.text = sb.ToString();
        }

        private void OnClickReassign()
        {
            EmploymentSystem.Instance.AssignAll();
            RefreshNow();
        }
    }
}
