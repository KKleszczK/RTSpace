using Unity.Netcode;
using UnityEngine;

public class UnitMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private UnitOwner owner;

    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        owner = GetComponent<UnitOwner>();

        if (IsServer)
            targetPosition.Value = transform.position;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition.Value,
            moveSpeed * Time.deltaTime
        );
    }

    [ServerRpc(RequireOwnership = false)]
    public void MoveToServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (owner == null)
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId != owner.ownerId.Value)
            return;

        targetPosition.Value = pos;
    }
}