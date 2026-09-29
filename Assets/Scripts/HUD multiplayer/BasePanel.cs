using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BasePanelTabs : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button labButton;
    [SerializeField] private Button hangarButton;
    [SerializeField] private Button assemblyButton;
    [SerializeField] private Button coreButton;

    [Header("Panels")]
    [SerializeField] private GameObject labPanel;
    [SerializeField] private GameObject hangarPanel;
    [SerializeField] private GameObject assemblyPanel;
    [SerializeField] private GameObject corePanel;

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;

    private int currentTabIndex = 0;

    private readonly string[] tabOrder =
    {
        "Hangar",
        "Assembly",
        "LAB",
        "CORE"
    };

    private void Start()
    {
        labButton.onClick.AddListener(
            () => ShowTab("LAB"));

        hangarButton.onClick.AddListener(
            () => ShowTab("Hangar"));

        assemblyButton.onClick.AddListener(
            () => ShowTab("Assembly"));

        coreButton.onClick.AddListener(
            () => ShowTab("CORE"));

        ShowTab("Hangar");
    }

    private void Update()
    {
        if (GameInputManager.Instance == null)
            return;

        if (GameInputManager.Instance.ChangeStationTab)
        {
            ShowNextTab();
        }
    }

    private void ShowNextTab()
    {
        currentTabIndex++;

        if (currentTabIndex >= tabOrder.Length)
        {
            currentTabIndex = 0;
        }

        ShowTab(
            tabOrder[currentTabIndex]);
    }

    private void ShowTab(string tabName)
    {
        labPanel.SetActive(
            tabName == "LAB");

        hangarPanel.SetActive(
            tabName == "Hangar");

        assemblyPanel.SetActive(
            tabName == "Assembly");

        corePanel.SetActive(
            tabName == "CORE");

        labButton.interactable =
            tabName != "LAB";

        hangarButton.interactable =
            tabName != "Hangar";

        assemblyButton.interactable =
            tabName != "Assembly";

        coreButton.interactable =
            tabName != "CORE";

        titleText.text = tabName;

        // Synchronizacja indeksu równie¿ przy klikaniu myszk¹.
        currentTabIndex =
            System.Array.IndexOf(
                tabOrder,
                tabName);
    }
}