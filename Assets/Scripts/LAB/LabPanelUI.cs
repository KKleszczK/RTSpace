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

    private BaseSafeZone localSafeZone;
    private PlayerUpgradeStats localUpgradeStats;
    private BaseCore localCore;


    private List<ResearchButtonUI> createdButtons = new();

    private void Start()
    {
        FindLocalPlayerResearch();
        FindLocalResearchBonuses();
        TryFindLocalCore();

        for (int i = 0; i < queueSlotButtons.Length; i++)
        {
            int index = i;

            queueSlotButtons[i].onClick.AddListener(
                () => RemoveQueueItem(index));
        }
    }

    private void Update()
    {
        if (localCore == null)
            TryFindLocalCore();

        if (playerResearch == null)
            FindLocalPlayerResearch();

        if (localSafeZone == null ||
            localUpgradeStats == null)
        {
            FindLocalResearchBonuses();
        }

        UpdateProgressBar();
        UpdateQueueUI();

        foreach (ResearchButtonUI button in createdButtons)
        {
            if (button != null)
                button.Refresh();
        }
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

    private void TryFindLocalCore()
    {
        if (localCore != null)
            return;

        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        BaseCore[] cores =
            FindObjectsByType<BaseCore>(
                FindObjectsSortMode.None);

        foreach (BaseCore core in cores)
        {
            if (core == null)
                continue;

            if (!core.IsSpawned)
                continue;

            if (core.OwnerClientId !=
                Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                continue;
            }

            localCore = core;

            localCore.tier.OnValueChanged +=
                OnCoreTierChanged;

            RefreshResearchButtons();

            return;
        }
    }

    private void OnCoreTierChanged(
    int previousTier,
    int newTier)
    {
        RefreshResearchButtons();
    }

    private void FindLocalResearchBonuses()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        ulong localClientId =
            Unity.Netcode.NetworkManager.Singleton.LocalClientId;

        // =====================================================
        // SAFE ZONE
        // =====================================================

        if (localSafeZone == null)
        {
            BaseSafeZone[] safeZones =
                FindObjectsByType<BaseSafeZone>(
                    FindObjectsSortMode.None);

            foreach (BaseSafeZone safeZone in safeZones)
            {
                if (!safeZone.IsSpawned)
                    continue;

                UnitOwner owner =
                    safeZone.GetComponent<UnitOwner>();

                if (owner == null)
                    continue;

                if (owner.ownerId.Value !=
                    localClientId)
                {
                    continue;
                }

                localSafeZone = safeZone;
                break;
            }
        }

        // =====================================================
        // RESEARCH UPGRADES
        // =====================================================

        if (localUpgradeStats == null)
        {
            PlayerUpgradeStats[] allStats =
                FindObjectsByType<PlayerUpgradeStats>(
                    FindObjectsSortMode.None);

            foreach (PlayerUpgradeStats stats in allStats)
            {
                if (!stats.IsSpawned)
                    continue;

                if (stats.OwnerClientId !=
                    localClientId)
                {
                    continue;
                }

                localUpgradeStats = stats;
                break;
            }
        }
    }

    private void UpdateQueueUI()
    {
        if (playerResearch == null)
            return;

        for (int i = 0; i < queueSlotIcons.Length; i++)
        {
            // =====================================================
            // RESEARCH W KOLEJCE
            // =====================================================

            if (i < playerResearch.researchQueue.Count)
            {
                string id =
                    playerResearch.researchQueue[i]
                        .ToString();

                ResearchDefinition research =
                    ResearchDatabase.Instance
                        .GetResearch(id);

                if (research != null)
                {
                    ResearchTierColorHelper.ApplyToImage(
                        queueSlotIcons[i],
                        research);

                    queueSlotButtons[i].interactable =
                        true;
                }
                else
                {
                    SetEmptyQueueSlot(i);
                }
            }

            // =====================================================
            // PUSTY SLOT
            // =====================================================

            else
            {
                SetEmptyQueueSlot(i);
            }
        }
    }

    private void SetEmptyQueueSlot(
    int index)
    {
        queueSlotIcons[index].sprite =
            emptySlotSprite;

        // Bardzo wa¿ne:
        // resetujemy kolor po poprzednim researchu.
        queueSlotIcons[index].color =
            Color.white;

        queueSlotButtons[index].interactable =
            false;
    }

    private void RemoveQueueItem(int index)
    {
        if (playerResearch == null)
            return;

        playerResearch.RequestRemoveFromQueue(index);
    }
    private void RefreshResearchButtons()
    {
        if (content == null)
            return;

        if (buttonPrefab == null)
            return;

        if (localCore == null)
            return;

        // =====================================================
        // USUWAMY STARE PRZYCISKI
        // =====================================================

        for (int i = content.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                content.GetChild(i).gameObject);
        }

        createdButtons.Clear();

        // =====================================================
        // SORTOWANIE
        // =====================================================

        List<ResearchDefinition> sortedResearches =
            new List<ResearchDefinition>(researches);

        sortedResearches.Sort(
            (a, b) =>
            {
                if (a == null && b == null)
                    return 0;

                if (a == null)
                    return 1;

                if (b == null)
                    return -1;

                return a.tier.CompareTo(b.tier);
            });

        int coreTier =
            localCore.tier.Value;

        // =====================================================
        // TWORZENIE DOSTÊPNYCH RESEARCHY
        // =====================================================

        foreach (ResearchDefinition research
                 in sortedResearches)
        {
            if (research == null)
                continue;

            // Research wy¿szego tieru ni¿ Core
            // nie jest jeszcze widoczny.
            if ((int)research.tier > coreTier)
                continue;

            ResearchButtonUI button =
                Instantiate(
                    buttonPrefab,
                    content);

            button.Setup(
                research,
                this);

            createdButtons.Add(
                button);
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

    private void OnDestroy()
    {
        if (localCore != null)
        {
            localCore.tier.OnValueChanged -=
                OnCoreTierChanged;
        }
    }

    public void ShowDescription(
    ResearchDefinition research)
    {
        if (research == null)
            return;

        FindLocalResearchBonuses();

        descriptionNameText.text =
            research.displayName;

        descriptionText.text =
            research.description;

        // =====================================================
        // BONUSES
        // =====================================================

        float labBonusPercent =
            localSafeZone != null
                ? localSafeZone.GetLabSpeedBonusPercent()
                : 0f;

        float researchBonusPercent =
            localUpgradeStats != null
                ? localUpgradeStats.GetResearchSpeedBonusPercent()
                : 0f;

        float totalBonusPercent =
            labBonusPercent +
            researchBonusPercent;

        totalBonusPercent =
            Mathf.Clamp(
                totalBonusPercent,
                0f,
                100f);

        // =====================================================
        // FINAL TIME
        // =====================================================

        float timeMultiplier =
            1f -
            totalBonusPercent / 100f;

        float finalResearchTime =
            Mathf.Max(
                0f,
                research.baseResearchTime *
                timeMultiplier);

        // =====================================================
        // TEXT
        // =====================================================

        costText.text =
            $"Met: {research.baseMetalCost}|" +
            $"Ene: {research.baseEnergyCost}|" +
            $"Time: {finalResearchTime:0.#}s";
    }

    private void FindLocalPlayerResearch()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        PlayerResearch[] all =
            FindObjectsByType<PlayerResearch>(
                FindObjectsSortMode.None);

        foreach (PlayerResearch pr in all)
        {
            if (!pr.IsSpawned)
                continue;

            if (pr.OwnerClientId ==
                Unity.Netcode.NetworkManager.Singleton.LocalClientId)
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