using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace SSBX
{
    /// <summary>粮仓：专职食物仓储（一级/二级/三级食物）。</summary>
    public class GranaryBuilding : Building, IFoodProvider
    {
#if ODIN_INSPECTOR
        [BoxGroup("库存"), InlineProperty, HideLabel]
#endif
        public Inventory inventory = new Inventory();

        public void Start()
        {
            Test();
        }

        public bool IsConstructed => isConstructed;
        public Vector3Int OriginCell => originCell;

        public int Get(ResourceType t) => inventory.Get(t);
        public bool TryProvide(ResourceType t, int amount) => inventory.TryConsume(t, amount);

        // 可选：限制只允许食物类型的 Add（此Demo从简，不拦截）



        public void Test()
        {
            inventory.Add(ResourceType.Barley, 100);
        }
    }
}
