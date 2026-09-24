using TMPro;
using UnityEngine;

public class MatchEndPanelUI : MonoBehaviour
{
    public static MatchEndPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text reasonText;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(
        bool victory,
        MatchEndReason reason)
    {
        panel.SetActive(true);

        resultText.text =
            victory ? "VICTORY" : "DEFEAT";

        reasonText.text =
            GetReasonText(victory, reason);
    }

    private string GetReasonText(
        bool victory,
        MatchEndReason reason)
    {
        switch (reason)
        {
            case MatchEndReason.BaseDestroyed:
                return victory
                    ? "Enemy base destroyed"
                    : "Your base was destroyed";

            case MatchEndReason.Surrender:
                return victory
                    ? "Enemy surrendered"
                    : "You surrendered";

            case MatchEndReason.Disconnect:
                return victory
                    ? "Enemy disconnected"
                    : "You disconnected";

            default:
                return "";
        }
    }
}