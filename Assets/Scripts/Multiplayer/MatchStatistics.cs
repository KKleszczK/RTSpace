using System.Collections.Generic;
using UnityEngine;

public class MatchStatistics : MonoBehaviour
{
    public static MatchStatistics Instance { get; private set; }

    [System.Serializable]
    public class PlayerStats
    {
        public ulong clientId;

        public int shipsKilled;
        public int modulesCrafted;
        public int researches;
        public int damageDealt;

        public int totalMetal;
        public int totalEnergy;

        // Income w kolejnych interwa³ach.
        // Pierwsza wartoœæ zawsze = 0.
        public List<int> metalIncomeHistory = new();
        public List<int> energyIncomeHistory = new();

        [System.NonSerialized]
        public int lastTotalMetal;

        [System.NonSerialized]
        public int lastTotalEnergy;
    }

    [SerializeField] private float snapshotInterval = 30f;

    private readonly Dictionary<ulong, PlayerStats> playerStats = new();

    private readonly Dictionary<ulong, PlayerResources> trackedResources = new();

    private float nextSnapshotTime;


    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        nextSnapshotTime = snapshotInterval;
    }


    private void Update()
    {
        TrackPlayerResources();

        if (MatchManager.Instance != null &&
            MatchManager.Instance.IsMatchFinished())
            return;

        if (Time.time >= nextSnapshotTime)
        {
            CaptureResourceIncome();

            nextSnapshotTime += snapshotInterval;
        }
    }


    // =========================================================
    // PLAYER STATS
    // =========================================================

    private PlayerStats GetPlayerStats(ulong clientId)
    {
        if (!playerStats.TryGetValue(
                clientId,
                out PlayerStats stats))
        {
            stats = new PlayerStats
            {
                clientId = clientId
            };

            playerStats.Add(
                clientId,
                stats);
        }

        return stats;
    }


    public PlayerStats GetStats(ulong clientId)
    {
        playerStats.TryGetValue(
            clientId,
            out PlayerStats stats);

        return stats;
    }


    public IReadOnlyDictionary<ulong, PlayerStats> GetAllStats()
    {
        return playerStats;
    }


    // =========================================================
    // RESOURCES
    // =========================================================

    private void TrackPlayerResources()
    {
        if (trackedResources.Count >= 2)
            return;

        PlayerResources[] resources =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        foreach (PlayerResources resource in resources)
        {
            if (!resource.IsSpawned)
                continue;

            ulong clientId =
                resource.OwnerClientId;

            if (trackedResources.ContainsKey(clientId))
                continue;

            trackedResources.Add(
                clientId,
                resource);

            PlayerStats stats =
                GetPlayerStats(clientId);

            /*
             * Total zawiera zasoby startowe.
             */
            stats.totalMetal =
                resource.metal.Value;

            stats.totalEnergy =
                resource.energy.Value;

            /*
             * Income zaczyna siê od 0.
             * Zasoby startowe NIE s¹ income.
             */
            stats.lastTotalMetal =
                stats.totalMetal;

            stats.lastTotalEnergy =
                stats.totalEnergy;

            stats.metalIncomeHistory.Add(0);
            stats.energyIncomeHistory.Add(0);

            /*
             * Nas³uchujemy tylko zmian zasobów.
             */
            resource.metal.OnValueChanged +=
                (oldValue, newValue) =>
                {
                    int gained =
                        newValue - oldValue;

                    if (gained > 0)
                        stats.totalMetal += gained;
                };

            resource.energy.OnValueChanged +=
                (oldValue, newValue) =>
                {
                    int gained =
                        newValue - oldValue;

                    if (gained > 0)
                        stats.totalEnergy += gained;
                };

            Debug.Log(
                $"[STATS] Tracking Player={clientId} | " +
                $"Start Metal={stats.totalMetal} | " +
                $"Start Energy={stats.totalEnergy}");
        }
    }


    private void CaptureResourceIncome()
    {
        foreach (PlayerStats stats in playerStats.Values)
        {
            int metalIncome =
                stats.totalMetal -
                stats.lastTotalMetal;

            int energyIncome =
                stats.totalEnergy -
                stats.lastTotalEnergy;

            metalIncome =
                Mathf.Max(0, metalIncome);

            energyIncome =
                Mathf.Max(0, energyIncome);

            stats.metalIncomeHistory.Add(
                metalIncome);

            stats.energyIncomeHistory.Add(
                energyIncome);

            stats.lastTotalMetal =
                stats.totalMetal;

            stats.lastTotalEnergy =
                stats.totalEnergy;

            Debug.Log(
                $"[STATS INCOME] Player={stats.clientId} | " +
                $"Metal +{metalIncome} | " +
                $"Energy +{energyIncome}");
        }
    }


    // =========================================================
    // SHIP KILLS
    // =========================================================

    public void RegisterShipDeath(ulong deadPlayerId)
    {
        foreach (PlayerStats stats in playerStats.Values)
        {
            if (stats.clientId == deadPlayerId)
                continue;

            stats.shipsKilled++;

            Debug.Log(
                $"[STATS] Player={stats.clientId} " +
                $"Ships Killed={stats.shipsKilled}");

            return;
        }
    }


    // =========================================================
    // MODULES
    // =========================================================

    public void RegisterModuleCrafted(ulong playerId)
    {
        PlayerStats stats =
            GetPlayerStats(playerId);

        stats.modulesCrafted++;

        Debug.Log(
            $"[STATS] Player={playerId} " +
            $"Modules Crafted={stats.modulesCrafted}");
    }


    // =========================================================
    // RESEARCH
    // =========================================================

    public void RegisterResearchCompleted(ulong playerId)
    {
        PlayerStats stats =
            GetPlayerStats(playerId);

        stats.researches++;

        Debug.Log(
            $"[STATS] Player={playerId} " +
            $"Researches={stats.researches}");
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void RegisterDamage(
        ulong damagedPlayerId,
        int damage)
    {
        if (damage <= 0)
            return;

        foreach (PlayerStats stats in playerStats.Values)
        {
            if (stats.clientId == damagedPlayerId)
                continue;

            stats.damageDealt += damage;

            Debug.Log(
                $"[STATS] Player={stats.clientId} " +
                $"Damage Dealt={stats.damageDealt}");

            return;
        }
    }
}