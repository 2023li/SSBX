using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// Addressables 资源服务：统一初始化、加载Prefab、实例化、缓存与释放。
    /// </summary>
    public class AddressableService : MonoBehaviour
    {
        public static AddressableService Instance { get; private set; }

        [LabelText("调试日志")] public bool verbose = false;

        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabHandles = new();
        private readonly HashSet<GameObject> _instances = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [Button("初始化(Addressables)")]
        public async Task InitializeAsync()
        {
            var handle = Addressables.InitializeAsync();
            await handle.Task;
            if (verbose) Debug.Log("[Addr] Addressables 初始化完成");
        }

        /// <summary>按地址加载 Prefab（带缓存）。</summary>
        public async Task<GameObject> LoadPrefabAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;

            if (_prefabHandles.TryGetValue(address, out var cached))
            {
                if (cached.IsValid() && cached.Status == AsyncOperationStatus.Succeeded)
                    return cached.Result;
                // 句柄异常则移除重载
                _prefabHandles.Remove(address);
            }

            var h = Addressables.LoadAssetAsync<GameObject>(address);
            await h.Task;
            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                if (verbose) Debug.LogWarning($"[Addr] 加载失败：{address}");
                return null;
            }
            _prefabHandles[address] = h;
            if (verbose) Debug.Log($"[Addr] 载入Prefab：{address}");
            return h.Result;
        }

        /// <summary>实例化一个面板（用已加载的Prefab）。</summary>
        public GameObject InstantiateFromPrefab(GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, parent, worldPositionStays: false);
            _instances.Add(go);
            return go;
        }

        /// <summary>销毁面板实例（不释放Prefab句柄）。</summary>
        public void DestroyInstance(GameObject go)
        {
            if (go == null) return;
            _instances.Remove(go);
            Object.Destroy(go);
        }

        /// <summary>释放所有 Prefab 句柄（切换场景/回主菜单时可调用）。</summary>
        [Button("释放所有缓存")]
        public void ReleaseAll()
        {
            foreach (var kv in _prefabHandles)
                if (kv.Value.IsValid()) Addressables.Release(kv.Value);
            _prefabHandles.Clear();
            if (verbose) Debug.Log("[Addr] 已释放所有Prefab句柄");
        }
    }
}
