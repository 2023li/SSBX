using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    public enum XPMode { AlwaysPerTurn, PerTurnWhenEmployees, PerTurnWhenHouseFull, ManualOnly }

    [Serializable]
    public class LevelStep
    {
        [LabelText("等级")] public int level;
        [LabelText("升级所需经验"), Min(0)] public int expToNext;

        [LabelText("（可选）升级后人口上限")] public int maxPopulationOverride;
        [LabelText("（可选）升级后岗位上限")] public int jobSlotsOverride;

        [LabelText("（可选）升级后替换为新配置BC"), Tooltip("用于医院/警局等升级覆盖半径的变更")]
        public BuildingConfig replaceConfig;
    }

    [CreateAssetMenu(fileName = "UPF_升级配置", menuName = "SSBX/Upgrade Profile")]
    public class BuildingUpgradeProfile : ScriptableObject
    {
        [LabelText("经验模式")] public XPMode xpMode = XPMode.AlwaysPerTurn;
        [LabelText("每回合经验(最小-最大)")] public Vector2Int xpGainPerTurn = new Vector2Int(3, 6);

        [LabelText("等级阶梯(按顺序)")]
        public List<LevelStep> levels = new List<LevelStep>
        {
            new LevelStep{ level=1, expToNext=60 },
            new LevelStep{ level=2, expToNext=120 },
            new LevelStep{ level=3, expToNext=200 },
            new LevelStep{ level=4, expToNext=0 }, // 0=最高级
        };
    }
}
