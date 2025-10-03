using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    [Serializable]
    public class SaveData
    {
        public int turn;
        public int gold, science, culture, happiness, faith;
        public List<BuildingData> buildings = new();
    }

    [Serializable]
    public class BuildingData
    {
        public string id;                 // BuildingConfig.id（BID_中文）
        public string prefabName;         // 可选：回放时查找
        public int x, y;                  // originCell
        public int owner;                 // Faction
        public bool constructed;
        public int buildProgress;

        // 细分状态（按类型可选填）
        public int house_level, house_pop, house_employed;
        public Dictionary<int, int> inv;   // 仓库/粮仓库存：key=ResourceType(int)
    }

    /// <summary>最小存档：JSON 文件；如装有 ES3，可切换为 ES3.Save/Load。</summary>
    public class SaveLoadSystem : MonoBehaviour
    {
        [LabelText("存档文件名")] public string fileName = "save_slot0.json";

        string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        [Button("保存")]
        public void Save()
        {
            var data = new SaveData();
            var ks = KingdomStats.Instance;
            data.turn = TurnSystem.Instance.CurrentTurn;
            data.gold = ks.gold; data.science = ks.science; data.culture = ks.culture;
            data.happiness = ks.happiness; data.faith = ks.faith;

            foreach (var b in GameObject.FindObjectsOfType<Building>())
            {
                var bd = new BuildingData
                {
                    id = b.config ? b.config.id : "",
                    prefabName = b.name.Replace("(Clone)", ""),
                    x = b.originCell.x,
                    y = b.originCell.y,
                    owner = (int)b.owner,
                    constructed = b.isConstructed,
                    buildProgress = b.buildProgress
                };

                if (b is HouseBuilding h)
                { bd.house_level = h.level; bd.house_pop = h.curPopulation; bd.house_employed = h.employed; }

                if (b is WarehouseBuilding w)
                { bd.inv = DumpInventory(w.inventory); }
                if (b is GranaryBuilding g)
                { bd.inv = DumpInventory(g.inventory); }

                data.buildings.Add(bd);
            }

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] 已保存到 {SavePath}");

            // 若你已导入 ES3，可改用：
             ES3.Save("save0", data);
        }

        [Button("读取")]
        public void Load()
        {
            if (!File.Exists(SavePath)) { Debug.LogWarning("无存档文件"); return; }
            var json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);

            // 清场（简单处理：仅演示）
            foreach (var b in GameObject.FindObjectsOfType<Building>()) Destroy(b.gameObject);

            // 读全局
            var ks = KingdomStats.Instance;
            ks.gold = data.gold; ks.science = data.science; ks.culture = data.culture;
            ks.happiness = data.happiness; ks.faith = data.faith;
            TurnSystem.Instance.GetType().GetProperty("CurrentTurn")?.SetValue(TurnSystem.Instance, data.turn, null);

            // 读建筑（演示：按 id/名称映射到你的 Prefab/Config）
            foreach (var bd in data.buildings)
            {
                var cfg = FindConfigById(bd.id);
                var prefab = FindPrefabByName(bd.prefabName);
                if (cfg == null || prefab == null) { Debug.LogWarning($"缺资源：{bd.id}/{bd.prefabName}"); continue; }

                var placed = BuildingManager.Instance.Place(prefab, cfg, new Vector3Int(bd.x, bd.y, 0));
                placed.owner = (Faction)bd.owner;
                placed.isConstructed = bd.constructed;
                placed.buildProgress = bd.buildProgress;

                if (placed is HouseBuilding h)
                { h.level = bd.house_level; h.curPopulation = bd.house_pop; h.employed = bd.house_employed; }

                if (placed is WarehouseBuilding w && bd.inv != null) LoadInventory(w.inventory, bd.inv);
                if (placed is GranaryBuilding g && bd.inv != null) LoadInventory(g.inventory, bd.inv);
            }
            Debug.Log("[Load] 读取完成（示例实现，可按需扩展）");

            // 若你已导入 ES3，可改用：
             data = ES3.Load<SaveData>("save0");
        }

        private BuildingConfig FindConfigById(string id)
        {
            // 简易查找：从 Resources 或你自维护的地址表中取。此处为了演示，返回场景中任意同id的config。
            foreach (var cfg in Resources.FindObjectsOfTypeAll<BuildingConfig>())
                if (cfg.id == id) return cfg;
            return null;
        }
        private Building FindPrefabByName(string name)
        {
            // 同上，演示：从场景里找同名Prefab引用（建议你用 Addressables/字典映射替换）
            foreach (var b in Resources.FindObjectsOfTypeAll<Building>())
                if (b.name.Replace("(Clone)", "") == name) return b;
            return null;
        }

        private Dictionary<int, int> DumpInventory(Inventory inv)
        {
            var dict = new Dictionary<int, int>();
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
            {
                int v = inv.Get(t);
                if (v > 0) dict[(int)t] = v;
            }
            return dict;
        }
        private void LoadInventory(Inventory inv, Dictionary<int, int> d)
        {
            foreach (var kv in d) inv.Add((ResourceType)kv.Key, kv.Value);
        }
    }
}
