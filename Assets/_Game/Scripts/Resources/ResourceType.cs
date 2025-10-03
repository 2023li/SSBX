namespace SSBX
{
    /// <summary>资源类型（可根据文档随时扩充）。</summary>
    public enum ResourceType
    {
        // 基础材料
        Log, Plank, Charcoal, Stone, Clay,
        Silk, Flax, Cotton, Wool,

        // 食物：一级
        Barley, Rice, Corn,
        // 食物：二级
        Chicken, Duck, Fish, Mutton, Milk, Bread,
        // 食物：三级（可作为奢侈品候选）
        Salt, Pastry, Wine, Tea, Honey,

        // 生活/奢侈相关（新增）
        Clothes,     // 衣服（一级额外需求）
        Furniture,   // 家具（一级额外需求）
        Gems,        // 宝石（三级额外需求/奢侈）

        // 后续还可加入 Porcelain/Incense/Spice 等
    }
}
