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

        [Title("编辑器引用")]
        [LabelText("预制体(建造用)")] public Building prefab;

        [LabelText("信息面板(可选)")] public BuildingPanelBase infoPanelPrefab;

        [TitleGroup("基础")]
        [LabelText("建筑ID(格式:BID_中文)")] public string id = "BID_示例";
        [LabelText("显示名")] public string displayName = "示例建筑";
        [LabelText("类别")] public BuildingCategory category = BuildingCategory.Common;
        [Min(1), LabelText("占格尺寸")] public int size = 1;
        [Min(1), LabelText("建造回合数")] public int buildTurns = 1;
        [LabelText("维护费/回合")] public float maintenanceGold = 0;

        [TitleGroup("建造消耗"), TableList]
        public List<ItemCost> buildCosts = new List<ItemCost>();

        [TitleGroup("岗位")]
        [LabelText("岗位数")] public int jobSlots = 0;

        [TitleGroup("取料")]
        [LabelText("取料移动力")] public float movePointsToFetch = 3f;

        [TitleGroup("前置")]
        [Min(1), LabelText("等级(如仓库/医院等级)")] public int level = 1;
        [LabelText("所需科技IDs")] public string[] requiredTechIds;
        [LabelText("所需信仰")] public int requiredFaith = 0;

        [TitleGroup("多环影响")]
        [LabelText("医疗环(半径/等级, 取最高)")] public List<LevelRing> medicalRings = new();
        [LabelText("治安环(半径/等级, 取最高)")] public List<LevelRing> securityRings = new();
        [LabelText("美化环环(半径/等级, 取最高)")] public List<LevelRing> beautifyRings = new();
    }
}
