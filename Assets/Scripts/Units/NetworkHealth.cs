using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [SerializeField] private int maxHp = 100;

    public NetworkVariable<int> hp = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int GetHealth()
    {
        return hp.Value;
    }

    public int GetMaxHealth()
    {
        return maxHp;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hp.Value = maxHp;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        hp.Value -= damage;

        if (hp.Value <= 0)
        {
            hp.Value = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        hp.Value += amount;

        if (hp.Value > maxHp)
            hp.Value = maxHp;
    }

    private void Die()
    {
        isDead.Value = true;

        NetworkObject networkObject = GetComponent<NetworkObject>();

        if (networkObject != null)
        {
            networkObject.Despawn(true);
        }
    }
}