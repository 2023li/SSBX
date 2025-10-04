using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace SSBX
{
    /// <summary>悬浮选择条：显示“信息”按钮。</summary>
    public class SelectionBarUI : MonoBehaviour
    {
        [LabelText("标题文本")] public TextMeshProUGUI txtTitle;
        [LabelText("信息按钮")] public Button btnInfo;

        private RectTransform _rt;
        private BuildingSelectionController _owner;
        private Camera _cam;
        private Building _bound;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _cam = Camera.main;
            if (btnInfo) btnInfo.onClick.AddListener(OnClickInfo);
        }

        public void Bind(BuildingSelectionController owner) => _owner = owner;

        public void ShowAt(Vector3 worldPos, Building b)
        {
            _bound = b;
            if (txtTitle) txtTitle.text = b.config ? b.config.displayName : b.name;
            gameObject.SetActive(true);
            PositionAt(worldPos);
        }

        public void PositionAt(Vector3 world)
        {
            var sp = _cam.WorldToScreenPoint(world + new Vector3(0, GridSystem.Instance.unityGrid.cellSize.y * 0.6f, 0));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, sp, null, out var lp);
            _rt.anchoredPosition = lp;
        }

        public void Hide() { gameObject.SetActive(false); _bound = null; }

        private void OnClickInfo()
        {
            if (_bound == null) return;
            UIManager.Instance.OpenInfo(_bound);
        }
    }
}
