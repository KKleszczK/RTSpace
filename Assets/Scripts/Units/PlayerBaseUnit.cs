using UnityEngine;

public class PlayerBaseUnit : MonoBehaviour
{
    private UnitOwner owner;
    private NetworkHealth health;
    private BaseHealingAura healingAura;

    private void Awake()
    {
        owner = GetComponent<UnitOwner>();
        health = GetComponent<NetworkHealth>();
        healingAura = GetComponent<BaseHealingAura>();
    }

    public UnitOwner Owner => owner;
    public NetworkHealth Health => health;
    public BaseHealingAura HealingAura => healingAura;
}