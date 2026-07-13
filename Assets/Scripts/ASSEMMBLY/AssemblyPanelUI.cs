using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AssemblyPanelUI : MonoBehaviour
{
    [Header("Modules")]
    [SerializeField] private List<ModuleDefinition> modules = new();
    [SerializeField] private Transform content;
    [SerializeField] private ModuleButtonUI buttonPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;

    [Header("Progress")]
    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float maxProgressWidth = 500f;

    [Header("Queue")]
    [SerializeField] private ModuleQueueSlotUI[] queueSlots;

    private PlayerModuleCrafting playerCrafting;

    private void Start()
    {
        CreateModuleButtons();

        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (queueSlots[i] != null)
                queueSlots[i].Setup(i, this);
        }
    }

    private void Update()
    {
        if (playerCrafting == null)
            FindLocalPlayerCrafting();

        UpdateProgressBar();
        UpdateQueueUI();
    }

    private void CreateModuleButtons()
    {
        if (content == null)
        {
            Debug.LogError("[CRAFT ERROR] AssemblyPanelUI: content == null", gameObject);
            return;
        }

        if (buttonPrefab == null)
        {
            Debug.LogError("[CRAFT ERROR] AssemblyPanelUI: buttonPrefab == null", gameObject);
            return;
        }

        foreach (ModuleDefinition module in modules)
        {
            if (module == null)
            {
                Debug.LogError("[CRAFT ERROR] AssemblyPanelUI: modu³ na liœcie jest null", gameObject);
                continue;
            }

            ModuleButtonUI button = Instantiate(buttonPrefab, content);
            button.Setup(module, this);
        }
    }

    public void ShowModuleInfo(ModuleDefinition module)
    {
        if (module == null)
            return;

        if (nameText != null)
            nameText.text = module.displayName;

        if (descriptionText != null)
            descriptionText.text = module.description;

        if (costText != null)
        {
            costText.text =
                $"M: {module.metalCost} " +
                $"E: {module.energyCost} " +
                $"T: {module.craftTime}s";
        }
    }

    public void SelectModule(ModuleDefinition module)
    {
        Debug.Log(
            "[CRAFT 02] AssemblyPanelUI odebra³ modu³: " +
            (module != null ? module.moduleId : "NULL"));

        if (module == null)
        {
            Debug.LogError("[CRAFT ERROR] SelectModule: module == null");
            return;
        }

        if (playerCrafting == null)
            FindLocalPlayerCrafting();

        if (playerCrafting == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] Nie znaleziono lokalnego PlayerModuleCrafting");
            return;
        }

        Debug.Log(
            "[CRAFT 03] Znaleziono PlayerModuleCrafting. " +
            "OwnerClientId=" + playerCrafting.OwnerClientId +
            " LocalClientId=" + NetworkManager.Singleton.LocalClientId);

        playerCrafting.RequestCraft(module);
    }

    private void FindLocalPlayerCrafting()
    {
        if (NetworkManager.Singleton == null)
            return;

        PlayerModuleCrafting[] all =
            FindObjectsByType<PlayerModuleCrafting>(
                FindObjectsSortMode.None);

        foreach (PlayerModuleCrafting crafting in all)
        {
            if (crafting.IsOwner)
            {
                playerCrafting = crafting;

                Debug.Log(
                    "[CRAFT DEBUG] Lokalny PlayerModuleCrafting znaleziony");

                return;
            }
        }
    }

    private void UpdateProgressBar()
    {
        if (playerCrafting == null || progressBar == null)
            return;

        Vector2 size = progressBar.sizeDelta;
        size.x =
            maxProgressWidth *
            Mathf.Clamp01(playerCrafting.currentProgress.Value);

        progressBar.sizeDelta = size;
    }

    private void UpdateQueueUI()
    {
        if (playerCrafting == null)
            return;

        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (queueSlots[i] == null)
                continue;

            if (i < playerCrafting.moduleQueue.Count)
            {
                string id =
                    playerCrafting.moduleQueue[i].ToString();

                ModuleDefinition module =
                    ModuleDatabase.Instance.GetModule(id);

                if (module != null)
                    queueSlots[i].SetModule(module);
                else
                    queueSlots[i].SetEmpty();
            }
            else
            {
                queueSlots[i].SetEmpty();
            }
        }
    }

    public void RemoveQueueItem(int index)
    {
        if (playerCrafting != null)
            playerCrafting.RequestRemoveFromQueue(index);
    }
}