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
        // 食物：三级
        Salt, Pastry, Wine, Tea, Honey,

        // 货币/点数类（不进入仓库库存）
        // Gold, Science, Culture, Faith, Happiness // → 走“国库”模块
    }
}
