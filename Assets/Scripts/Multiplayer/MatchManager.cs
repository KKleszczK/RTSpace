using Unity.Netcode;
using UnityEngine;

public enum MatchEndReason
{
    BaseDestroyed,
    Surrender,
    Disconnect
}

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public NetworkVariable<bool> matchFinished = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> winnerClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> loserClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        Instance = this;
    }

    public void EndMatchServer(
        ulong loserId,
        MatchEndReason reason)
    {
        if (!IsServer)
            return;

        if (matchFinished.Value)
            return;

        ulong winnerId = FindOpponentClientId(loserId);

        if (winnerId == ulong.MaxValue)
        {
            Debug.LogError(
                $"[MATCH] Cannot find opponent for player {loserId}");

            return;
        }

        matchFinished.Value = true;
        winnerClientId.Value = winnerId;
        loserClientId.Value = loserId;

        Debug.Log(
            $"[MATCH END] Winner={winnerId} " +
            $"Loser={loserId} Reason={reason}");

        ShowMatchResultClientRpc(
            winnerId,
            loserId,
            reason);
    }

    private ulong FindOpponentClientId(ulong playerId)
    {
        foreach (ulong clientId in
                 NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != playerId)
                return clientId;
        }

        return ulong.MaxValue;
    }

    [ClientRpc]
    private void ShowMatchResultClientRpc(
        ulong winnerId,
        ulong loserId,
        MatchEndReason reason)
    {
        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        bool victory =
            localClientId == winnerId;

        Debug.Log(
            $"[MATCH RESULT] " +
            $"{(victory ? "VICTORY" : "DEFEAT")} " +
            $"Reason={reason}");

        MatchEndPanelUI.Instance?.Show(
            victory,
            reason);
    }

    public void Surrender()
    {
        if (!IsSpawned)
            return;

        SurrenderServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SurrenderServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (matchFinished.Value)
            return;

        ulong surrenderingPlayerId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"[MATCH] Player {surrenderingPlayerId} surrendered.");

        EndMatchServer(
            surrenderingPlayerId,
            MatchEndReason.Surrender);
    }

    public void ShowHostDisconnectedVictory()
    {
        if (IsServer)
            return;

        Debug.Log(
            "[MATCH] Host disconnected. Local player wins.");

        MatchEndPanelUI.Instance?.Show(
            true,
            MatchEndReason.Disconnect);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }

        Debug.Log(
            $"[MATCH] Disconnect callback registered. " +
            $"Local={NetworkManager.LocalClientId} " +
            $"IsHost={IsHost} IsServer={IsServer} IsClient={IsClient}");
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log(
            $"[MATCH DISCONNECT] " +
            $"DisconnectedId={clientId} | " +
            $"Local={NetworkManager.LocalClientId} | " +
            $"ServerId={NetworkManager.ServerClientId} | " +
            $"IsHost={IsHost} | " +
            $"IsServer={IsServer} | " +
            $"IsClient={IsClient}");

        if (matchFinished.Value)
            return;

        // HOST: przeciwnik (Client) siê roz³¹czy³
        if (IsServer)
        {
            if (clientId != NetworkManager.LocalClientId)
            {
                EndMatchServer(
                    clientId,
                    MatchEndReason.Disconnect);
            }

            return;
        }

        // CLIENT: utraciliœmy po³¹czenie z Hostem
        ShowHostDisconnectedVictory();
    }
}