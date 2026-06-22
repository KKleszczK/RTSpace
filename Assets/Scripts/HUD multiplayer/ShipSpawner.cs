using Unity.Netcode;
using UnityEngine;

public class ShipSpawner : NetworkBehaviour
{
    public GameObject shipPrefab;
    [SerializeField] private float shipFlightHeight = 0.5f;

    public void SpawnMyShip()
    {
        SpawnShipServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnShipServerRpc(ulong ownerClientId)
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(-5f, 5f),
            shipFlightHeight,
            Random.Range(-5f, 5f)
        );

        GameObject shipObj = Instantiate(shipPrefab, spawnPos, Quaternion.identity);

        NetworkObject netObj = shipObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        ShipUnit ship = shipObj.GetComponent<ShipUnit>();
        ship.ownerId.Value = ownerClientId;
    }
}