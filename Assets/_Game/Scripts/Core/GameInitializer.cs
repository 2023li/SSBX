using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 运行时初始化器：仅调用初始化流程（不创建对象），供你在运行期开关。
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [LabelText("开局Bake格子")] public bool bakeCellsOnStart = true;
        [LabelText("开局重算地块影响")] public bool rebuildAreaEffectsOnStart = true;
        [LabelText("开局分配就业")] public bool assignJobsOnStart = true;
        [LabelText("开局刷新热力层")] public bool rebuildHeatmapOnStart = true;

        private void Start()
        {
            if (bakeCellsOnStart) GridSystem.Instance?.BakeCells();
            if (rebuildAreaEffectsOnStart) AreaEffectSystem.Instance?.RebuildAll();
            if (assignJobsOnStart) EmploymentSystem.Instance?.AssignAll();

            if (rebuildHeatmapOnStart)
            {
                var heat = FindObjectOfType<HeatmapController>();
                heat?.Rebuild();
            }
        }

        [Button("立即执行(编辑器可点)")]
        public void RunNow()
        {
            Start();
        }
    }
}
