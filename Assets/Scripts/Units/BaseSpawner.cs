using Unity.Netcode;
using UnityEngine;

public class BaseSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject basePrefab;

    [SerializeField] private Vector3 hostBasePosition = new Vector3(-15f, 0f, 0f);
    [SerializeField] private Vector3 clientBasePosition = new Vector3(15f, 0f, 0f);

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        SpawnBaseForClient(0, hostBasePosition);
        SpawnBaseForClient(1, clientBasePosition);
    }

    private void SpawnBaseForClient(ulong clientId, Vector3 position)
    {
        GameObject baseObj = Instantiate(basePrefab, position, Quaternion.identity);

        NetworkObject netObj = baseObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        UnitOwner owner = baseObj.GetComponent<UnitOwner>();
        owner.SetOwner(clientId);

        PlayerBase playerBase = FindPlayerBase(clientId);

        if (playerBase != null)
            playerBase.SetBase(netObj);
    }

    private PlayerBase FindPlayerBase(ulong clientId)
    {
        PlayerBase[] players = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);

        foreach (PlayerBase p in players)
        {
            if (p.OwnerClientId == clientId)
                return p;
        }

        return null;
    }
}