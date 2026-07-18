using Unity.Netcode;
using UnityEngine;

public class BaseSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject basePrefab;

    [SerializeField]
    private Vector3 hostBasePosition =
        new Vector3(-15f, 0f, 0f);

    [SerializeField]
    private Vector3 clientBasePosition =
        new Vector3(15f, 0f, 0f);

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        SpawnBaseForClient(
            NetworkManager.ServerClientId,
            hostBasePosition);

        foreach (ulong clientId in
                 NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId ==
                NetworkManager.ServerClientId)
            {
                continue;
            }

            SpawnBaseForClient(
                clientId,
                clientBasePosition);
        }

        NetworkManager.Singleton
            .OnClientConnectedCallback +=
            OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton
                .OnClientConnectedCallback -=
                OnClientConnected;
        }
    }

    private void OnClientConnected(
        ulong clientId)
    {
        if (!IsServer)
            return;

        if (clientId ==
            NetworkManager.ServerClientId)
        {
            return;
        }

        if (FindBaseForOwner(clientId) != null)
        {
            Debug.LogWarning(
                $"[BASE SPAWNER] Baza gracza {clientId} ju¿ istnieje.");

            return;
        }

        SpawnBaseForClient(
            clientId,
            clientBasePosition);
    }

    private void SpawnBaseForClient(
        ulong clientId,
        Vector3 position)
    {
        if (!IsServer)
            return;

        if (basePrefab == null)
        {
            Debug.LogError(
                "[BASE SPAWNER] basePrefab == null.");

            return;
        }

        GameObject baseObj =
            Instantiate(
                basePrefab,
                position,
                Quaternion.identity);

        NetworkObject netObj =
            baseObj.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError(
                "[BASE SPAWNER] Base prefab nie ma NetworkObject.");

            Destroy(baseObj);
            return;
        }

        // Najwa¿niejsza zmiana:
        netObj.SpawnWithOwnership(clientId);

        UnitOwner owner =
            baseObj.GetComponent<UnitOwner>();

        if (owner != null)
        {
            owner.SetOwner(clientId);
        }
        else
        {
            Debug.LogWarning(
                "[BASE SPAWNER] Brak UnitOwner na basePrefab.");
        }

        PlayerBase playerBase =
            FindPlayerBase(clientId);

        if (playerBase != null)
        {
            playerBase.SetBase(netObj);
        }
        else
        {
            Debug.LogWarning(
                $"[BASE SPAWNER] Nie znaleziono PlayerBase dla clientId={clientId}.");
        }

        BaseHangar hangar =
            baseObj.GetComponentInChildren<BaseHangar>();

        Debug.Log(
            $"[BASE SPAWNER] Zespawnowano bazê. " +
            $"clientId={clientId}, " +
            $"networkOwner={netObj.OwnerClientId}, " +
            $"hangarOwner={(hangar != null ? hangar.OwnerClientId : 999)}");
    }

    private PlayerBase FindPlayerBase(
        ulong clientId)
    {
        PlayerBase[] players =
            FindObjectsByType<PlayerBase>(
                FindObjectsSortMode.None);

        foreach (PlayerBase player in players)
        {
            if (!player.IsSpawned)
                continue;

            if (player.OwnerClientId == clientId)
                return player;
        }

        return null;
    }

    private BaseHangar FindBaseForOwner(
        ulong clientId)
    {
        BaseHangar[] hangars =
            FindObjectsByType<BaseHangar>(
                FindObjectsSortMode.None);

        foreach (BaseHangar hangar in hangars)
        {
            if (!hangar.IsSpawned)
                continue;

            if (hangar.OwnerClientId == clientId)
                return hangar;
        }

        return null;
    }
}