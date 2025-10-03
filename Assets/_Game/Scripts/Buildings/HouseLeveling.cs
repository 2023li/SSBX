using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 居民房升级：满员时每回合获得经验，达到阈值自动升级（上限Lv4）。
    /// </summary>
    public class HouseLeveling : MonoBehaviour
    {
        [LabelText("目标住房")] public HouseBuilding house;

        [BoxGroup("经验")]
        [LabelText("当前经验"), ReadOnly] public int exp;
        [LabelText("下级所需经验")] public int expToNext = 60;
        [LabelText("满员经验/回合(±波动)")] public Vector2Int expGainFull = new Vector2Int(3, 6);

        [BoxGroup("升级后参数(建议)")]
        [LabelText("Lv2最大人口")] public int lv2Max = 17;
        [LabelText("Lv3最大人口")] public int lv3Max = 32;
        [LabelText("Lv4最大人口")] public int lv4Max = 50;

        private void Awake()
        {
            if (!house) house = GetComponent<HouseBuilding>();
            if (TurnSystem.Instance != null) TurnSystem.Instance.OnBeforeTurnEnd += OnBeforeTurnEnd;
        }

        private void OnDestroy()
        {
            if (TurnSystem.Instance != null) TurnSystem.Instance.OnBeforeTurnEnd -= OnBeforeTurnEnd;
        }

        private void OnBeforeTurnEnd(int turn)
        {
            if (!house || !house.isConstructed) return;

            // 满员判定：以“当前人口==最大人口”为准
            if (house.curPopulation >= house.maxPopulation)
            {
                int add = Random.Range(expGainFull.x, expGainFull.y + 1);
                exp += add;
                if (exp >= expToNext) DoLevelUp();
            }
        }

        [Button("手动升级(调试)")]
        private void DoLevelUp()
        {
            if (house.level >= 4) return;
            exp = 0; expToNext = Mathf.RoundToInt(expToNext * 1.8f); // 下一档更难

            house.level++;
            if (house.level == 2) house.maxPopulation = lv2Max;
            else if (house.level == 3) house.maxPopulation = lv3Max;
            else if (house.level == 4) house.maxPopulation = lv4Max;

            Debug.Log($"[HouseLeveling] {house.name} 升级到 Lv{house.level}，MaxPop={house.maxPopulation}");
        }
    }
}
