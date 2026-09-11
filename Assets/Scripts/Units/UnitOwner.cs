using Unity.Netcode;
using UnityEngine;

public class UnitOwner : NetworkBehaviour
{
    public NetworkVariable<ulong> ownerId =
        new NetworkVariable<ulong>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    [SerializeField] private Renderer[] unitRenderers;

    // =========================================================
    // NETWORK
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ownerId.OnValueChanged +=
            OnOwnerChanged;

        ApplyColor();
    }

    public override void OnNetworkDespawn()
    {
        ownerId.OnValueChanged -=
            OnOwnerChanged;

        base.OnNetworkDespawn();
    }

    // =========================================================
    // OWNER
    // =========================================================

    public bool IsMine()
    {
        if (NetworkManager.Singleton == null)
            return false;

        return ownerId.Value ==
            NetworkManager.Singleton.LocalClientId;
    }

    public bool IsEnemy(
        UnitOwner other)
    {
        if (other == null)
            return false;

        return ownerId.Value !=
            other.ownerId.Value;
    }

    public void SetOwner(
        ulong newOwnerId)
    {
        if (!IsServer)
            return;

        ownerId.Value =
            newOwnerId;
    }

    // =========================================================
    // COLOR
    // =========================================================

    private void OnOwnerChanged(
        ulong oldValue,
        ulong newValue)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        Color playerColor =
            PlayerColorHelper.GetColor(
                ownerId.Value);

        foreach (Renderer unitRenderer in unitRenderers)
        {
            if (unitRenderer == null)
                continue;

            unitRenderer.material.color =
                playerColor;
        }
    }
}