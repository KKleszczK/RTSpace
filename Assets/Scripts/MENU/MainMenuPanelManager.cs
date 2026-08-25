using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanelManager : MonoBehaviour
{
    // =========================================================
    // PANELS
    // =========================================================

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private CanvasGroup lobbyGroup;
    [SerializeField] private CanvasGroup optionsGroup;
    [SerializeField] private CanvasGroup creditsGroup;
    [SerializeField] private CanvasGroup backGroup;

    // =========================================================
    // RELAY
    // =========================================================

    [Header("Relay")]
    [SerializeField] private RelayManager relayManager;

    // =========================================================
    // LOBBY UI
    // =========================================================

    [Header("Lobby")]
    [SerializeField] private LobbyManager lobbyManager;

    [Header("Join")]
    [SerializeField] private CanvasGroup joinGroup;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Text joinButtonText;

    [SerializeField]
    private Color joinInactiveColor =
        new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField]
    private Color joinActiveColor =
        Color.white;

    private bool isJoining;

    private bool isLeavingLobby;

    private bool wasConnectedToLobby;


    // =========================================================
    // STATE
    // =========================================================

    private bool isCreatingLobby;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (joinCodeInput != null)
        {
            joinCodeInput.characterLimit = 6;

            joinCodeInput.onValueChanged.AddListener(
                OnJoinCodeChanged);
        }

        RefreshJoinButton();
        ShowMainMenu();
    }

    private void Update()
    {
        CheckLostHostConnection();
    }

    private void CheckLostHostConnection()
    {
        if (!wasConnectedToLobby)
            return;

        if (isLeavingLobby)
            return;

        if (NetworkManager.Singleton == null)
        {
            ReturnGuestAfterHostDisconnect();
            return;
        }

        // Je¿eli nadal jesteœmy poprawnie
        // po³¹czonym klientem, nic nie robimy.
        if (NetworkManager.Singleton.IsConnectedClient)
            return;

        // Nie jesteœmy ju¿ po³¹czeni z hostem.
        ReturnGuestAfterHostDisconnect();
    }

    private void ReturnGuestAfterHostDisconnect()
    {
        if (!wasConnectedToLobby)
            return;

        wasConnectedToLobby = false;
        isLeavingLobby = true;

        Debug.Log(
            "[MENU] Utracono hosta - powrót do Main Menu.");

        ShowMainMenu();

        isLeavingLobby = false;
    }

    // =========================================================
    // MAIN MENU
    // =========================================================

    public void ShowMainMenu()
    {
        ShowOnly(
            mainMenuGroup);
    }

    // =========================================================
    // HOST
    // =========================================================

    public async void HostGame()
    {
        if (isCreatingLobby)
            return;

        if (relayManager == null)
        {
            Debug.LogError(
                "[MENU] Brak RelayManager.");

            return;
        }

        isCreatingLobby = true;

        try
        {
            string code =
                await relayManager.CreateRelay();

            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError(
                    "[MENU] Relay nie zwróci³ kodu.");

                return;
            }

            // =============================================
            // USTAW KOD W LOBBY
            // =============================================

            if (lobbyManager != null)
            {
                lobbyManager.SetJoinCode(
                    code);
            }

            // =============================================
            // POKA¯ LOBBY
            // =============================================

            ShowOnly(
                lobbyGroup);

            Debug.Log(
                "[MENU] Lobby utworzone. Code: " +
                code);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "[MENU] Host error: " +
                e.Message);
        }
        finally
        {
            isCreatingLobby = false;
        }
    }

    // =========================================================
    // OPTIONS
    // =========================================================

    public void ShowOptions()
    {
        ShowOnly(
            optionsGroup);
    }

    // =========================================================
    // CREDITS
    // =========================================================

    public void ShowCredits()
    {
        ShowOnly(
            creditsGroup);
    }

    // =========================================================
    // PANEL CONTROL
    // =========================================================

    private void ShowOnly(
    CanvasGroup target)
    {
        SetGroup(mainMenuGroup, false);
        SetGroup(lobbyGroup, false);
        SetGroup(optionsGroup, false);
        SetGroup(creditsGroup, false);
        SetGroup(joinGroup, false);

        SetGroup(target, true);

        // BACK jest widoczny wszêdzie
        // poza g³ównym menu.
        bool showBack =
            target != mainMenuGroup;

        SetGroup(
            backGroup,
            showBack);
    }

    private void SetGroup(
        CanvasGroup group,
        bool visible)
    {
        if (group == null)
            return;

        group.alpha =
            visible ? 1f : 0f;

        group.interactable =
            visible;

        group.blocksRaycasts =
            visible;
    }
    public void ShowJoin()
    {
        ShowOnly(
            joinGroup);

        if (joinCodeInput != null)
        {
            joinCodeInput.text = "";
            joinCodeInput.ActivateInputField();
        }

        RefreshJoinButton();
    }
    private void OnJoinCodeChanged(
    string value)
    {
        if (joinCodeInput == null)
            return;

        string normalized =
            value
                .Trim()
                .ToUpper();

        if (joinCodeInput.text != normalized)
        {
            joinCodeInput.SetTextWithoutNotify(
                normalized);
        }

        RefreshJoinButton();
    }
    private void RefreshJoinButton()
    {
        if (joinButton == null ||
            joinCodeInput == null)
        {
            return;
        }

        bool hasValidLength =
            joinCodeInput.text
                .Trim()
                .Length == 6;

        bool canJoin =
            hasValidLength &&
            !isJoining;

        // Mo¿liwoœæ klikniêcia.
        joinButton.interactable =
            canJoin;

        // Zmieniamy KOLOR NAPISU,
        // a nie kolor Image przycisku.
        if (joinButtonText != null)
        {
            joinButtonText.color =
                canJoin
                    ? joinActiveColor
                    : joinInactiveColor;
        }
    }
    public async void JoinGame()
    {
        if (isJoining)
            return;

        if (relayManager == null ||
            joinCodeInput == null)
        {
            return;
        }

        string code =
            joinCodeInput.text
                .Trim()
                .ToUpper();

        if (code.Length != 6)
            return;

        isJoining = true;
        RefreshJoinButton();

        try
        {
            await relayManager.JoinRelay(
                code);

            /*
             * StartClient() nie oznacza jeszcze,
             * ¿e po³¹czenie z hostem jest ju¿ gotowe.
             *
             * Czekamy, a¿ klient faktycznie
             * zostanie po³¹czony.
             */
            float timeout = 10f;
            float elapsed = 0f;

            while (NetworkManager.Singleton != null &&
                   !NetworkManager.Singleton.IsConnectedClient &&
                   elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                await System.Threading.Tasks.Task.Yield();
            }

            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.LogWarning(
                    "[MENU] Nie uda³o siê po³¹czyæ z lobby.");

                joinCodeInput.text = "";
                return;
            }

            // =====================================================
            // PO£¥CZENIE UDANE
            // =====================================================

            // Guest równie¿ dostaje kod lobby
            // na przycisku COPY.
            wasConnectedToLobby = true;

            if (lobbyManager != null)
            {
                lobbyManager.SetJoinCode(
                    code);
            }

            ShowOnly(
                lobbyGroup);

            Debug.Log(
                "[MENU] Do³¹czono do lobby jako guest.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "[MENU] Join failed: " +
                e.Message);

            joinCodeInput.text = "";
        }
        finally
        {
            isJoining = false;
            RefreshJoinButton();
        }
    }
    public void QuitGame()
    {
        Debug.Log("[MENU] Quit Game");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void BackToMainMenu()
    {
        if (isLeavingLobby)
            return;

        isLeavingLobby = true;
        wasConnectedToLobby = false;

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            // Tylko serwer mo¿e resetowaæ
            // NetworkVariable lobby.
            if (NetworkManager.Singleton.IsServer &&
                lobbyManager != null)
            {
                lobbyManager.ResetLobby();
            }

            NetworkManager.Singleton.Shutdown();

            Debug.Log(
                "[MENU] Opuszczono sesjê.");
        }

        ShowMainMenu();

        isLeavingLobby = false;
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -=
                OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(
    ulong clientId)
    {
        if (!wasConnectedToLobby)
            return;

        if (isLeavingLobby)
            return;

        ReturnGuestAfterHostDisconnect();
    }
}