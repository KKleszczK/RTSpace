using Unity.Netcode;
using UnityEngine;

public class DebugShipSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject shipPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.5f, 4f);

    public void SpawnMyShip()
    {
        SpawnShipServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnShipServerRpc(ulong ownerClientId)
    {
        Vector3 pos = transform.position + spawnOffset;

        GameObject shipObj = Instantiate(shipPrefab, pos, Quaternion.identity);

        NetworkObject netObj = shipObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        UnitOwner owner = shipObj.GetComponent<UnitOwner>();
        owner.SetOwner(ownerClientId);

        ShipStats stats = shipObj.GetComponent<ShipStats>();
        if (stats != null)
            stats.RecalculateStats();

        NetworkHealth health = shipObj.GetComponent<NetworkHealth>();
        if (health != null)
            health.RecalculateHealthFromStats(true);
    }
}