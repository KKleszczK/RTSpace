using Unity.Netcode;
using UnityEngine;

public class PlayerUpgradeStats : NetworkBehaviour
{
    // =========================================================
    // SHIP HP
    // =========================================================

    public NetworkVariable<float> shipHpFlat = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> shipHpPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // SHIP SHIELD
    // =========================================================

    public NetworkVariable<float> shipShieldFlat = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> shipShieldPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // SHIP SPEED
    // =========================================================

    public NetworkVariable<float> shipSpeedFlat = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> shipSpeedPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // STATION RESEARCH SPEED
    // =========================================================

    public NetworkVariable<float> stationResearchSpeedPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // STATION HP
    // =========================================================

    public NetworkVariable<float> stationHpFlat = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // STATION SHIELD
    // =========================================================

    public NetworkVariable<float> stationShieldFlat = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // GENERATOR BOOSTS
    // =========================================================

    public NetworkVariable<float> generator1BoostPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> generator2BoostPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> generator3BoostPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // APPLY RESEARCH
    // =========================================================

    public void ApplyResearch(
        ResearchDefinition research)
    {
        if (!IsServer)
            return;

        if (research == null)
            return;

        switch (research.effectType)
        {
            // =================================================
            // SHIP HP
            // =================================================

            case ResearchEffectType.ShipHp:

                if (research.valueType ==
                    ResearchValueType.Flat)
                {
                    shipHpFlat.Value +=
                        research.value;
                }
                else
                {
                    shipHpPercent.Value +=
                        research.value;
                }

                RefreshOwnedShips();
                break;

            // =================================================
            // SHIP SHIELD
            // =================================================

            case ResearchEffectType.ShipShield:

                if (research.valueType ==
                    ResearchValueType.Flat)
                {
                    shipShieldFlat.Value +=
                        research.value;
                }
                else
                {
                    shipShieldPercent.Value +=
                        research.value;
                }

                RefreshOwnedShips();
                break;

            // =================================================
            // SHIP SPEED
            // =================================================

            case ResearchEffectType.ShipSpeed:

                if (research.valueType ==
                    ResearchValueType.Flat)
                {
                    shipSpeedFlat.Value +=
                        research.value;
                }
                else
                {
                    shipSpeedPercent.Value +=
                        research.value;
                }

                RefreshOwnedShips();
                break;

            // =================================================
            // STATION RESEARCH SPEED
            // =================================================

            case ResearchEffectType.StationResearchSpeed:

                stationResearchSpeedPercent.Value +=
                    research.value;

                break;

            // =================================================
            // STATION HULL HP
            // =================================================

            case ResearchEffectType.StationHullHp:

                stationHpFlat.Value +=
                    research.value;

                RefreshOwnedBaseStats();
                break;

            // =================================================
            // STATION SHIELD
            // =================================================

            case ResearchEffectType.StationShield:

                stationShieldFlat.Value +=
                    research.value;

                RefreshOwnedBaseStats();
                break;

            // =================================================
            // GENERATOR BOOST
            // =================================================

            case ResearchEffectType.GeneratorBoost:

                switch (research.tier)
                {
                    case ResearchTier.Tier1:

                        generator1BoostPercent.Value +=
                            research.value;

                        break;

                    case ResearchTier.Tier2:

                        generator2BoostPercent.Value +=
                            research.value;

                        break;

                    case ResearchTier.Tier3:

                        generator3BoostPercent.Value +=
                            research.value;

                        break;
                }

                break;

            // =================================================
            // MODULE UNLOCK
            // =================================================

            case ResearchEffectType.UnlockModules:

                /*
                 * Unlock modu³ów jest obs³ugiwany
                 * przez PlayerResearch.
                 */
                break;
        }

        Debug.Log(
            $"[RESEARCH STATS] " +
            $"ShipHP={shipHpFlat.Value} flat / {shipHpPercent.Value}% | " +
            $"ShipShield={shipShieldFlat.Value} flat / {shipShieldPercent.Value}% | " +
            $"ShipSpeed={shipSpeedFlat.Value} flat / {shipSpeedPercent.Value}% | " +
            $"ResearchSpeed={stationResearchSpeedPercent.Value}% | " +
            $"StationHP={stationHpFlat.Value} | " +
            $"StationShield={stationShieldFlat.Value} | " +
            $"Generator1={generator1BoostPercent.Value}% | " +
            $"Generator2={generator2BoostPercent.Value}% | " +
            $"Generator3={generator3BoostPercent.Value}%");
    }

    // =========================================================
    // REFRESH DEPLOYED SHIPS
    // =========================================================

    private void RefreshOwnedShips()
    {
        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit ship in allShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            if (ship.ownerId.Value !=
                OwnerClientId)
            {
                continue;
            }

            ship.RecalculateResearchStats();
        }
    }

    // =========================================================
    // REFRESH BASE
    // =========================================================

    private void RefreshOwnedBaseStats()
    {
        if (!IsServer)
            return;

        BaseUnit[] bases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit playerBase in bases)
        {
            if (playerBase == null)
                continue;

            if (!playerBase.IsSpawned)
                continue;

            if (playerBase.IsDead)
                continue;

            if (playerBase.OwnerId !=
                OwnerClientId)
            {
                continue;
            }

            playerBase.RecalculateResearchStats();
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public float GetResearchSpeedMultiplier()
    {
        return Mathf.Max(
            0.01f,
            1f +
            stationResearchSpeedPercent.Value / 100f);
    }

    public float GetGeneratorBoostMultiplier(
    int generatorIndex)
    {
        float boostPercent = 0f;

        switch (generatorIndex)
        {
            case 1:
                boostPercent =
                    generator1BoostPercent.Value;
                break;

            case 2:
                boostPercent =
                    generator2BoostPercent.Value;
                break;

            case 3:
                boostPercent =
                    generator3BoostPercent.Value;
                break;

            default:
                return 1f;
        }

        return Mathf.Max(
            0.01f,
            1f +
            boostPercent / 100f);
    }
}