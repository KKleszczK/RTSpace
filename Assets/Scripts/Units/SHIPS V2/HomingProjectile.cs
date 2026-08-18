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

    private IDamageable target;

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
    IDamageable newTarget,
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

        if (target.IsDead)
            return false;

        if (target.DamageTransform == null)
            return false;

        return true;
    }

    private void UpdateMovement()
    {
        Vector3 targetPosition =
            target.DamageTransform.position;

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
         * Zapamiêtujemy pozycjê przed zadaniem damage.
         * Cel mo¿e umrzeæ od tego trafienia.
         */
        Vector3 hitPosition =
            target.DamageTransform.position;

        IDamageable primaryTarget =
            target;

        // =====================================================
        // NORMALNY HIT
        // =====================================================

        primaryTarget.TakeWeaponDamage(
            hullDamage,
            shieldDamage);

        // Slow dzia³a tylko na statki.
        if (canSlowOnHit &&
            primaryTarget is ShipUnit shipTarget &&
            shipTarget.IsSpawned &&
            !shipTarget.isDead.Value)
        {
            shipTarget.ApplySlow(
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
    IDamageable primaryTarget,
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

        // =====================================================
        // SHIPS
        // =====================================================

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit shipTarget in allShips)
        {
            ApplyAoeToTarget(
                primaryTarget,
                shipTarget,
                hitPosition,
                rangeSquared,
                aoeHullDamage,
                aoeShieldDamage);
        }

        // =====================================================
        // BASES
        // =====================================================

        BaseUnit[] allBases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit baseTarget in allBases)
        {
            ApplyAoeToTarget(
                primaryTarget,
                baseTarget,
                hitPosition,
                rangeSquared,
                aoeHullDamage,
                aoeShieldDamage);
        }
    }

    private void ApplyAoeToTarget(
    IDamageable primaryTarget,
    IDamageable aoeTarget,
    Vector3 hitPosition,
    float rangeSquared,
    float aoeHullDamage,
    float aoeShieldDamage)
    {
        if (aoeTarget == null)
            return;

        // G³ówny cel dosta³ ju¿ normalny hit.
        if (ReferenceEquals(
                aoeTarget,
                primaryTarget))
        {
            return;
        }

        if (aoeTarget.IsDead)
            return;

        if (aoeTarget.DamageTransform == null)
            return;

        // Bez friendly fire.
        if (aoeTarget.OwnerId ==
            attackerOwnerId)
        {
            return;
        }

        Vector3 offset =
            aoeTarget.DamageTransform.position -
            hitPosition;

        if (offset.sqrMagnitude >
            rangeSquared)
        {
            return;
        }

        aoeTarget.TakeWeaponDamage(
            aoeHullDamage,
            aoeShieldDamage);

        /*
         * Slow jest w³aœciwoœci¹ statku,
         * wiêc baza dostaje damage,
         * ale nie dostaje Slow.
         */
        if (canSlowOnHit &&
            aoeTarget is ShipUnit shipTarget &&
            shipTarget.IsSpawned &&
            !shipTarget.isDead.Value)
        {
            shipTarget.ApplySlow(
                slowPercent,
                slowDuration);
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
    IDamageable primaryTarget)
    {
        if (!IsServer)
            return;

        if (primaryTarget == null)
            return;

        List<IDamageable> alreadyHit =
            new List<IDamageable>();

        alreadyHit.Add(
            primaryTarget);

        IDamageable currentTarget =
            primaryTarget;

        float currentHullDamage =
            hullDamage;

        float currentShieldDamage =
            shieldDamage;

        for (int jump = 0;
             jump < maxTargets;
             jump++)
        {
            IDamageable nextTarget =
                FindNearestChainTarget(
                    currentTarget,
                    alreadyHit);

            if (nextTarget == null)
                break;

            currentHullDamage *=
                chainDamageMultiplier;

            currentShieldDamage *=
                chainDamageMultiplier;

            Vector3 chainStart =
                currentTarget.DamageTransform.position;

            Vector3 chainEnd =
                nextTarget.DamageTransform.position;

            ShowChainVisualClientRpc(
                chainStart,
                chainEnd);

            nextTarget.TakeWeaponDamage(
                currentHullDamage,
                currentShieldDamage);

            // Slow tylko dla statków.
            if (canSlowOnHit &&
                nextTarget is ShipUnit shipTarget &&
                shipTarget.IsSpawned &&
                !shipTarget.isDead.Value)
            {
                shipTarget.ApplySlow(
                    slowPercent,
                    slowDuration);
            }

            /*
             * AOE nie uruchamia siê ponownie
             * przy kolejnych skokach Chain.
             */
            alreadyHit.Add(
                nextTarget);

            currentTarget =
                nextTarget;
        }
    }

    private IDamageable FindNearestChainTarget(
    IDamageable fromTarget,
    List<IDamageable> alreadyHit)
    {
        if (fromTarget == null)
            return null;

        if (fromTarget.DamageTransform == null)
            return null;

        IDamageable nearest =
            null;

        float nearestDistanceSquared =
            chainJumpsRange *
            chainJumpsRange;

        // =====================================================
        // SHIPS
        // =====================================================

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit candidate in allShips)
        {
            CheckChainCandidate(
                fromTarget,
                candidate,
                alreadyHit,
                ref nearest,
                ref nearestDistanceSquared);
        }

        // =====================================================
        // BASES
        // =====================================================

        BaseUnit[] allBases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit candidate in allBases)
        {
            CheckChainCandidate(
                fromTarget,
                candidate,
                alreadyHit,
                ref nearest,
                ref nearestDistanceSquared);
        }

        return nearest;
    }

    private void CheckChainCandidate(
    IDamageable fromTarget,
    IDamageable candidate,
    List<IDamageable> alreadyHit,
    ref IDamageable nearest,
    ref float nearestDistanceSquared)
    {
        if (candidate == null)
            return;

        if (candidate.IsDead)
            return;

        if (candidate.DamageTransform == null)
            return;

        // Bez friendly fire.
        if (candidate.OwnerId ==
            attackerOwnerId)
        {
            return;
        }

        // Nie wracamy do ju¿ trafionego celu.
        if (alreadyHit.Contains(candidate))
            return;

        Vector3 offset =
            candidate.DamageTransform.position -
            fromTarget.DamageTransform.position;

        float distanceSquared =
            offset.sqrMagnitude;

        if (distanceSquared >
            nearestDistanceSquared)
        {
            return;
        }

        nearestDistanceSquared =
            distanceSquared;

        nearest =
            candidate;
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