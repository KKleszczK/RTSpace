using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ShipAuraManager : NetworkBehaviour
{
    [SerializeField]
    private ShipUnit ship;

    [Header("Update")]
    [SerializeField]
    private float updateInterval = 0.25f;

    private float nextUpdateTime;

    /*
     * Leczenie mo¿e mieæ wartoœci u³amkowe.
     * Np. 2 HP/s przy updateInterval 0.25
     * daje 0.5 HP na tick.
     *
     * Zbieramy u³amki, a¿ uzbiera siê
     * przynajmniej 1 pe³ny punkt HP.
     */
    private float pendingHealing;

    private void Awake()
    {
        if (ship == null)
        {
            ship =
                GetComponent<ShipUnit>();
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (ship == null)
            return;

        if (ship.isDead.Value)
            return;

        if (Time.time < nextUpdateTime)
            return;

        nextUpdateTime =
            Time.time + updateInterval;

        UpdateIncomingAuras();
    }

    // =========================================================
    // UPDATE ALL AURAS AFFECTING THIS SHIP
    // =========================================================

    private void UpdateIncomingAuras()
    {
        float totalHealingPerSecond = 0f;
        float strongestRangeBoost = 0f;

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit sourceShip in allShips)
        {
            if (!IsValidAuraSource(sourceShip))
                continue;

            CheckAuraModule(
                sourceShip,
                0,
                sourceShip.normalModule1.Value,
                ref totalHealingPerSecond,
                ref strongestRangeBoost);

            CheckAuraModule(
                sourceShip,
                1,
                sourceShip.normalModule2.Value,
                ref totalHealingPerSecond,
                ref strongestRangeBoost);

            CheckAuraModule(
                sourceShip,
                2,
                sourceShip.normalModule3.Value,
                ref totalHealingPerSecond,
                ref strongestRangeBoost);

            CheckAuraModule(
                sourceShip,
                3,
                sourceShip.classModule.Value,
                ref totalHealingPerSecond,
                ref strongestRangeBoost);
        }

        // =====================================================
        // RANGE BOOST
        // =====================================================

        /*
         * Zawsze ustawiamy aktualn¹ wartoœæ.
         *
         * Jeœli statek wyjdzie ze wszystkich aur,
         * strongestRangeBoost bêdzie 0
         * i bonus automatycznie zniknie.
         */
        ship.SetAuraRangeBoost(
            strongestRangeBoost);

        // =====================================================
        // HEALING
        // =====================================================

        if (totalHealingPerSecond > 0f)
        {
            pendingHealing +=
                totalHealingPerSecond *
                updateInterval;

            int wholeHealing =
                Mathf.FloorToInt(
                    pendingHealing);

            if (wholeHealing > 0)
            {
                ship.Heal(
                    wholeHealing);

                pendingHealing -=
                    wholeHealing;
            }
        }
        else
        {
            pendingHealing = 0f;
        }
    }

    // =========================================================
    // CHECK ONE MODULE
    // =========================================================

    private void CheckAuraModule(
        ShipUnit sourceShip,
        int slotIndex,
        FixedString64Bytes moduleId,
        ref float totalHealingPerSecond,
        ref float strongestRangeBoost)
    {
        if (sourceShip == null)
            return;

        if (moduleId.IsEmpty)
            return;

        if (ModuleDatabase.Instance == null)
            return;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        if (module == null)
            return;

        if (!module.haveAura)
            return;

        float auraRange =
            Mathf.Max(
                0f,
                module.auraRange);

        if (auraRange <= 0f)
            return;

        Vector3 offset =
            ship.transform.position -
            sourceShip.transform.position;

        offset.y = 0f;

        float rangeSquared =
            auraRange * auraRange;

        if (offset.sqrMagnitude >
            rangeSquared)
        {
            return;
        }

        /*
         * Mno¿nik Class Slota dotyczy
         * TYLKO auraEffectValue.
         *
         * auraRange pozostaje bez zmian.
         */
        float classMultiplier =
            sourceShip.GetModuleEffectMultiplier(
                slotIndex,
                module);

        float finalAuraValue =
            module.auraEffectValue *
            classMultiplier;

        switch (module.auraEffect)
        {
            // =================================================
            // HEALING
            // =================================================

            case AuraEffectType.Healing:

                /*
                 * Healing ró¿nych aur sumuje siê.
                 *
                 * 2 HP/s + 3 HP/s = 5 HP/s.
                 */
                totalHealingPerSecond +=
                    Mathf.Max(
                        0f,
                        finalAuraValue);

                break;

            // =================================================
            // RANGE BOOST
            // =================================================

            case AuraEffectType.RangeBoost:

                /*
                 * RangeBoost NIE sumuje siê.
                 * Zawsze dzia³a najmocniejsza aura.
                 */
                strongestRangeBoost =
                    Mathf.Max(
                        strongestRangeBoost,
                        finalAuraValue);

                break;
        }
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool IsValidAuraSource(
        ShipUnit sourceShip)
    {
        if (sourceShip == null)
            return false;

        if (!sourceShip.IsSpawned)
            return false;

        if (sourceShip.isDead.Value)
            return false;

        // Aura dzia³a tylko na statki tego samego gracza.
        if (sourceShip.ownerId.Value !=
            ship.ownerId.Value)
        {
            return false;
        }

        return true;
    }
}