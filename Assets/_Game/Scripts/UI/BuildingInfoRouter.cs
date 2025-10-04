using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 信息面板路由：优先用 Addressables（按约定地址）；未启用时退回 Inspector 映射。
    /// 地址优先级：ID > Category > Type > Default
    /// </summary>
    public class BuildingInfoRouter : MonoBehaviour
    {
        [LabelText("使用Addressables加载")] public bool useAddressables = true;

        [System.Serializable] public class IdMap { public string configId; public BuildingPanelBase panelPrefab; }
        [System.Serializable] public class CatMap { public BuildingCategory category; public BuildingPanelBase panelPrefab; }

        [InfoBox("当 useAddressables=false 时，使用下方映射；true 时忽略下方映射", InfoMessageType.Info)]
        [LabelText("按配置ID映射(手动)")] public List<IdMap> byId = new();
        [LabelText("按类别映射(手动)")] public List<CatMap> byCategory = new();

        [Title("类型默认（手动模式用）")]
        [LabelText("居民房默认")] public BuildingPanelBase housePanel;
        [LabelText("仓库默认")] public BuildingPanelBase warehousePanel; // 粮仓同用
        [LabelText("通用默认")] public BuildingPanelBase genericPanel;

        /// <summary>基于模式创建并返回已实例化的面板。</summary>
        public async Task<BuildingPanelBase> CreatePanelAsync(Building b, Transform parent)
        {
            // 优先：若 Config 上手动指定了面板Prefab，直接实例化
            if (b != null && b.config != null && b.config.infoPanelPrefab != null)
            {
                var panel = Instantiate(b.config.infoPanelPrefab, parent);
                return panel;
            }


            
            if (useAddressables) return await CreatePanelByAddressables(b, parent);
            else return CreatePanelByInspector(b, parent);
        }

        // --- Addressables 路径解析 ---
        private async Task<BuildingPanelBase> CreatePanelByAddressables(Building b, Transform parent)
        {
            if (b == null || b.config == null) return null;

            // 1) 按ID
            string addr = ResourceKeys.UiBuildingInfoById(b.config.id);
            var panel = await TryInstantiate(addr, parent);
            if (panel != null) return panel;

            // 2) 按类别
            addr = ResourceKeys.UiBuildingInfoByCategory(b.config.category);
            panel = await TryInstantiate(addr, parent);
            if (panel != null) return panel;

            // 3) 按类型（House/Warehouse/Granary/…）
            string type = b is HouseBuilding ? "House"
                        : b is WarehouseBuilding ? "Warehouse"
                        : b is GranaryBuilding ? "Granary"
                        : b.GetType().Name;
            addr = ResourceKeys.UiBuildingInfoByType(type);
            panel = await TryInstantiate(addr, parent);
            if (panel != null) return panel;

            // 4) 兜底
            addr = ResourceKeys.UiBuildingInfoDefault;
            panel = await TryInstantiate(addr, parent);
            if (panel != null) return panel;

            Debug.LogWarning($"[InfoRouter] 未找到任何匹配面板地址（ID/Category/Type/Default）。建筑：{b.config.id}/{b.config.displayName}");
            return null;
        }

        private async Task<BuildingPanelBase> TryInstantiate(string address, Transform parent)
        {
            var prefab = await AddressableService.Instance.LoadPrefabAsync(address);
            if (prefab == null) return null;

            var go = AddressableService.Instance.InstantiateFromPrefab(prefab, parent);
            var panel = go.GetComponent<BuildingPanelBase>();
            if (panel == null)
            {
                Debug.LogWarning($"[InfoRouter] 地址 {address} 对应的Prefab缺少 BuildingPanelBase 脚本。");
                AddressableService.Instance.DestroyInstance(go);
                return null;
            }
            return panel;
        }

        // --- Inspector 手动映射（原方案保留，便于过渡） ---
        private BuildingPanelBase CreatePanelByInspector(Building b, Transform parent)
        {
            BuildingPanelBase prefab = null;

            // 1) ID
            if (b.config != null && !string.IsNullOrEmpty(b.config.id))
            {
                for (int i = 0; i < byId.Count; i++)
                    if (byId[i].configId == b.config.id) { prefab = byId[i].panelPrefab; break; }
            }
            // 2) Category
            if (prefab == null && b.config != null)
            {
                for (int i = 0; i < byCategory.Count; i++)
                    if (byCategory[i].category == b.config.category) { prefab = byCategory[i].panelPrefab; break; }
            }
            // 3) Type Default
            if (prefab == null)
            {
                if (b is HouseBuilding) prefab = housePanel;
                else if (b is WarehouseBuilding || b is GranaryBuilding) prefab = warehousePanel;
                else prefab = genericPanel;
            }

            if (prefab == null) return null;
            return Instantiate(prefab, parent);
        }
    }
}
