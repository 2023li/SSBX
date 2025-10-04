using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 通用建筑升级：挂到任意需要升级的建筑上，通过 Profile 配置实现。
    /// 支持：Always / 有员工 / 居民房满员 三种经验触发。
    /// 升级后可选覆盖：人口上限、岗位上限、替换BC（医疗/治安覆盖变化）、并自动重算地块影响。
    /// </summary>
    public class BuildingLeveling : MonoBehaviour
    {
        [LabelText("升级配置Profile")] public BuildingUpgradeProfile profile;

        [BoxGroup("状态"), ReadOnly, LabelText("当前等级")] public int level = 1;
        [BoxGroup("状态"), ReadOnly, LabelText("当前经验")] public int exp = 0;

        private Building _b;

        private void Awake()
        {
            _b = GetComponent<Building>();
            if (_b != null && _b.config != null) level = Mathf.Max(1, _b.config.level);

            if (TurnSystem.Instance != null)
                TurnSystem.Instance.OnBeforeTurnEnd += OnBeforeTurnEnd;
        }

        private void OnDestroy()
        {
            if (TurnSystem.Instance != null)
                TurnSystem.Instance.OnBeforeTurnEnd -= OnBeforeTurnEnd;
        }

        private void OnBeforeTurnEnd(int turn)
        {
            if (profile == null || _b == null || !_b.isConstructed) return;

            if (CanGainXP())
            {
                int add = Random.Range(profile.xpGainPerTurn.x, profile.xpGainPerTurn.y + 1);
                exp += add;
                TryLevelUp();
            }
        }

        [Button("手动+10XP")]
        public void Debug_Add10XP()
        {
            exp += 10;
            TryLevelUp();
        }

        private bool CanGainXP()
        {
            switch (profile.xpMode)
            {
                case XPMode.AlwaysPerTurn: return true;
                case XPMode.PerTurnWhenEmployees:
                    var jp = GetComponent<JobProvider>();
                    return jp != null && jp.occupied > 0;
                case XPMode.PerTurnWhenHouseFull:
                    var h = GetComponent<HouseBuilding>();
                    return h != null && h.curPopulation >= h.maxPopulation;
                default: return false;
            }
        }

        private void TryLevelUp()
        {
            var step = GetStep(level);
            while (step != null && step.expToNext > 0 && exp >= step.expToNext)
            {
                exp -= step.expToNext;
                level++;
                ApplyLevelEffects(level);
                step = GetStep(level);
            }
        }

        private LevelStep GetStep(int lvl)
        {
            if (profile == null || profile.levels == null) return null;
            for (int i = 0; i < profile.levels.Count; i++)
                if (profile.levels[i].level == lvl) return profile.levels[i];
            return null;
        }

        private void ApplyLevelEffects(int newLevel)
        {
            if (_b == null) return;

            var step = GetStep(newLevel);
            if (step == null) return;

            // 1) 写回 config.level
            if (_b.config != null) _b.config.level = newLevel;

            // 2) 可选：人口/岗位覆盖（按组件类型）
            var h = GetComponent<HouseBuilding>();
            if (h != null && step.maxPopulationOverride > 0) h.maxPopulation = step.maxPopulationOverride;

            var jp = GetComponent<JobProvider>();
            if (jp != null && step.jobSlotsOverride > 0) jp.jobSlots = step.jobSlotsOverride;

            // 3) 可选：替换 BC（用于医院/警局等覆盖变化）
            if (step.replaceConfig != null && _b.config != step.replaceConfig)
            {
                _b.config = step.replaceConfig;
            }

            // 4) 重算地块影响（确保覆盖变化立即反映）
            if (AreaEffectSystem.Instance != null)
                AreaEffectSystem.Instance.RebuildAll();

            Debug.Log($"[Leveling] {_b.name} 升级到 Lv{newLevel}（剩余经验：{exp}）");
        }
    }
}
