using System.Collections.Generic;

namespace SSBX
{
    /// <summary>通用库存，供仓库/建筑使用。</summary>
    public class Inventory
    {
        private readonly Dictionary<ResourceType, int> _dict = new Dictionary<ResourceType, int>();

        public int Get(ResourceType t) => _dict.TryGetValue(t, out var v) ? v : 0;

        public void Add(ResourceType t, int amount)
        {
            if (amount == 0) return;
            if (!_dict.ContainsKey(t)) _dict[t] = 0;
            _dict[t] += amount;
            if (_dict[t] < 0) _dict[t] = 0;
        }

        public bool HasAtLeast(ResourceType t, int amount) => Get(t) >= amount;

        public bool TryConsume(ResourceType t, int amount)
        {
            if (!HasAtLeast(t, amount)) return false;
            _dict[t] -= amount;
            return true;
        }
    }
}
