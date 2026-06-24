using Unity.Netcode;
using UnityEngine;

public class UnitOwner : NetworkBehaviour
{
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private Renderer unitRenderer;

    public override void OnNetworkSpawn()
    {
        ownerId.OnValueChanged += OnOwnerChanged;
        ApplyColor();
    }

    public override void OnNetworkDespawn()
    {
        ownerId.OnValueChanged -= OnOwnerChanged;
    }

    public bool IsMine()
    {
        return ownerId.Value == NetworkManager.Singleton.LocalClientId;
    }

    public bool IsEnemy(UnitOwner other)
    {
        if (other == null)
            return false;

        return ownerId.Value != other.ownerId.Value;
    }

    public void SetOwner(ulong newOwnerId)
    {
        if (!IsServer)
            return;

        ownerId.Value = newOwnerId;
    }

    private void OnOwnerChanged(ulong oldValue, ulong newValue)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (unitRenderer == null)
            unitRenderer = GetComponent<Renderer>();

        if (unitRenderer == null)
            return;

        unitRenderer.material.color = ownerId.Value == 0
            ? Color.blue
            : Color.red;
    }
}