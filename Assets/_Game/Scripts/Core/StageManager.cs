using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SSBX
{
    public enum Stage { Tribe = 0, Chiefdom = 1, ProtoState = 2, Dynasty = 3 }

    /// <summary>
    /// 阶段管理：监听“广场完工 + 敌对建筑清除”进入酋邦。
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public Stage current = Stage.Tribe;

#if ODIN_INSPECTOR
        [ReadOnly, Title("推进条件状态")]
#endif
        public bool plazaBuilt;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public int aliveEnemyBuildings;

        [Header("识别关键建筑的ID（与BuildingConfig.id一致）")]
        public string plazaId = "plaza";
        public string palaceId = "palace";   // 进入邦国
        public string mausoleumId = "mausoleum"; // 进入王朝

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameEvents.OnBuildingConstructed += OnBuildingConstructed;
            GameEvents.OnBuildingDestroyed += OnBuildingDestroyed;
        }

        private void OnDestroy()
        {
            GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
            GameEvents.OnBuildingDestroyed -= OnBuildingDestroyed;
        }

        private void OnBuildingConstructed(Building b)
        {
            // 统计敌对/关键建筑
            if (b.owner == Faction.Enemy) { aliveEnemyBuildings++; return; }

            var id = b.config != null ? b.config.id : string.Empty;
            if (id == plazaId) plazaBuilt = true;

            TryAdvance();
        }

        
        private void OnBuildingDestroyed(Building b)
        {
            if (b.owner == Faction.Enemy)
            {
                aliveEnemyBuildings = Mathf.Max(0, aliveEnemyBuildings - 1);
                TryAdvance(); // 可能满足“清除敌对建筑”
            }
        }

        private void TryAdvance()
        {
            // 部落→酋邦：广场 + 敌对清零（双条件）
            if (current == Stage.Tribe && plazaBuilt && aliveEnemyBuildings == 0)
            {
                current = Stage.Chiefdom;
                Debug.Log("阶段推进：进入『酋邦』");
                // TODO：解锁更大地图、开启新系统等
            }
        }
    }
}
