using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipUnit))]
public class ShipAbilityManager : NetworkBehaviour
{
    private ShipUnit ship;

    /*
     * slotIndex -> czas, kiedy ability
     * z tego slotu mo¿e zostaæ ponownie u¿yte.
     */
    private readonly Dictionary<int, float>
        nextAbilityUseTime = new();

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        ship =
            GetComponent<ShipUnit>();
    }

    // =========================================================
    // DEBUG INPUT
    // =========================================================

    private void Update()
    {
        /*
         * Input czytamy po stronie klienta,
         * nie serwera.
         */
        if (!IsClient)
            return;

        if (ship == null)
            return;

        if (!ship.IsMine())
            return;

        if (ship.isDead.Value)
            return;

        if (Keyboard.current == null)
            return;

        /*
         * Tymczasowa aktywacja ability:
         * P = aktywuj wszystkie dostêpne ability.
         */
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            Debug.Log(
                $"[ABILITY DEBUG] P pressed on {ship.name}.",
                ship);

            RequestUseAllAbilities();
        }
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /*
     * PóŸniej przycisk UI mo¿e po prostu
     * wywo³aæ tê metodê.
     */
    public void RequestUseAllAbilities()
    {
        if (ship == null)
            return;

        if (!ship.IsMine())
            return;

        RequestUseAllAbilitiesServerRpc();
    }

    // =========================================================
    // SERVER REQUEST
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestUseAllAbilitiesServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (ship == null)
            return;

        if (ship.isDead.Value)
            return;

        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        /*
         * Klient mo¿e aktywowaæ ability
         * tylko swojego statku.
         */
        if (senderClientId !=
            ship.ownerId.Value)
        {
            Debug.LogWarning(
                $"[ABILITY BLOCKED] Client {senderClientId} " +
                $"próbowa³ u¿yæ ability statku owner={ship.ownerId.Value}.",
                ship);

            return;
        }

        int activatedAbilities = 0;

        activatedAbilities +=
            TryUseAbilityFromSlot(
                0,
                ship.normalModule1.Value);

        activatedAbilities +=
            TryUseAbilityFromSlot(
                1,
                ship.normalModule2.Value);

        activatedAbilities +=
            TryUseAbilityFromSlot(
                2,
                ship.normalModule3.Value);

        activatedAbilities +=
            TryUseAbilityFromSlot(
                3,
                ship.classModule.Value);

        Debug.Log(
            $"[ABILITY] {ship.name}: " +
            $"aktywowane ability={activatedAbilities}.",
            ship);
    }

    // =========================================================
    // SLOT
    // =========================================================

    private int TryUseAbilityFromSlot(
        int slotIndex,
        FixedString64Bytes moduleId)
    {
        if (moduleId.IsEmpty)
            return 0;

        if (ModuleDatabase.Instance == null)
            return 0;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        if (module == null)
            return 0;

        if (!module.haveAbility)
            return 0;

        if (module.abilityType ==
            ModuleAbilityType.None)
        {
            return 0;
        }

        // =====================================================
        // COOLDOWN
        // =====================================================

        if (nextAbilityUseTime.TryGetValue(
                slotIndex,
                out float readyTime))
        {
            if (Time.time < readyTime)
            {
                float remaining =
                    readyTime - Time.time;

                Debug.Log(
                    $"[ABILITY] {module.displayName} " +
                    $"cooldown: {remaining:0.0}s.",
                    ship);

                return 0;
            }
        }

        // =====================================================
        // EXECUTE
        // =====================================================

        bool abilityUsed =
            ExecuteAbility(
                module,
                slotIndex);

        if (!abilityUsed)
            return 0;

        float cooldown =
            Mathf.Max(
                0f,
                module.abilityCooldown);

        nextAbilityUseTime[slotIndex] =
            Time.time + cooldown;

        Debug.Log(
            $"[ABILITY] {ship.name} u¿y³ " +
            $"{module.abilityType} | " +
            $"slot={slotIndex} | " +
            $"cooldown={cooldown:0.##}s.",
            ship);

        return 1;
    }

    // =========================================================
    // EXECUTE ABILITY
    // =========================================================

    private bool ExecuteAbility(
        ModuleDefinition module,
        int slotIndex)
    {
        switch (module.abilityType)
        {
            case ModuleAbilityType.ShieldDisruptor:

                return ExecuteShieldDisruptor(
                    module);

            default:

                Debug.LogWarning(
                    $"[ABILITY] Brak implementacji dla " +
                    $"{module.abilityType}.",
                    ship);

                return false;
        }
    }

    // =========================================================
    // SHIELD DISRUPTOR
    // =========================================================

    private bool ExecuteShieldDisruptor(
        ModuleDefinition module)
    {
        if (!IsServer)
            return false;

        float range =
            Mathf.Max(
                0f,
                module.abilityRange);

        if (range <= 0f)
            return false;

        float rangeSquared =
            range * range;

        int affectedTargets = 0;

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit target in allShips)
        {
            if (!IsValidShieldDisruptorTarget(
                    target,
                    rangeSquared))
            {
                continue;
            }

            /*
             * Statek mo¿e mieæ MaxShield > 0,
             * ale aktualnie Shield = 0.
             * Wtedy nie ma czego niszczyæ.
             */
            if (target.shield.Value <= 0)
                continue;

            int removedShield =
                target.shield.Value;

            target.shield.Value = 0;

            affectedTargets++;

            Debug.Log(
                $"[SHIELD DISRUPTOR] " +
                $"{target.name}: usuniêto " +
                $"{removedShield} shield.",
                target);
        }

        Debug.Log(
            $"[SHIELD DISRUPTOR] " +
            $"{ship.name} | Range={range:0.##} | " +
            $"Targets={affectedTargets}.",
            ship);

        /*
         * Ability uznajemy za u¿yte nawet wtedy,
         * gdy nie by³o przeciwnika z aktywn¹ tarcz¹.
         *
         * Gracz nacisn¹³ ability -> cooldown zaczyna dzia³aæ.
         */
        return true;
    }

    private bool IsValidShieldDisruptorTarget(
        ShipUnit target,
        float rangeSquared)
    {
        if (target == null)
            return false;

        if (target == ship)
            return false;

        if (!target.IsSpawned)
            return false;

        if (target.isDead.Value)
            return false;

        // Tylko przeciwnicy.
        if (target.ownerId.Value ==
            ship.ownerId.Value)
        {
            return false;
        }

        Vector3 offset =
            target.transform.position -
            ship.transform.position;

        // RTS -> dystans po p³aszczyŸnie XZ.
        offset.y = 0f;

        return offset.sqrMagnitude <=
               rangeSquared;
    }

    // =========================================================
    // OPTIONAL HELPERS FOR FUTURE UI
    // =========================================================

    public bool HasAnyAbility()
    {
        return
            SlotHasAbility(
                ship.normalModule1.Value) ||
            SlotHasAbility(
                ship.normalModule2.Value) ||
            SlotHasAbility(
                ship.normalModule3.Value) ||
            SlotHasAbility(
                ship.classModule.Value);
    }

    private bool SlotHasAbility(
        FixedString64Bytes moduleId)
    {
        if (moduleId.IsEmpty)
            return false;

        if (ModuleDatabase.Instance == null)
            return false;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        return module != null &&
               module.haveAbility &&
               module.abilityType !=
               ModuleAbilityType.None;
    }
}