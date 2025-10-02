using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SSBX
{
    /// <summary>仓库：通用库存，运输系统的端点。</summary>
    public class WarehouseBuilding : Building, IFoodProvider
    {
#if ODIN_INSPECTOR
        [BoxGroup("库存"), InlineProperty, HideLabel]
#endif
        public Inventory inventory = new Inventory();

        public bool IsConstructed => isConstructed;
        public Vector3Int OriginCell => originCell;

        public int Get(ResourceType t) => inventory.Get(t);
        public bool TryProvide(ResourceType t, int amount) => inventory.TryConsume(t, amount);

        [ContextMenu("调试：添加测试食物")]
        public void Debug_AddFood()
        {
            inventory.Add(ResourceType.Barley, 100);
            inventory.Add(ResourceType.Rice, 100);
            inventory.Add(ResourceType.Corn, 100);
        }
    }
}
