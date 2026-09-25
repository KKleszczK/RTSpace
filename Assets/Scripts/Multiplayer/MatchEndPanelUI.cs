using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchEndPanelUI : MonoBehaviour
{
    public static MatchEndPanelUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text reasonText;

    [Header("Statistics")]
    [SerializeField] private TMP_Text shipKillsValue;
    [SerializeField] private TMP_Text modulesCraftedValue;
    [SerializeField] private TMP_Text researchesValue;
    [SerializeField] private TMP_Text damageDealtValue;
    [SerializeField] private TMP_Text totalMetalValue;
    [SerializeField] private TMP_Text totalEnergyValue;

    [SerializeField] private Color localPlayerColor = Color.blue;
    [SerializeField] private Color enemyPlayerColor = Color.red;

    [Header("Charts")]
    [SerializeField] private MatchResourceChartUI metalChart;
    [SerializeField] private MatchResourceChartUI energyChart;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(bool victory, MatchEndReason reason)
    {
        panel.SetActive(true);

        resultText.text =
            victory ? "VICTORY" : "DEFEAT";

        reasonText.text =
            GetReasonText(victory, reason);

        Canvas.ForceUpdateCanvases();

        RefreshStatistics();
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

    private void RefreshStatistics()
    {
        if (MatchStatistics.Instance == null)
            return;

        if (NetworkManager.Singleton == null)
            return;

        ulong localId =
            NetworkManager.Singleton.LocalClientId;

        MatchStatistics.PlayerStats localStats =
            MatchStatistics.Instance.GetStats(localId);

        MatchStatistics.PlayerStats enemyStats = null;

        foreach (var pair in
                 MatchStatistics.Instance.GetAllStats())
        {
            if (pair.Key == localId)
                continue;

            enemyStats = pair.Value;
            break;
        }

        if (localStats == null ||
            enemyStats == null)
        {
            Debug.LogWarning(
                "[STATS UI] Brak statystyk jednego z graczy.");

            return;
        }

        shipKillsValue.text =
            FormatStats(
                localStats.shipsKilled,
                enemyStats.shipsKilled);

        modulesCraftedValue.text =
            FormatStats(
                localStats.modulesCrafted,
                enemyStats.modulesCrafted);

        researchesValue.text =
            FormatStats(
                localStats.researches,
                enemyStats.researches);

        damageDealtValue.text =
            FormatStats(
                localStats.damageDealt,
                enemyStats.damageDealt);

        totalMetalValue.text =
            FormatStats(
                localStats.totalMetal,
                enemyStats.totalMetal);

        totalEnergyValue.text =
            FormatStats(
                localStats.totalEnergy,
                enemyStats.totalEnergy);

        metalChart.Draw(
            localStats.metalIncomeHistory,
            enemyStats.metalIncomeHistory);

        energyChart.Draw(
            localStats.energyIncomeHistory,
            enemyStats.energyIncomeHistory);
    }

    private string FormatStats(
        int localValue,
        int enemyValue)
    {
        string localColor =
            ColorUtility.ToHtmlStringRGB(
                localPlayerColor);

        string enemyColor =
            ColorUtility.ToHtmlStringRGB(
                enemyPlayerColor);

        return
            $"<color=#{localColor}>{localValue}</color>" +
            " | " +
            $"<color=#{enemyColor}>{enemyValue}</color>";
    }
}