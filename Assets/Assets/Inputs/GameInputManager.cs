using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }

    private GameInputActions inputActions;

    public GameInputActions InputActions =>
        inputActions;

    public bool QueueCommandPressed =>
    inputActions != null &&
    inputActions.Gameplay.QueueCommand.IsPressed();


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(
            gameObject);

        inputActions =
            new GameInputActions();
    }


    private void OnEnable()
    {
        inputActions?.Enable();
    }


    private void OnDisable()
    {
        inputActions?.Disable();
    }
}