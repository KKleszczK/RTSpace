using Unity.Netcode;
using UnityEngine;

public class BaseCore : NetworkBehaviour
{
    public NetworkVariable<int> tier = new(1);
    public NetworkVariable<float> progress = new(0f);
    public NetworkVariable<bool> isUpgrading = new(false);

    private PlayerResources resources;
    private float upgradeTime;

    private void Update()
    {
        if (!IsServer || !isUpgrading.Value)
            return;

        progress.Value += Time.deltaTime / upgradeTime;

        if (progress.Value >= 1f)
        {
            tier.Value++;
            progress.Value = 0f;
            isUpgrading.Value = false;
        }
    }

    public void RequestUpgrade()
    {
        RequestUpgradeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestUpgradeServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isUpgrading.Value || tier.Value >= 3)
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        resources = FindPlayerResources(senderId);
        if (resources == null)
            return;

        int metalCost = tier.Value == 1 ? 100 : 200;
        int energyCost = tier.Value == 1 ? 200 : 500;
        upgradeTime = tier.Value == 1 ? 10f : 30f;

        if (!resources.CanAfford(metalCost, energyCost))
            return;

        resources.Spend(metalCost, energyCost);

        progress.Value = 0f;
        isUpgrading.Value = true;
    }

    private PlayerResources FindPlayerResources(ulong clientId)
    {
        PlayerResources[] all = FindObjectsByType<PlayerResources>(FindObjectsSortMode.None);

        foreach (PlayerResources r in all)
        {
            if (r.OwnerClientId == clientId)
                return r;
        }

        return null;
    }
}