using UnityEngine;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 地块影响系统：监听建筑的构建/拆除，将 医疗/治安/美化 写入或撤回。
    /// 半径按 Chebyshev 距离（与8方向移动一致）。
    /// </summary>
    public class AreaEffectSystem : MonoBehaviour
    {
        public static AreaEffectSystem Instance { get; private set; }

        [LabelText("调试日志")] public bool verbose;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameEvents.OnBuildingConstructed += OnBuildingConstructed;
            GameEvents.OnBuildingDestroyed += OnBuildingDestroyed;
        }

        private void OnDestroy()
        {
            GameEvents.OnBuildingConstructed -= OnBuildingConstructed;
            GameEvents.OnBuildingDestroyed -= OnBuildingDestroyed;
        }

        private void OnBuildingConstructed(Building b)
        {
            if (b == null || b.config == null) return;
            ApplyEffect(b, +1);
        }

        private void OnBuildingDestroyed(Building b)
        {
            if (b == null || b.config == null) return;
            ApplyEffect(b, -1);
        }

        private void ApplyEffect(Building b, int sign)
        {
            var cfg = b.config;
            int r = cfg.serviceRadius;
            if (r <= 0 && cfg.medicalLevel == 0 && cfg.securityBonus == 0 && cfg.beautifyBonus == 0) return;

            var grid = GridSystem.Instance;

            // 遍历半径内格子
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > r) continue;
                    var c = new Vector3Int(b.originCell.x + dx, b.originCell.y + dy, 0);
                    if (!grid.IsInside(c)) continue;

                    // 叠加（写入GridSystem的CellData）
                    if (cfg.medicalLevel != 0)
                        grid.AddMedical(c, (short)(sign * cfg.medicalLevel));
                    if (cfg.securityBonus != 0)
                        grid.AddSecurity(c, (short)(sign * cfg.securityBonus));
                    if (cfg.beautifyBonus != 0)
                        grid.AddBeautify(c, (short)(sign * cfg.beautifyBonus));
                }

            if (verbose)
                Debug.Log($"[AreaEffect] {(sign > 0 ? "施加" : "移除")} {b.config.displayName} 半径={r} 医疗+{cfg.medicalLevel} 治安+{cfg.securityBonus} 美化+{cfg.beautifyBonus}");
        }

        [Button("调试：打印某格影响")]
        private void Debug_PrintCell(Vector3Int c)
        {
            var g = GridSystem.Instance;
            if (!g.IsInside(c)) { Debug.Log("不在地图内"); return; }
            Debug.Log($"格{c}: 医疗={g.GetMedical(c)} 治安={g.GetSecurity(c)} 美化={g.GetBeautify(c)}");
        }
    }
}
