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



        [Button]
        public void Test()
        {
            inventory.Add(ResourceType.Barley, 100);
            inventory.Add(ResourceType.Rice, 100);
            inventory.Add(ResourceType.Corn, 100);

            // 二级食物
            inventory.Add(ResourceType.Chicken, 50);
            inventory.Add(ResourceType.Fish, 50);
            inventory.Add(ResourceType.Bread, 50);

            // 三级食物/奢侈食物
            inventory.Add(ResourceType.Wine, 30);
            inventory.Add(ResourceType.Tea, 30);
            inventory.Add(ResourceType.Honey, 30);

            // 生活/奢侈物品
            inventory.Add(ResourceType.Clothes, 20);
            inventory.Add(ResourceType.Furniture, 20);
            inventory.Add(ResourceType.Gems, 10);

            Debug.Log("粮仓库存初始化完成！");
        }


        [Button("显示库存状态")]
        public void DisplayInventoryStatus()
        {
            Debug.Log("=== 粮仓库存状态 ===");

            // 一级食物
            Debug.Log($"一级食物 - 大麦: {inventory.Get(ResourceType.Barley)}, 水稻: {inventory.Get(ResourceType.Rice)}, 玉米: {inventory.Get(ResourceType.Corn)}");

            // 二级食物  
            Debug.Log($"二级食物 - 鸡: {inventory.Get(ResourceType.Chicken)}, 鱼: {inventory.Get(ResourceType.Fish)}, 面包: {inventory.Get(ResourceType.Bread)}");

            // 三级食物
            Debug.Log($"三级食物 - 葡萄酒: {inventory.Get(ResourceType.Wine)}, 茶: {inventory.Get(ResourceType.Tea)}, 蜂蜜: {inventory.Get(ResourceType.Honey)}");

            // 奢侈物品
            Debug.Log($"奢侈物品 - 衣服: {inventory.Get(ResourceType.Clothes)}, 家具: {inventory.Get(ResourceType.Furniture)}, 宝石: {inventory.Get(ResourceType.Gems)}");
        }

    }
}
