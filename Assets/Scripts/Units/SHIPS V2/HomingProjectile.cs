using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HomingProjectile : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float hitDistance = 0.25f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float maximumLifetime = 30f;

    [Header("AOE Visual")]
    [SerializeField] private GameObject aoeImpactPrefab;

    [Header("Chain Visual")]
    [SerializeField] private LineRenderer chainLinePrefab;

    [SerializeField] private float chainVisualLifetime = 0.2f;

    private ShipUnit target;

    private float hullDamage;
    private float shieldDamage;
    private float moveSpeed;

    private ulong attackerOwnerId;

    // =========================================================
    // SLOW
    // =========================================================

    private bool canSlowOnHit;
    private float slowPercent;
    private float slowDuration;

    // =========================================================
    // AOE
    // =========================================================

    private bool hasAoe;
    private float aoeRange;
    private float aoeDamageMultiplier;

    // =========================================================

    private float spawnTime;

    private bool initialized;
    private bool hasHit;

    public void Initialize(
    ShipUnit newTarget,
    float newHullDamage,
    float newShieldDamage,
    float newMoveSpeed,
    ulong newAttackerOwnerId,
    bool newCanSlowOnHit,
    float newSlowPercent,
    float newSlowDuration,
    bool newHasAoe,
    float newAoeRange,
    float newAoeDamageMultiplier,
    bool newCanChainAttack,
    int newMaxTargets,
    float newChainJumpsRange,
    float newChainDamageMultiplier)
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

        attackerOwnerId =
            newAttackerOwnerId;

        // SLOW

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

        // AOE

        hasAoe =
            newHasAoe;

        aoeRange =
            Mathf.Max(
                0f,
                newAoeRange);

        aoeDamageMultiplier =
            Mathf.Max(
                0f,
                newAoeDamageMultiplier);

        // CHAIN

        canChainAttack =
            newCanChainAttack;

        maxTargets =
            Mathf.Max(
                0,
                newMaxTargets);

        chainJumpsRange =
            Mathf.Max(
                0f,
                newChainJumpsRange);

        chainDamageMultiplier =
            Mathf.Max(
                0f,
                newChainDamageMultiplier);

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

    // =========================================================
    // HIT
    // =========================================================

    private void HitTarget()
    {
        if (hasHit)
            return;

        hasHit = true;

        if (!IsTargetValid())
        {
            DespawnProjectile();
            return;
        }

        /*
         * Zapamiêtujemy miejsce trafienia,
         * poniewa¿ g³ówny cel mo¿e umrzeæ
         * od tego hita.
         */
        Vector3 hitPosition =
            target.transform.position;

        ShipUnit primaryTarget =
            target;

        // =====================================================
        // NORMALNY HIT
        // =====================================================

        primaryTarget.TakeWeaponDamage(
            hullDamage,
            shieldDamage);

        if (canSlowOnHit &&
            primaryTarget != null &&
            primaryTarget.IsSpawned &&
            !primaryTarget.isDead.Value)
        {
            primaryTarget.ApplySlow(
                slowPercent,
                slowDuration);
        }

        // =====================================================
        // AOE
        // =====================================================

        if (hasAoe &&
            aoeRange > 0f)
        {
            ApplyAoeDamage(
                primaryTarget,
                hitPosition);

            ShowAoeImpactClientRpc(
                hitPosition,
                aoeRange);
        }

        // =====================================================
        // CHAIN
        // =====================================================

        if (canChainAttack &&
            maxTargets > 0 &&
            chainJumpsRange > 0f)
        {
            ApplyChainAttack(
                primaryTarget);
        }

        DespawnProjectile();
    }

    // =========================================================
    // AOE DAMAGE
    // =========================================================

    private void ApplyAoeDamage(
        ShipUnit primaryTarget,
        Vector3 hitPosition)
    {
        if (!IsServer)
            return;

        float aoeHullDamage =
            hullDamage *
            aoeDamageMultiplier;

        float aoeShieldDamage =
            shieldDamage *
            aoeDamageMultiplier;

        float rangeSquared =
            aoeRange *
            aoeRange;

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit aoeTarget in allShips)
        {
            if (aoeTarget == null)
                continue;

            /*
             * G³ówny cel dosta³ ju¿
             * normalny hit.
             */
            if (aoeTarget == primaryTarget)
                continue;

            if (!aoeTarget.IsSpawned)
                continue;

            if (aoeTarget.isDead.Value)
                continue;

            /*
             * Bez friendly fire.
             */
            if (aoeTarget.ownerId.Value ==
                attackerOwnerId)
            {
                continue;
            }

            Vector3 offset =
                aoeTarget.transform.position -
                hitPosition;

            if (offset.sqrMagnitude >
                rangeSquared)
            {
                continue;
            }

            aoeTarget.TakeWeaponDamage(
                aoeHullDamage,
                aoeShieldDamage);

            if (canSlowOnHit &&
                aoeTarget.IsSpawned &&
                !aoeTarget.isDead.Value)
            {
                aoeTarget.ApplySlow(
                    slowPercent,
                    slowDuration);
            }
        }
    }

    // =========================================================
    // AOE VISUAL
    // =========================================================

    [ClientRpc]
    private void ShowAoeImpactClientRpc(
        Vector3 position,
        float radius)
    {
        if (aoeImpactPrefab == null)
            return;

        GameObject visual =
            Instantiate(
                aoeImpactPrefab,
                position,
                Quaternion.identity);

        AoeImpactVisual impact =
            visual.GetComponent<AoeImpactVisual>();

        if (impact != null)
        {
            impact.Initialize(
                radius);
        }
    }

    // =========================================================
    // CHAIN
    // =========================================================

    private bool canChainAttack;
    private int maxTargets;
    private float chainJumpsRange;
    private float chainDamageMultiplier;

    // =========================================================
    // CHAIN ATTACK
    // =========================================================

    private void ApplyChainAttack(
        ShipUnit primaryTarget)
    {
        if (!IsServer)
            return;

        if (primaryTarget == null)
            return;

        List<ShipUnit> alreadyHit =
            new List<ShipUnit>();

        alreadyHit.Add(
            primaryTarget);

        ShipUnit currentTarget =
            primaryTarget;

        float currentHullDamage =
            hullDamage;

        float currentShieldDamage =
            shieldDamage;

        for (int jump = 0;
             jump < maxTargets;
             jump++)
        {
            ShipUnit nextTarget =
                FindNearestChainTarget(
                    currentTarget,
                    alreadyHit);

            if (nextTarget == null)
                break;

            /*
             * Ka¿dy kolejny skok mno¿y damage
             * poprzedniego skoku.
             */
            currentHullDamage *=
                chainDamageMultiplier;

            currentShieldDamage *=
                chainDamageMultiplier;

            Vector3 chainStart =
                currentTarget.transform.position;

            Vector3 chainEnd =
                nextTarget.transform.position;

            ShowChainVisualClientRpc(
                chainStart,
                chainEnd);

            nextTarget.TakeWeaponDamage(
                currentHullDamage,
                currentShieldDamage);

            /*
             * Chain jest pe³noprawnym trafieniem
             * dla efektu Slow.
             */
            if (canSlowOnHit &&
                nextTarget.IsSpawned &&
                !nextTarget.isDead.Value)
            {
                nextTarget.ApplySlow(
                    slowPercent,
                    slowDuration);
            }

            /*
             * UWAGA:
             * tutaj NIE wywo³ujemy ApplyAoeDamage().
             * AOE odpala wy³¹cznie g³ówny hit.
             */

            alreadyHit.Add(
                nextTarget);

            currentTarget =
                nextTarget;
        }
    }

    private ShipUnit FindNearestChainTarget(
    ShipUnit fromTarget,
    List<ShipUnit> alreadyHit)
    {
        if (fromTarget == null)
            return null;

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        ShipUnit nearest = null;

        float nearestDistanceSquared =
            chainJumpsRange *
            chainJumpsRange;

        foreach (ShipUnit candidate in allShips)
        {
            if (candidate == null)
                continue;

            if (!candidate.IsSpawned)
                continue;

            if (candidate.isDead.Value)
                continue;

            // Bez friendly fire.
            if (candidate.ownerId.Value ==
                attackerOwnerId)
            {
                continue;
            }

            // Nie wracamy na ju¿ trafiony statek.
            if (alreadyHit.Contains(candidate))
                continue;

            Vector3 offset =
                candidate.transform.position -
                fromTarget.transform.position;

            float distanceSquared =
                offset.sqrMagnitude;

            if (distanceSquared >
                nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared =
                distanceSquared;

            nearest =
                candidate;
        }

        return nearest;
    }

    [ClientRpc]
    private void ShowChainVisualClientRpc(
    Vector3 startPosition,
    Vector3 endPosition)
    {
        if (chainLinePrefab == null)
            return;

        LineRenderer line =
            Instantiate(
                chainLinePrefab);

        line.positionCount = 2;
        line.useWorldSpace = true;

        line.SetPosition(
            0,
            startPosition);

        line.SetPosition(
            1,
            endPosition);

        Destroy(
            line.gameObject,
            chainVisualLifetime);
    }

    // =========================================================
    // DESPAWN
    // =========================================================

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