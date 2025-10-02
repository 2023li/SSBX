using System;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SSBX
{
    public enum BuildingCategory { Common, Economy, Industry, Culture, Military, Wonder, Service, Storage }

    [Serializable]
    public struct ItemCost
    {
        public ResourceType type;
        public int amount;
    }

#if ODIN_INSPECTOR
    [InlineEditor]
#endif
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "SSBX/Building Config")]
    public class BuildingConfig : ScriptableObject
    {
#if ODIN_INSPECTOR
        [TitleGroup("基础"), PropertyOrder(0)]
#endif
        public string id;
        public string displayName;
        public BuildingCategory category = BuildingCategory.Common;
        [Min(1)] public int size = 1;
        [Min(1)] public int buildTurns = 1;
        public float maintenanceGold = 0;

#if ODIN_INSPECTOR
        [TitleGroup("建造消耗"), TableList]
#endif
        public List<ItemCost> buildCosts = new List<ItemCost>();

#if ODIN_INSPECTOR
        [TitleGroup("服务影响")]
#endif
        public int serviceRadius = 0;
        public int medicalLevel = 0;
        public int securityBonus = 0;
        public int beautifyBonus = 0;
        public int jobSlots = 0;

#if ODIN_INSPECTOR
        [TitleGroup("移动取料")]
#endif
        public float movePointsToFetch = 3f;

#if ODIN_INSPECTOR
        [TitleGroup("等级与前置")]
#endif
        [Min(1)] public int level = 1;
        public string[] requiredTechIds;
        public int requiredFaith = 0;
    }
}
