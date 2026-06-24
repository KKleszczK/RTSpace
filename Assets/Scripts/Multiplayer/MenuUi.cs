using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUi : MonoBehaviour
{
    [Header("Relay")]
    public RelayManager relayManager;

    [Header("UI")]
    public TMP_InputField joinCodeInput;
    public TMP_Text currentCodeText;

    public Button hostButton;
    public Button joinButton;
    public Button startButton;
    

    public Button copyButton;

    private string currentJoinCode;

    private void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);
        startButton.onClick.AddListener(StartGame);

        startButton.interactable = false;

        copyButton.onClick.AddListener(CopyCode);

        copyButton.interactable = false;

    }

    private void Update()
    {
        UpdateButtons();
        UpdateStartButton();
    }

    private void UpdateStartButton()
    {
        if (startButton == null || copyButton == null)
            return;

        if (NetworkManager.Singleton == null)
        {
            startButton.interactable = false;
            copyButton.interactable = false;
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;
        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;

        startButton.interactable = isHost && playerCount == 2;

        copyButton.interactable =
    isHost && !string.IsNullOrEmpty(currentJoinCode);
    }

    public async void HostGame()
    {
        hostButton.interactable = false;
        joinButton.interactable = false;

        try
        {
            string code = await relayManager.CreateRelay();

            if (string.IsNullOrEmpty(code))
                return;

            currentJoinCode = code;
            currentCodeText.text = "CODE: " + code;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Host error: " + e.Message);
            currentCodeText.text = "Host error";
        }
    }

    public async void JoinGame()
    {
        string code = joinCodeInput.text.Trim().ToUpper();

        if (!IsValidJoinCode(code))
        {
            //currentCodeText.text = "Invalid code";
            joinCodeInput.text = "";
            return;
        }

        hostButton.interactable = false;
        joinButton.interactable = false;

        try
        {
            await relayManager.JoinRelay(code);
        }
        catch (System.Exception e)
        {
            //Debug.LogError("Nie uda³o siê do³¹czyæ: " + e.Message);

            //currentCodeText.text = "Wrong or expired code";
            joinCodeInput.text = "";
        }
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost)
            return;

        if (NetworkManager.Singleton.ConnectedClientsList.Count != 2)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single
        );


    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(currentJoinCode))
            return;

        GUIUtility.systemCopyBuffer = currentJoinCode;

        Debug.Log("Join code copied!");
    }

    private void UpdateButtons()
    {
        if (hostButton == null || joinButton == null || startButton == null || copyButton == null)
            return;

        int codeLength = joinCodeInput.text.Trim().Length;

        hostButton.interactable = codeLength == 0 && !NetworkManager.Singleton.IsListening;
        joinButton.interactable = codeLength == 6 && !NetworkManager.Singleton.IsListening;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        int playerCount = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.ConnectedClientsList.Count
            : 0;

        startButton.interactable = isHost && playerCount == 2;
        copyButton.interactable = isHost && !string.IsNullOrEmpty(currentJoinCode);
    }

    private bool IsValidJoinCode(string code)
    {
        if (code.Length < 6 || code.Length > 12)
            return false;

        string allowed = "6789BCDFGHJKLMNPQRTW";

        foreach (char c in code.ToUpper())
        {
            if (!allowed.Contains(c))
                return false;
        }

        return true;
    }

}