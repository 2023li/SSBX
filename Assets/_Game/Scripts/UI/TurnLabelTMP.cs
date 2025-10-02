using TMPro;
using UnityEngine;

namespace SSBX
{
    /// <summary>绑定到一个 TextMeshProUGUI，用于显示“第X回合”。</summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TurnLabelTMP : MonoBehaviour
    {
        private TextMeshProUGUI _tmp;

        private void Awake()
        {
            _tmp = GetComponent<TextMeshProUGUI>();
            if (TurnSystem.Instance != null)
                TurnSystem.Instance.OnTurnBegan += OnTurnBegan;
        }

        private void OnDestroy()
        {
            if (TurnSystem.Instance != null)
                TurnSystem.Instance.OnTurnBegan -= OnTurnBegan;
        }

        private void Start()
        {
            if (TurnSystem.Instance != null)
                _tmp.text = $" {TurnSystem.Instance.CurrentTurn}";
        }

        private void OnTurnBegan(int curTurn)
        {
            _tmp.text = $" {curTurn} ";
        }
    }
}
