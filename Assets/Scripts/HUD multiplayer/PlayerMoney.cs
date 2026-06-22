using Unity.Netcode;
using UnityEngine;

public class PlayerMoney : NetworkBehaviour
{
    public NetworkVariable<int> money = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        money.OnValueChanged += OnMoneyChanged;
    }

    public override void OnNetworkDespawn()
    {
        money.OnValueChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int oldValue, int newValue)
    {
        Debug.Log($"Player {OwnerClientId} money: {newValue}");
    }

    [ServerRpc]
    public void AddMoneyServerRpc(int amount)
    {
        money.Value += amount;
    }

    [ServerRpc]
    public void RemoveMoneyServerRpc(int amount)
    {
        money.Value -= amount;
    }
}