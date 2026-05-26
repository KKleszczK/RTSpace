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

    private void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);
        startButton.onClick.AddListener(StartGame);

        startButton.interactable = false;
    }

    public async void HostGame()
    {
        hostButton.interactable = false;
        joinButton.interactable = false;

        string code = await relayManager.CreateRelay();

        if (string.IsNullOrEmpty(code))
            return;

        currentCodeText.text = "CODE: " + code;

        startButton.interactable = true;
    }

    public async void JoinGame()
    {
        string code = joinCodeInput.text;

        if (string.IsNullOrWhiteSpace(code))
            return;

        hostButton.interactable = false;
        joinButton.interactable = false;

        await relayManager.JoinRelay(code);
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single
        );
    }
}