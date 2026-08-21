using TMPro;
using UnityEngine;

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

    // =========================================================
    // RELAY
    // =========================================================

    [Header("Relay")]
    [SerializeField] private RelayManager relayManager;

    // =========================================================
    // LOBBY UI
    // =========================================================

    [Header("Lobby UI")]
    [SerializeField] private TMP_Text lobbyCodeText;

    // =========================================================
    // STATE
    // =========================================================

    private bool isCreatingLobby;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ShowMainMenu();
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

            if (lobbyCodeText != null)
            {
                lobbyCodeText.text =
                    "CODE: " + code;
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
        SetGroup(
            mainMenuGroup,
            false);

        SetGroup(
            lobbyGroup,
            false);

        SetGroup(
            optionsGroup,
            false);

        SetGroup(
            creditsGroup,
            false);

        SetGroup(
            target,
            true);
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
}