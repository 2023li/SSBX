using UnityEngine;

namespace SSBX
{
    /// <summary>可为他者提供食物的建筑接口。</summary>
    public interface IFoodProvider
    {
        bool IsConstructed { get; }
        Vector3Int OriginCell { get; }
        int Get(ResourceType t);
        bool TryProvide(ResourceType t, int amount);
    }
}
