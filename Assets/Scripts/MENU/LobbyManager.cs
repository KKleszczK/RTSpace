using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Code")]
    [SerializeField] private Button codeButton;
    [SerializeField] private TMP_Text codeButtonText;

    [Header("Ready Status")]
    [SerializeField] private TMP_Text hostReadyText;
    [SerializeField] private TMP_Text clientReadyText;

    [Header("Ready / Start Button")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;

    [Header("Countdown")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Player Slots")]
    [SerializeField] private Image hostPlayerImage;
    [SerializeField] private Image guestPlayerImage;

    [SerializeField] private TMP_Text hostPlayerText;
    [SerializeField] private TMP_Text guestPlayerText;

    [Header("Player Slot Graphics")]
    [SerializeField] private Sprite localPlayerSprite;
    [SerializeField] private Sprite enemyPlayerSprite;

    // =========================================================
    // COLORS
    // =========================================================

    [Header("Colors")]
    [SerializeField]
    private Color notReadyColor =
        new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField]
    private Color readyColor =
        Color.white;

    // =========================================================
    // NETWORK STATE
    // =========================================================

    public NetworkVariable<bool> hostReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> clientReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> countdownStarted = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // LOCAL STATE
    // =========================================================

    private string currentJoinCode;

    // =========================================================
    // NETWORK
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        hostReady.OnValueChanged +=
            OnReadyStateChanged;

        clientReady.OnValueChanged +=
            OnReadyStateChanged;

        countdownStarted.OnValueChanged +=
            OnCountdownStateChanged;

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(
                OnReadyButtonClicked);

            readyButton.onClick.AddListener(
                OnReadyButtonClicked);
        }

        if (codeButton != null)
        {
            codeButton.onClick.RemoveListener(
                CopyJoinCode);

            codeButton.onClick.AddListener(
                CopyJoinCode);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(
                false);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }

        RefreshLobbyUI();
        RefreshPlayerSlots();
    }

    private void OnClientConnected(
    ulong clientId)
    {
        RefreshPlayerSlots();
    }

    private void OnClientDisconnected(
        ulong clientId)
    {
        RefreshPlayerSlots();
    }

    public override void OnNetworkDespawn()
    {
        hostReady.OnValueChanged -=
            OnReadyStateChanged;

        clientReady.OnValueChanged -=
            OnReadyStateChanged;

        countdownStarted.OnValueChanged -=
            OnCountdownStateChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // JOIN CODE
    // =========================================================

    public void SetJoinCode(
        string code)
    {
        currentJoinCode =
            code;

        if (codeButtonText != null)
        {
            codeButtonText.text =
                code;
        }
    }

    private void CopyJoinCode()
    {
        if (string.IsNullOrEmpty(
                currentJoinCode))
        {
            return;
        }

        GUIUtility.systemCopyBuffer =
            currentJoinCode;

        Debug.Log(
            "[LOBBY] Join code copied.");
    }

    // =========================================================
    // READY BUTTON
    // =========================================================

    private void OnReadyButtonClicked()
    {
        if (countdownStarted.Value)
            return;

        // =====================================================
        // HOST
        // =====================================================

        if (IsHost)
        {
            // Host jeszcze nie jest READY.
            if (!hostReady.Value)
            {
                ToggleReadyServerRpc();
                return;
            }

            // Host ju¿ jest READY.
            // Przycisk dzia³a teraz jako START,
            // ale tylko gdy guest równie¿ jest READY.
            if (clientReady.Value)
            {
                StartCountdownServerRpc();
            }

            return;
        }

        // =====================================================
        // GUEST
        // =====================================================

        // Guest mo¿e READY odklikaæ i klikn¹æ ponownie.
        ToggleReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(
    ServerRpcParams rpcParams = default)
    {
        if (countdownStarted.Value)
            return;

        ulong senderId =
            rpcParams.Receive.SenderClientId;

        // =====================================================
        // HOST
        // =====================================================

        if (senderId ==
            NetworkManager.ServerClientId)
        {
            // Host mo¿e tylko wejœæ w READY.
            // Nie mo¿e ju¿ go odklikn¹æ.
            if (!hostReady.Value)
            {
                hostReady.Value = true;
            }

            return;
        }

        // =====================================================
        // GUEST
        // =====================================================

        // Guest mo¿e prze³¹czaæ READY
        // dopóki countdown siê nie rozpocz¹³.
        clientReady.Value =
            !clientReady.Value;
    }

    // =========================================================
    // START
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    private void StartCountdownServerRpc(
        ServerRpcParams rpcParams = default)
    {
        /*
         * Tylko host mo¿e rozpocz¹æ grê.
         */
        if (rpcParams.Receive.SenderClientId !=
            NetworkManager.ServerClientId)
        {
            return;
        }

        if (!hostReady.Value ||
            !clientReady.Value)
        {
            return;
        }

        if (countdownStarted.Value)
            return;

        countdownStarted.Value =
            true;

        StartCoroutine(
            CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        ShowCountdownClientRpc(
            3);

        yield return new WaitForSeconds(
            1f);

        ShowCountdownClientRpc(
            2);

        yield return new WaitForSeconds(
            1f);

        ShowCountdownClientRpc(
            1);

        yield return new WaitForSeconds(
            1f);

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single);
    }

    [ClientRpc]
    private void ShowCountdownClientRpc(
        int value)
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(
            true);

        countdownText.text =
            value.ToString();
    }

    // =========================================================
    // NETWORK CALLBACKS
    // =========================================================

    private void OnReadyStateChanged(
        bool oldValue,
        bool newValue)
    {
        RefreshLobbyUI();
    }

    private void OnCountdownStateChanged(
        bool oldValue,
        bool newValue)
    {
        RefreshLobbyUI();
    }

    // =========================================================
    // UI REFRESH
    // =========================================================

    private void RefreshLobbyUI()
    {
        // =====================================================
        // READY IDENTIFIERS
        // =====================================================

        if (hostReadyText != null)
        {
            hostReadyText.color =
                hostReady.Value
                    ? readyColor
                    : notReadyColor;
        }

        if (clientReadyText != null)
        {
            clientReadyText.color =
                clientReady.Value
                    ? readyColor
                    : notReadyColor;
        }

        if (readyButton == null ||
            readyButtonText == null)
        {
            return;
        }

        // =====================================================
        // COUNTDOWN
        // =====================================================

        if (countdownStarted.Value)
        {
            readyButton.interactable = false;

            if (readyButton.image != null)
            {
                readyButton.image.color =
                    notReadyColor;
            }

            return;
        }

        // =====================================================
        // HOST
        // =====================================================

        if (IsHost)
        {
            // Host jeszcze nie klikn¹³ READY.
            if (!hostReady.Value)
            {
                readyButtonText.text =
                    "READY";

                readyButton.interactable =
                    true;

                if (readyButton.image != null)
                {
                    readyButton.image.color =
                        notReadyColor;
                }

                return;
            }

            // Host jest ju¿ READY.
            // READY zmienia siê na START.
            readyButtonText.text =
                "START";

            // START aktywny dopiero,
            // gdy guest równie¿ jest READY.
            bool canStart =
                clientReady.Value;

            readyButton.interactable =
                canStart;

            if (readyButton.image != null)
            {
                readyButton.image.color =
                    canStart
                        ? readyColor
                        : notReadyColor;
            }

            return;
        }

        // =====================================================
        // GUEST
        // =====================================================

        // U guesta napis ZAWSZE pozostaje READY.
        readyButtonText.text =
            "READY";

        readyButton.interactable =
            true;

        if (readyButton.image != null)
        {
            readyButton.image.color =
                clientReady.Value
                    ? readyColor
                    : notReadyColor;
        }
    }

    private void RefreshPlayerSlots()
    {
        if (NetworkManager.Singleton == null)
            return;

        int playerCount =
            NetworkManager.Singleton.ConnectedClientsList.Count;

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        ulong hostClientId =
            NetworkManager.ServerClientId;

        // =====================================================
        // HOST SLOT
        // =====================================================

        bool hostExists =
            playerCount >= 1;

        if (hostPlayerImage != null)
        {
            hostPlayerImage.gameObject.SetActive(
                hostExists);

            if (hostExists)
            {
                bool hostIsLocal =
                    hostClientId ==
                    localClientId;

                hostPlayerImage.sprite =
                    hostIsLocal
                        ? localPlayerSprite
                        : enemyPlayerSprite;

                hostPlayerImage.color =
                    PlayerColorHelper.GetColor(
                        hostClientId);
            }
        }

        if (hostPlayerText != null)
        {
            hostPlayerText.text =
                hostExists
                    ? "HOST (IN LOBBY)"
                    : "EMPTY";
        }

        // =====================================================
        // FIND GUEST
        // =====================================================

        ulong guestClientId =
            ulong.MaxValue;

        foreach (ulong clientId in
                 NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId ==
                hostClientId)
            {
                continue;
            }

            guestClientId =
                clientId;

            break;
        }

        bool guestExists =
            guestClientId !=
            ulong.MaxValue;

        // =====================================================
        // GUEST SLOT
        // =====================================================

        if (guestPlayerImage != null)
        {
            guestPlayerImage.gameObject.SetActive(
                guestExists);

            if (guestExists)
            {
                bool guestIsLocal =
                    guestClientId ==
                    localClientId;

                guestPlayerImage.sprite =
                    guestIsLocal
                        ? localPlayerSprite
                        : enemyPlayerSprite;

                guestPlayerImage.color =
                    PlayerColorHelper.GetColor(
                        guestClientId);
            }
        }

        if (guestPlayerText != null)
        {
            guestPlayerText.text =
                guestExists
                    ? "GUEST (IN LOBBY)"
                    : "EMPTY";
        }
    }

    
}