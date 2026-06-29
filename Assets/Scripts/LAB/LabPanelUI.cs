using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabPanelUI : MonoBehaviour
{
    [Header("Research")]
    [SerializeField] private List<ResearchDefinition> researches = new();

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private ResearchButtonUI buttonPrefab;

    [SerializeField] private TMP_Text descriptionNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;

    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float maxProgressWidth = 500f;

    private ResearchDefinition selectedResearch;
    private PlayerResearch playerResearch;

    [SerializeField] private Image[] queueSlotIcons;
    [SerializeField] private Button[] queueSlotButtons;
    [SerializeField] private Sprite emptySlotSprite;

    private List<ResearchButtonUI> createdButtons = new();

    private void Start()
    {
        FindLocalPlayerResearch();
        CreateButtons();
        for (int i = 0; i < queueSlotButtons.Length; i++)
        {
            int index = i;
            queueSlotButtons[i].onClick.AddListener(() => RemoveQueueItem(index));
        }
    }

    private void Update()
    {
        if (playerResearch == null)
            FindLocalPlayerResearch();

        UpdateProgressBar();
        UpdateQueueUI();
        foreach (ResearchButtonUI button in createdButtons)
            button.Refresh();
    }
    private void UpdateProgressBar()
    {
        if (playerResearch == null || progressBar == null)
            return;

        float progress = playerResearch.currentProgress.Value;

        Vector2 size = progressBar.sizeDelta;
        size.x = maxProgressWidth * progress;
        progressBar.sizeDelta = size;
    }

    private void UpdateQueueUI()
    {
        if (playerResearch == null)
            return;

        for (int i = 0; i < queueSlotIcons.Length; i++)
        {
            if (i < playerResearch.researchQueue.Count)
            {
                string id = playerResearch.researchQueue[i].ToString();
                ResearchDefinition research = ResearchDatabase.Instance.GetResearch(id);

                queueSlotIcons[i].sprite = research != null ? research.icon : emptySlotSprite;
                queueSlotButtons[i].interactable = true;
            }
            else
            {
                queueSlotIcons[i].sprite = emptySlotSprite;
                queueSlotButtons[i].interactable = false;
            }
        }
    }

    private void RemoveQueueItem(int index)
    {
        if (playerResearch == null)
            return;

        playerResearch.RequestRemoveFromQueue(index);
    }
    private void CreateButtons()
    {
        foreach (ResearchDefinition research in researches)
        {
            ResearchButtonUI button =
                Instantiate(buttonPrefab, content);

            button.Setup(research, this);
            createdButtons.Add(button);
        }
    }

    public void SelectResearch(ResearchDefinition research)
    {
        selectedResearch = research;

        if (playerResearch == null)
            FindLocalPlayerResearch();

        if (playerResearch != null)
            playerResearch.RequestResearch(selectedResearch);
    }

    public void ShowDescription(ResearchDefinition research)
    {
        descriptionNameText.text = research.displayName;
        descriptionText.text = research.description;
        costText.text =
            $"Met: {research.baseMetalCost}|Ene: {research.baseEnergyCost}|Time: {research.baseResearchTime}s";
    }

    private void FindLocalPlayerResearch()
    {
        PlayerResearch[] all =
            FindObjectsByType<PlayerResearch>(FindObjectsSortMode.None);

        foreach (PlayerResearch pr in all)
        {
            if (pr.OwnerClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                playerResearch = pr;
                return;
            }
        }
    }

    public bool IsResearchCompleted(string researchId)
    {
        if (playerResearch == null)
            return false;

        return playerResearch.IsCompleted(researchId);
    }
}