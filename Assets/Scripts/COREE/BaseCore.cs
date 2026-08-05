using Unity.Netcode;
using UnityEngine;

public class BaseCore : NetworkBehaviour
{
    [System.Serializable]
    public class CoreUpgradeData
    {
        [Min(0)]
        public int metalCost;

        [Min(0)]
        public int energyCost;

        [Min(0.01f)]
        public float upgradeTime = 1f;
    }

    public NetworkVariable<int> tier = new(1);

    public NetworkVariable<float> progress = new(0f);

    public NetworkVariable<bool> isUpgrading = new(false);

    [Header("Upgrade T1 -> T2")]
    [SerializeField]
    private CoreUpgradeData upgradeToTier2 =
        new CoreUpgradeData();

    [Header("Upgrade T2 -> T3")]
    [SerializeField]
    private CoreUpgradeData upgradeToTier3 =
        new CoreUpgradeData();

    private PlayerResources resources;
    private float currentUpgradeTime;

    private void Update()
    {
        if (!IsServer)
            return;

        if (!isUpgrading.Value)
            return;

        if (currentUpgradeTime <= 0f)
        {
            CancelUpgrade();
            return;
        }

        progress.Value +=
            Time.deltaTime / currentUpgradeTime;

        if (progress.Value < 1f)
            return;

        tier.Value =
            Mathf.Min(tier.Value + 1, 3);

        progress.Value = 0f;
        isUpgrading.Value = false;
        currentUpgradeTime = 0f;
    }

    public void RequestUpgrade()
    {
        RequestUpgradeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestUpgradeServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (isUpgrading.Value)
            return;

        if (tier.Value >= 3)
            return;

        ulong senderId =
            rpcParams.Receive.SenderClientId;

        resources =
            FindPlayerResources(senderId);

        if (resources == null)
            return;

        CoreUpgradeData upgradeData =
            GetUpgradeDataForCurrentTier();

        if (upgradeData == null)
            return;

        int metalCost =
            Mathf.Max(0, upgradeData.metalCost);

        int energyCost =
            Mathf.Max(0, upgradeData.energyCost);

        float upgradeTime =
            Mathf.Max(0.01f, upgradeData.upgradeTime);

        if (!resources.CanAfford(
                metalCost,
                energyCost))
        {
            return;
        }

        resources.Spend(
            metalCost,
            energyCost);

        currentUpgradeTime =
            upgradeTime;

        progress.Value = 0f;
        isUpgrading.Value = true;
    }

    private CoreUpgradeData GetUpgradeDataForCurrentTier()
    {
        return tier.Value switch
        {
            1 => upgradeToTier2,
            2 => upgradeToTier3,
            _ => null
        };
    }

    private void CancelUpgrade()
    {
        progress.Value = 0f;
        isUpgrading.Value = false;
        currentUpgradeTime = 0f;
    }

    private PlayerResources FindPlayerResources(
        ulong clientId)
    {
        PlayerResources[] all =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        foreach (PlayerResources resource in all)
        {
            if (resource.OwnerClientId == clientId)
                return resource;
        }

        return null;
    }

    public int GetNextUpgradeMetalCost()
    {
        CoreUpgradeData data =
            GetUpgradeDataForCurrentTier();

        return data != null
            ? data.metalCost
            : 0;
    }

    public int GetNextUpgradeEnergyCost()
    {
        CoreUpgradeData data =
            GetUpgradeDataForCurrentTier();

        return data != null
            ? data.energyCost
            : 0;
    }

    public float GetNextUpgradeTime()
    {
        CoreUpgradeData data =
            GetUpgradeDataForCurrentTier();

        return data != null
            ? data.upgradeTime
            : 0f;
    }
}