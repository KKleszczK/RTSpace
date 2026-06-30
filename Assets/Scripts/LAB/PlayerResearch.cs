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

    private HashSet<string> completedResearches = new();

    private void Awake()
    {
        resources = GetComponent<PlayerResources>();
        researchQueue = new NetworkList<FixedString64Bytes>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        ProcessQueue();
    }

    public void RequestResearch(ResearchDefinition research)
    {
        if (research == null)
            return;

        RequestResearchServerRpc(research.researchId);
    }

    [ServerRpc]
    private void RequestResearchServerRpc(string researchId)
    {
        ResearchDefinition research = ResearchDatabase.Instance.GetResearch(researchId);

        if (research == null)
            return;

        if (completedResearches.Contains(researchId))
            return;

        if (IsInQueue(researchId))
            return;

        if (researchQueue.Count >= MaxQueue)
            return;

        if (!resources.CanAfford(research.baseMetalCost, research.baseEnergyCost))
            return;

        resources.Spend(research.baseMetalCost, research.baseEnergyCost);

        researchQueue.Add(new FixedString64Bytes(researchId));
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

        currentProgress.Value += Time.deltaTime / currentResearch.baseResearchTime;

        if (currentProgress.Value >= 1f)
        {
            CompleteResearch(currentResearch);

            researchQueue.RemoveAt(0);
            currentResearch = null;
            currentProgress.Value = 0f;
        }
    }

    private void CompleteResearch(ResearchDefinition research)
    {
        completedResearches.Add(research.researchId);

        

        PlayerUpgradeStats upgrades = GetComponent<PlayerUpgradeStats>();

        if (upgrades != null)
            upgrades.ApplyResearch(research);

        Debug.Log("Research complete: " + research.displayName);
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

    public bool IsCompleted(string researchId)
    {
        return completedResearches.Contains(researchId);
    }
}