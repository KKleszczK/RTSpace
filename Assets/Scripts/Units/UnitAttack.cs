using Unity.Netcode;
using UnityEngine;

public class UnitAttack : NetworkBehaviour
{
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private BaseAttack attackBehaviour;

    private UnitOwner owner;
    private NetworkHealth myHealth;

    public NetworkVariable<float> attackProgress = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool CanAttack => attackProgress.Value >= 1f;

    public override void OnNetworkSpawn()
    {
        owner = GetComponent<UnitOwner>();
        myHealth = GetComponent<NetworkHealth>();

        if (attackBehaviour == null)
            attackBehaviour = GetComponent<BaseAttack>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (myHealth != null && myHealth.isDead.Value)
            return;

        UpdateCooldown();

        if (CanAttack)
            TryAttack();
    }

    private void UpdateCooldown()
    {
        if (attackProgress.Value >= 1f)
            return;

        attackProgress.Value += Time.deltaTime / attackCooldown;

        if (attackProgress.Value > 1f)
            attackProgress.Value = 1f;
    }

    private void TryAttack()
    {
        if (attackBehaviour == null)
            return;

        NetworkHealth target = FindEnemyInRange();

        if (target == null)
            return;

        attackBehaviour.Attack(target);

        attackProgress.Value = 0f;
    }

    private NetworkHealth FindEnemyInRange()
    {
        NetworkHealth[] healths = FindObjectsByType<NetworkHealth>(FindObjectsSortMode.None);

        NetworkHealth closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (NetworkHealth health in healths)
        {
            if (health == null || health == myHealth)
                continue;

            if (health.isDead.Value)
                continue;

            UnitOwner targetOwner = health.GetComponent<UnitOwner>();

            if (targetOwner == null || owner == null)
                continue;

            if (!owner.IsEnemy(targetOwner))
                continue;

            float distance = Vector3.Distance(transform.position, health.transform.position);

            if (distance <= attackRange && distance < closestDistance)
            {
                closestTarget = health;
                closestDistance = distance;
            }
        }

        return closestTarget;
    }
}