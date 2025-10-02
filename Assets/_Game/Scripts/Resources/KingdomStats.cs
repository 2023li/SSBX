using UnityEngine;

namespace SSBX
{
    /// <summary>国库与全局点数（非仓库共享）</summary>
    public class KingdomStats : MonoBehaviour
    {
        public static KingdomStats Instance { get; private set; }

        [Header("基础点数（全局）")]
        public int gold = 0;
        public int science = 0;
        public int culture = 0;
        [Range(0, 100)] public int happiness = 50;
        public int faith = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount; return true;
        }

        public void AddGold(int amount) { gold += amount; if (gold < 0) gold = 0; }
    }
}
