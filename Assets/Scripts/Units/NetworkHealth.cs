using Unity.Netcode;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    private ShipStats shipStats;

    public NetworkVariable<int> hp = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> maxHp = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new(
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
        return maxHp.Value;
    }

    public override void OnNetworkSpawn()
    {
        shipStats = GetComponent<ShipStats>();

        if (IsServer)
            RecalculateHealthFromStats(true);
    }



    public void RecalculateHealthFromStats(bool fullHeal = false)
    {
        if (!IsServer)
            return;

        int oldMaxHp = maxHp.Value;

        if (shipStats != null)
            maxHp.Value = shipStats.MaxHp;

        if (fullHeal)
        {
            hp.Value = maxHp.Value;
        }
        else
        {
            int diff = maxHp.Value - oldMaxHp;
            hp.Value += diff;

            if (hp.Value > maxHp.Value)
                hp.Value = maxHp.Value;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || isDead.Value)
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
        if (!IsServer || isDead.Value)
            return;

        hp.Value += amount;

        if (hp.Value > maxHp.Value)
            hp.Value = maxHp.Value;
    }

    private void Die()
    {
        isDead.Value = true;
        NetworkObject.Despawn(true);
    }


    
}