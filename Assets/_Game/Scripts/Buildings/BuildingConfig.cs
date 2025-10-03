using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    public enum BuildingCategory { Common, Economy, Industry, Culture, Military, Wonder, Service, Storage }

    [Serializable]
    public struct ItemCost { public ResourceType type; public int amount; }

    [Serializable]
    public struct LevelRing
    {
        [LabelText("半径(格)")] public int radius;       // Chebyshev距离
        [LabelText("等级(1-3)")][Range(0, 3)] public int level; // 0=忽略
    }

    [CreateAssetMenu(fileName = "BC_建筑名", menuName = "SSBX/Building Config")]
    public class BuildingConfig : ScriptableObject
    {
        [TitleGroup("基础")]
        [LabelText("建筑ID(格式:BID_中文)")] public string id = "BID_示例";
        [LabelText("显示名")] public string displayName = "示例建筑";
        [LabelText("类别")] public BuildingCategory category = BuildingCategory.Common;
        [Min(1), LabelText("占格尺寸")] public int size = 1;
        [Min(1), LabelText("建造回合数")] public int buildTurns = 1;
        [LabelText("维护费/回合")] public float maintenanceGold = 0;

        [TitleGroup("建造消耗"), TableList]
        public List<ItemCost> buildCosts = new List<ItemCost>();

        [TitleGroup("（兼容旧版）单半径影响"), InfoBox("若下方“多环影响”不为空，系统将忽略这里", InfoMessageType.Info)]
        [LabelText("服务半径")] public int serviceRadius = 0;
        [LabelText("医疗等级(单半径)")][Range(0, 3)] public int medicalLevel = 0;
        [LabelText("治安加成(单半径)")][Range(0, 3)] public int securityBonus = 0;
        [LabelText("美化加成(可叠加)")] public int beautifyBonus = 0;

        [TitleGroup("岗位")]
        [LabelText("岗位数")] public int jobSlots = 0;

        [TitleGroup("取料")]
        [LabelText("取料移动力")] public float movePointsToFetch = 3f;

        [TitleGroup("前置")]
        [Min(1), LabelText("等级(如仓库/医院等级)")] public int level = 1;
        [LabelText("所需科技IDs")] public string[] requiredTechIds;
        [LabelText("所需信仰")] public int requiredFaith = 0;

        [TitleGroup("多环影响（新）")]
        [LabelText("医疗环(半径/等级, 取最高)")] public List<LevelRing> medicalRings = new();
        [LabelText("治安环(半径/等级, 取最高)")] public List<LevelRing> securityRings = new();
    }
}
