using Unity.Netcode;
using UnityEngine;

public class BaseHealingAura : NetworkBehaviour
{
    [SerializeField] private float healingRange = 8f;
    [SerializeField] private int healPerTick = 5;
    [SerializeField] private float healTickTime = 1f;

    [SerializeField] private Transform rangeCircle;

    private UnitOwner owner;
    private float timer;

    public override void OnNetworkSpawn()
    {
        owner = GetComponent<UnitOwner>();
        UpdateRangeCircle();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        timer += Time.deltaTime;

        if (timer < healTickTime)
            return;

        timer = 0f;
        HealShipsInRange();
    }

    private void HealShipsInRange()
    {
        NetworkHealth[] healths = FindObjectsByType<NetworkHealth>(FindObjectsSortMode.None);

        foreach (NetworkHealth health in healths)
        {
            if (health.isDead.Value)
                continue;

            if (health.gameObject == gameObject)
                continue;

            UnitOwner targetOwner = health.GetComponent<UnitOwner>();

            if (targetOwner == null || owner == null)
                continue;

            if (targetOwner.ownerId.Value != owner.ownerId.Value)
                continue;

            float distance = Vector3.Distance(transform.position, health.transform.position);

            if (distance <= healingRange)
            {
                health.Heal(healPerTick);
            }
        }
    }

    private void UpdateRangeCircle()
    {
        if (rangeCircle == null)
            return;

        float diameter = healingRange * 2f;
        rangeCircle.localScale = new Vector3(diameter, 0.01f, diameter);
    }
}