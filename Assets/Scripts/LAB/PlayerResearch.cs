using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerResearch : NetworkBehaviour
{
    public const int MaxQueue = 5;

    public NetworkList<FixedString64Bytes> researchQueue;

    public NetworkVariable<FixedString64Bytes> currentResearchId = new();
    public NetworkVariable<float> currentProgress = new(0f);

    private PlayerResources resources;
    private ResearchDefinition currentResearch;
    private PlayerUpgradeStats upgrades;


    public NetworkList<FixedString64Bytes> completedResearches;
    public NetworkList<FixedString64Bytes> unlockedModules;


    private BaseSafeZone safeZone;

    private void Awake()
    {
        resources =
            GetComponent<PlayerResources>();

        upgrades =
            GetComponent<PlayerUpgradeStats>();

        researchQueue =
            new NetworkList<FixedString64Bytes>();

        completedResearches =
            new NetworkList<FixedString64Bytes>();

        unlockedModules =
            new NetworkList<FixedString64Bytes>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (safeZone == null)
            FindSafeZone();

        ProcessQueue();
    }

    private void FindSafeZone()
    {
        BaseSafeZone[] safeZones =
            FindObjectsByType<BaseSafeZone>(
                FindObjectsSortMode.None);

        foreach (BaseSafeZone zone in safeZones)
        {
            if (!zone.IsSpawned)
                continue;

            UnitOwner baseOwner =
                zone.GetComponent<UnitOwner>();

            if (baseOwner == null)
                continue;

            if (baseOwner.ownerId.Value !=
                OwnerClientId)
            {
                continue;
            }

            safeZone = zone;
            return;
        }
    }

    public void RequestResearch(ResearchDefinition research)
    {
        if (research == null)
            return;

        RequestResearchServerRpc(research.researchId);
    }

    [ServerRpc]
    private void RequestResearchServerRpc(
    string researchId)
    {
        ResearchDefinition research =
            ResearchDatabase.Instance.GetResearch(
                researchId);

        if (research == null)
            return;

        // =====================================================
        // CORE TIER CHECK
        // =====================================================

        BaseCore core =
            FindOwnedCore();

        if (core == null)
        {
            Debug.LogWarning(
                "[RESEARCH BLOCKED] Nie znaleziono Core gracza.");

            return;
        }

        if ((int)research.tier >
            core.tier.Value)
        {
            Debug.LogWarning(
                $"[RESEARCH BLOCKED] " +
                $"{research.researchId} wymaga Core Tier " +
                $"{(int)research.tier}, " +
                $"a gracz ma Tier {core.tier.Value}.");

            return;
        }

        // =====================================================
        // NORMAL VALIDATION
        // =====================================================

        if (IsCompleted(researchId))
            return;

        if (IsInQueue(researchId))
            return;

        if (researchQueue.Count >= MaxQueue)
            return;

        if (!resources.CanAfford(
                research.baseMetalCost,
                research.baseEnergyCost))
        {
            return;
        }

        resources.Spend(
            research.baseMetalCost,
            research.baseEnergyCost);

        researchQueue.Add(
            new FixedString64Bytes(
                researchId));
    }

    public void RequestRemoveFromQueue(int index)
    {
        RemoveFromQueueServerRpc(index);
    }

    [ServerRpc]
    private void RemoveFromQueueServerRpc(int index)
    {
        if (index < 0 || index >= researchQueue.Count)
            return;

        string id = researchQueue[index].ToString();
        ResearchDefinition research = ResearchDatabase.Instance.GetResearch(id);

        if (research != null)
        {
            resources.AddMetal(research.baseMetalCost);
            resources.AddEnergy(research.baseEnergyCost);
        }

        researchQueue.RemoveAt(index);

        if (index == 0)
        {
            currentResearch = null;
            currentProgress.Value = 0f;
            currentResearchId.Value = "";
        }
    }

    private void ProcessQueue()
    {
        if (researchQueue.Count == 0)
        {
            currentResearch = null;
            currentResearchId.Value = "";
            currentProgress.Value = 0f;
            return;
        }

        if (currentResearch == null)
        {
            string id = researchQueue[0].ToString();
            currentResearch = ResearchDatabase.Instance.GetResearch(id);
            currentResearchId.Value = researchQueue[0];
        }

        if (currentResearch == null)
            return;

        float baseResearchTime =
            Mathf.Max(
            0f,
            currentResearch.baseResearchTime);

        // =====================================================
        // BONUSES
        // =====================================================

        float labBonusPercent =
            safeZone != null
                ? safeZone.GetLabSpeedBonusPercent()
                : 0f;

        float researchBonusPercent =
            upgrades != null
                ? upgrades.GetResearchSpeedBonusPercent()
                : 0f;

        // Najpierw SUMUJEMY wszystkie bonusy.
        float totalBonusPercent =
            labBonusPercent +
            researchBonusPercent;

        // Dopiero sumê ograniczamy do 100%.
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
            baseResearchTime *
            timeMultiplier;

        // =====================================================
        // PROGRESS
        // =====================================================

        if (finalResearchTime <= 0f)
        {
            currentProgress.Value = 1f;
        }
        else
        {
            currentProgress.Value +=
                Time.deltaTime /
                finalResearchTime;
        }

        if (currentProgress.Value >= 1f)
        {
            CompleteResearch(currentResearch);

            researchQueue.RemoveAt(0);

            currentResearch = null;
            currentResearchId.Value = default;
            currentProgress.Value = 0f;
        }
    }

    private void CompleteResearch(
    ResearchDefinition research)
    {
        if (research == null)
            return;

        // =========================================================
        // COMPLETED RESEARCH
        // =========================================================

        if (!IsCompleted(
                research.researchId))
        {
            completedResearches.Add(
                new FixedString64Bytes(
                    research.researchId));
        }

        // =========================================================
        // APPLY RESEARCH EFFECT
        // =========================================================

        PlayerUpgradeStats upgrades =
            GetComponent<PlayerUpgradeStats>();

        if (upgrades != null)
        {
            upgrades.ApplyResearch(
                research);
        }

        // =========================================================
        // MODULE UNLOCK
        // =========================================================

        if (research.effectType ==
            ResearchEffectType.UnlockModules)
        {
            UnlockModulesFromResearch(
                research);
        }

        ShowResearchCompletedClientRpc(
            research.researchId);

        Debug.Log(
            $"[RESEARCH COMPLETE] " +
            $"{research.displayName} | " +
            $"ID={research.researchId}");
    }

    [ClientRpc]
    private void ShowResearchCompletedClientRpc(
    string researchId)
    {
        // PlayerResearch istnieje na wszystkich klientach,
        // ale popup pokazujemy tylko w³aœcicielowi.
        if (!IsOwner)
            return;

        if (ResearchDatabase.Instance == null)
            return;

        ResearchDefinition research =
            ResearchDatabase.Instance.GetResearch(
                researchId);

        if (research == null)
            return;

        NotificationManager notificationManager =
            FindFirstObjectByType<NotificationManager>();

        if (notificationManager == null)
        {
            Debug.LogWarning(
                "[NOTIFICATION] Nie znaleziono NotificationManager.");

            return;
        }

        notificationManager.ShowResearchCompleted(
            research);
    }

    private bool IsInQueue(string researchId)
    {
        for (int i = 0; i < researchQueue.Count; i++)
        {
            if (researchQueue[i].ToString() == researchId)
                return true;
        }

        return false;
    }

    public bool IsCompleted(
    string researchId)
    {
        for (int i = 0;
             i < completedResearches.Count;
             i++)
        {
            if (completedResearches[i].ToString() ==
                researchId)
            {
                return true;
            }
        }

        return false;
    }
    private void UnlockModulesFromResearch(
    ResearchDefinition research)
    {
        if (!IsServer)
            return;

        if (research == null)
            return;

        if (research.unlockedModuleIds == null)
            return;

        foreach (string moduleId in
                 research.unlockedModuleIds)
        {
            if (string.IsNullOrWhiteSpace(
                    moduleId))
            {
                continue;
            }

            // Opcjonalne zabezpieczenie:
            // sprawdzamy, czy taki modu³
            // rzeczywiœcie istnieje.
            if (ModuleDatabase.Instance == null)
                continue;

            ModuleDefinition module =
                ModuleDatabase.Instance.GetModule(
                    moduleId);

            if (module == null)
            {
                Debug.LogWarning(
                    $"[RESEARCH UNLOCK] " +
                    $"Nie znaleziono modu³u " +
                    $"o ID={moduleId}");

                continue;
            }

            if (IsModuleInUnlockedList(
                    moduleId))
            {
                continue;
            }

            unlockedModules.Add(
                new FixedString64Bytes(
                    moduleId));

            Debug.Log(
                $"[RESEARCH UNLOCK] " +
                $"Odblokowano modu³: " +
                $"{moduleId}");
        }
    }
    private bool IsModuleInUnlockedList(
    string moduleId)
    {
        for (int i = 0;
             i < unlockedModules.Count;
             i++)
        {
            if (unlockedModules[i].ToString() ==
                moduleId)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsModuleUnlocked(
    ModuleDefinition module)
    {
        if (module == null)
            return false;

        // Modu³ dostêpny od pocz¹tku gry.
        if (module.unlockedByDefault)
            return true;

        // Modu³ wymagaj¹cy researchu.
        return IsModuleInUnlockedList(
            module.moduleId);
    }

    public bool IsModuleUnlocked(
    string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return false;

        if (ModuleDatabase.Instance == null)
            return false;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId);

        return IsModuleUnlocked(
            module);
    }

    private BaseCore FindOwnedCore()
    {
        BaseCore[] cores =
            FindObjectsByType<BaseCore>(
                FindObjectsSortMode.None);

        foreach (BaseCore core in cores)
        {
            if (core == null)
                continue;

            if (!core.IsSpawned)
                continue;

            if (core.OwnerClientId ==
                OwnerClientId)
            {
                return core;
            }
        }

        return null;
    }
}