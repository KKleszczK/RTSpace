using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AssemblyPanelUI : MonoBehaviour
{
    [SerializeField] private List<ModuleDefinition> modules = new();
    [SerializeField] private Transform content;
    [SerializeField] private ModuleButtonUI buttonPrefab;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;

    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float maxProgressWidth = 500f;

    [SerializeField] private ModuleQueueSlotUI[] queueSlots;

    private PlayerModuleCrafting playerCrafting;

    private void Start()
    {
        FindLocalPlayerCrafting();
        foreach (ModuleDefinition module in modules)
        {
            ModuleButtonUI button = Instantiate(buttonPrefab, content);
            button.Setup(module, this);
        }

        for (int i = 0; i < queueSlots.Length; i++)
            queueSlots[i].Setup(i, this);
    }
    private void Update()
    {
        UpdateProgressBar();
        UpdateQueueUI();
    }

    public void ShowModuleInfo(ModuleDefinition module)
    {
        nameText.text = module.displayName;
        descriptionText.text = module.description;
        costText.text = $"M: {module.metalCost} E: {module.energyCost} T: {module.craftTime}s";
    }

    public void SelectModule(ModuleDefinition module)
    {
        if (playerCrafting == null)
            FindLocalPlayerCrafting();

        if (playerCrafting != null)
            playerCrafting.RequestCraft(module);
    }

    private void FindLocalPlayerCrafting()
    {
        PlayerModuleCrafting[] all =
            FindObjectsByType<PlayerModuleCrafting>(FindObjectsSortMode.None);

        foreach (PlayerModuleCrafting crafting in all)
        {
            if (crafting.OwnerClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                playerCrafting = crafting;
                return;
            }
        }
    }
    private void UpdateProgressBar()
    {
        if (playerCrafting == null || progressBar == null)
            return;

        Vector2 size = progressBar.sizeDelta;
        size.x = maxProgressWidth * playerCrafting.currentProgress.Value;
        progressBar.sizeDelta = size;
    }

    private void UpdateQueueUI()
    {
        if (playerCrafting == null)
            return;

        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (i < playerCrafting.moduleQueue.Count)
            {
                string id = playerCrafting.moduleQueue[i].ToString();
                ModuleDefinition module = ModuleDatabase.Instance.GetModule(id);

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