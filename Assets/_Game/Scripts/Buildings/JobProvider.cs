using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    public enum JobType { Basic, Service, Culture, Industry }

    /// <summary>岗位提供者：挂在可提供岗位的建筑上（医院/警局/工厂等）。</summary>
    public class JobProvider : MonoBehaviour
    {
        [LabelText("岗位类型")] public JobType jobType = JobType.Service;
        [LabelText("岗位总数"), Min(0)] public int jobSlots = 5;
        [LabelText("优先级/薪资(高优先)"), Min(0)] public int priority = 1;
        [LabelText("通勤容忍上限L"), Min(0f)] public float commuteMaxCost = 10f;

        [ReadOnly, LabelText("已占用岗位")] public int occupied = 0;

        public int FreeSlots => Mathf.Max(0, jobSlots - occupied);

        [Button("清空岗位占用")]
        public void Debug_Clear() => occupied = 0;
    }
}
