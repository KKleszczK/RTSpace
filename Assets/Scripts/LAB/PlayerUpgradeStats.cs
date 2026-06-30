using Unity.Netcode;
using UnityEngine;

public class PlayerUpgradeStats : NetworkBehaviour
{
    public NetworkVariable<float> shipHpBonusPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void ApplyResearch(ResearchDefinition research)
    {
        if (!IsServer)
            return;

        if (research == null)
            return;

        switch (research.researchId)
        {
            case "ship_hull_hp":
                shipHpBonusPercent.Value += research.upgradeValue / 100f;

                Debug.Log("HP BONUS NOW: " + shipHpBonusPercent.Value);

                RefreshOwnedShips();
                break;
        }
    }

    private void RefreshOwnedShips()
    {
        UnitOwner[] owners = FindObjectsByType<UnitOwner>(FindObjectsSortMode.None);

        foreach (UnitOwner owner in owners)
        {
            if (owner.ownerId.Value != OwnerClientId)
                continue;

            ShipStats stats = owner.GetComponent<ShipStats>();

            if (stats != null)
                stats.RecalculateStats();

            NetworkHealth health = owner.GetComponent<NetworkHealth>();
            if (health != null)
                health.RecalculateHealthFromStats();
        }
    }
}