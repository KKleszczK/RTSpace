using Unity.Netcode;

public class PlayerBase : NetworkBehaviour
{
    public NetworkVariable<ulong> baseNetworkObjectId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void SetBase(NetworkObject baseObject)
    {
        if (!IsServer) return;
        baseNetworkObjectId.Value = baseObject.NetworkObjectId;
    }

    public NetworkHealth GetBaseHealth()
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(baseNetworkObjectId.Value, out NetworkObject baseObj))
        {
            return baseObj.GetComponent<NetworkHealth>();
        }

        return null;
    }
}