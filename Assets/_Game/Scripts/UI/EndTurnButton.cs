using UnityEngine;
using UnityEngine.UI;

namespace SSBX
{
    /// <summary>UI绑定到“结束回合”按钮。</summary>
    [RequireComponent(typeof(Button))]
    public class EndTurnButton : MonoBehaviour
    {
        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnClick);
        }

        private void Update()
        {
            if (TurnSystem.Instance == null) return;
            _btn.interactable = !TurnSystem.Instance.IsBlocked;
        }

        private void OnClick()
        {
            TurnSystem.Instance.RequestEndTurn();
        }
    }
}
