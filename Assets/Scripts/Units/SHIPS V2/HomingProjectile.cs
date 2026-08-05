using Unity.Netcode;
using UnityEngine;

public class HomingProjectile : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float hitDistance = 0.25f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float maximumLifetime = 30f;

    private ShipUnit target;

    private float hullDamage;
    private float shieldDamage;
    private float moveSpeed;

    private bool canSlowOnHit;
    private float slowPercent;
    private float slowDuration;

    private float spawnTime;
    private bool initialized;
    private bool hasHit;

    public void Initialize(
        ShipUnit newTarget,
        float newHullDamage,
        float newShieldDamage,
        float newMoveSpeed,
        bool newCanSlowOnHit,
        float newSlowPercent,
        float newSlowDuration)
    {
        target =
            newTarget;

        hullDamage =
            Mathf.Max(
                0f,
                newHullDamage);

        shieldDamage =
            Mathf.Max(
                0f,
                newShieldDamage);

        moveSpeed =
            Mathf.Max(
                0.01f,
                newMoveSpeed);

        canSlowOnHit =
            newCanSlowOnHit;

        slowPercent =
            Mathf.Clamp(
                newSlowPercent,
                0f,
                100f);

        slowDuration =
            Mathf.Max(
                0f,
                newSlowDuration);

        initialized = true;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            spawnTime =
                Time.time;
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (!initialized ||
            hasHit)
        {
            return;
        }

        if (Time.time >=
            spawnTime + maximumLifetime)
        {
            DespawnProjectile();
            return;
        }

        if (!IsTargetValid())
        {
            DespawnProjectile();
            return;
        }

        UpdateMovement();
    }

    private bool IsTargetValid()
    {
        if (target == null)
            return false;

        if (!target.IsSpawned)
            return false;

        if (target.isDead.Value)
            return false;

        return true;
    }

    private void UpdateMovement()
    {
        Vector3 targetPosition =
            target.transform.position;

        Vector3 direction =
            targetPosition -
            transform.position;

        float distance =
            direction.magnitude;

        float movementThisFrame =
            moveSpeed *
            Time.deltaTime;

        if (distance <= hitDistance ||
            distance <= movementThisFrame)
        {
            HitTarget();
            return;
        }

        RotateTowardsTarget(
            direction);

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                movementThisFrame);
    }

    private void RotateTowardsTarget(
        Vector3 direction)
    {
        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime);
    }

    private void HitTarget()
    {
        if (hasHit)
            return;

        hasHit = true;

        if (IsTargetValid())
        {
            target.TakeWeaponDamage(
                hullDamage,
                shieldDamage);

            if (canSlowOnHit)
            {
                target.ApplySlow(
                    slowPercent,
                    slowDuration);
            }
        }

        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        if (!IsServer)
            return;

        if (NetworkObject != null &&
            NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}