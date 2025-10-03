using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>
    /// 运输UI：选择两座仓库创建路线，并显示预测 L/Q/M；支持查看与删除已建路线。
    /// </summary>
    public class TransportUI : MonoBehaviour
    {
        [LabelText("Header文本")] public TextMeshProUGUI txtHeader;
        [LabelText("列表文本")] public TextMeshProUGUI txtList;

        [LabelText("按钮：新建路线")] public Button btnNewRoute;
        [LabelText("按钮：确认")] public Button btnConfirm;
        [LabelText("按钮：取消")] public Button btnCancel;
        [LabelText("按钮：刷新")] public Button btnRefresh;

        [ReadOnly, LabelText("选择模式")] public bool selecting;
        [ReadOnly, LabelText("源仓库")] public WarehouseBuilding pickA;
        [ReadOnly, LabelText("目标仓库")] public WarehouseBuilding pickB;

        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
            if (btnNewRoute) btnNewRoute.onClick.AddListener(BeginPick);
            if (btnConfirm) btnConfirm.onClick.AddListener(ConfirmRoute);
            if (btnCancel) btnCancel.onClick.AddListener(CancelPick);
            if (btnRefresh) btnRefresh.onClick.AddListener(RefreshList);
        }

        private void OnEnable() => RefreshList();

        [Button("刷新列表")]
        public void RefreshList()
        {
            var trs = TransportRouteSystem.Instance;
            if (!trs) return;

            // Header：参数摘要
            if (txtHeader)
                txtHeader.text = $"路线数：{trs.routes.Count}｜分母偏移={trs.throughputDivisorOffset} 维护系数={trs.maintenanceFactor} 最小Q={trs.minQPerTurn}";

            // 文本列表
            var sb = new StringBuilder();
            for (int i = 0; i < trs.routes.Count; i++)
            {
                var r = trs.routes[i];
                if (!r.source || !r.target) continue;
                var pred = trs.PredictStats(r.source, r.target);
                sb.AppendLine($"[{i}] {r.source.name} → {r.target.name}｜L={pred.L:0.##} 预估Q={pred.Q}/回 合 维护={pred.M}/回");
            }
            if (txtList) txtList.text = sb.ToString();
        }

        private void Update()
        {
            if (!selecting) return;

            if (TryPickWarehouse(out var wh))
            {
                if (pickA == null)
                {
                    pickA = wh;
                    Hint($"已选择『源』：{wh.name}，请再点一座仓库作为『目标』。");
                }
                else if (wh != pickA)
                {
                    pickB = wh;
                    var (L, Q, M) = TransportRouteSystem.Instance.PredictStats(pickA, pickB);
                    Hint(L < 0 ? "两仓不可达，请重选。" : $"预测：L={L:0.##}，Q={Q}/回，维护={M}/回。点『确认』创建。");
                }
            }
        }

        private bool TryPickWarehouse(out WarehouseBuilding wh)
        {
            wh = null;
            if (!Input.GetMouseButtonDown(0)) return false;
            var sp = Input.mousePosition;
            var screen = new Vector3(sp.x, sp.y, -_cam.transform.position.z);
            var world = _cam.ScreenToWorldPoint(screen);
            world.z = 0f;

            if (!GridSystem.Instance.TryGetCell(world, out var cell)) return false;
            var b = GridIndex.Instance.GetBuildingAt(cell) as WarehouseBuilding;
            if (b == null) { Hint("该格不是仓库。"); return false; }
            wh = b; return true;
        }

        private void BeginPick()
        {
            selecting = true; pickA = pickB = null;
            Hint("选择『源仓库』。");
        }

        private void ConfirmRoute()
        {
            if (!selecting || pickA == null || pickB == null) { Hint("未选齐两座仓库。"); return; }
            var pred = TransportRouteSystem.Instance.PredictStats(pickA, pickB);
            if (pred.L < 0) { Hint("两仓不可达，无法创建。"); return; }

            TransportRouteSystem.Instance.AddRoute(pickA, pickB);
            selecting = false; RefreshList();
            Hint("路线已创建。");
        }

        private void CancelPick()
        {
            selecting = false; pickA = pickB = null;
            Hint("已取消。");
        }

        private void Hint(string msg) { if (txtHeader) txtHeader.text = msg; }
    }
}
