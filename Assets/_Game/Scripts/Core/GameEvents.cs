using System;

namespace SSBX
{
    /// <summary>
    /// 全局事件总线：建筑完工/拆除等。
    /// </summary>
    public static class GameEvents
    {
        public static event Action<Building> OnBuildingConstructed;
        public static event Action<Building> OnBuildingDestroyed;

        public static void RaiseBuildingConstructed(Building b) => OnBuildingConstructed?.Invoke(b);
        public static void RaiseBuildingDestroyed(Building b) => OnBuildingDestroyed?.Invoke(b);
    }
}
