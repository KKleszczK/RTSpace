using System.Collections.Generic;
using UnityEngine;

public class MatchStatistics : MonoBehaviour
{
    public static MatchStatistics Instance { get; private set; }

    [System.Serializable]
    public class ResourceSnapshot
    {
        public float time;
        public int metal;
        public int energy;
    }

    [System.Serializable]
    public class PlayerStats
    {
        public ulong clientId;

        public int shipsKilled;

        public int totalMetal;
        public int totalEnergy;

        public int modulesCrafted;
        public int researches;

        public int damageDealt;

        public List<ResourceSnapshot> resourceHistory = new();
    }

    [SerializeField] private float snapshotInterval = 30f;

    private readonly Dictionary<ulong, PlayerStats> playerStats = new();

    private readonly Dictionary<ulong, PlayerResources> trackedResources = new();

    public int shipsKilled;





    private float matchTime;
    private float nextSnapshotTime;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Pierwszy odczyt od razu po rozpoczêciu meczu.
        CaptureResourceSnapshot();

        nextSnapshotTime = snapshotInterval;
    }

    private void Update()
    {
        TrackPlayerResources();

        if (MatchManager.Instance != null &&
            MatchManager.Instance.IsMatchFinished())
            return;

        matchTime += Time.deltaTime;

        if (matchTime >= nextSnapshotTime)
        {
            CaptureResourceSnapshot();
            nextSnapshotTime += snapshotInterval;
        }
    }

    private PlayerStats GetPlayerStats(ulong clientId)
    {
        if (!playerStats.TryGetValue(clientId, out PlayerStats stats))
        {
            stats = new PlayerStats
            {
                clientId = clientId
            };

            playerStats.Add(clientId, stats);
        }

        return stats;
    }

    private void CaptureResourceSnapshot()
    {
        PlayerResources[] resources =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        foreach (PlayerResources resource in resources)
        {
            if (!resource.IsSpawned)
                continue;

            ulong clientId = resource.OwnerClientId;

            PlayerStats stats =
                GetPlayerStats(clientId);

            stats.resourceHistory.Add(
                new ResourceSnapshot
                {
                    time = matchTime,
                    metal = resource.metal.Value,
                    energy = resource.energy.Value
                });

            Debug.Log(
                $"[STATS LOCAL] Player={clientId} " +
                $"Time={matchTime:F0}s " +
                $"Metal={resource.metal.Value} " +
                $"Energy={resource.energy.Value}");
        }
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

            ulong clientId = resource.OwnerClientId;

            if (trackedResources.ContainsKey(clientId))
                continue;

            trackedResources.Add(clientId, resource);

            PlayerStats stats = GetPlayerStats(clientId);

            // Startowe zasoby równie¿ liczymy do Total.
            stats.totalMetal = resource.metal.Value;
            stats.totalEnergy = resource.energy.Value;

            resource.metal.OnValueChanged +=
                (oldValue, newValue) =>
                {
                    int gained = newValue - oldValue;

                    if (gained > 0)
                        stats.totalMetal += gained;
                };

            resource.energy.OnValueChanged +=
                (oldValue, newValue) =>
                {
                    int gained = newValue - oldValue;

                    if (gained > 0)
                        stats.totalEnergy += gained;
                };

            Debug.Log(
                $"[STATS] Tracking Player={clientId} " +
                $"Start Metal={stats.totalMetal} " +
                $"Energy={stats.totalEnergy}");
        }
    }

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

    public void RegisterModuleCrafted(ulong playerId)
    {
        PlayerStats stats = GetPlayerStats(playerId);
        stats.modulesCrafted++;

        Debug.Log(
            $"[STATS] Player={playerId} " +
            $"Modules Crafted={stats.modulesCrafted}");
    }

    public void RegisterResearchCompleted(ulong playerId)
    {
        PlayerStats stats = GetPlayerStats(playerId);
        stats.researches++;

        Debug.Log(
            $"[STATS] Player={playerId} " +
            $"Researches={stats.researches}");
    }

    public void RegisterDamage(ulong damagedPlayerId, int damage)
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