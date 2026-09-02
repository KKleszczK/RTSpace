using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }

    private const string BindingOverridesKey =
        "InputBindingOverrides";

    private GameInputActions inputActions;


    public GameInputActions InputActions =>
        inputActions;


    public bool QueueCommandPressed =>
        inputActions != null &&
        inputActions.Gameplay.QueueCommand.IsPressed();


    // =========================================================
    // UNITY
    // =========================================================

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

        LoadBindingOverrides();
    }


    private void OnEnable()
    {
        inputActions?.Enable();
    }


    private void OnDisable()
    {
        inputActions?.Disable();
    }


    // =========================================================
    // SAVE
    // =========================================================

    public void SaveBindingOverrides()
    {
        if (inputActions == null)
            return;

        string json =
            inputActions.asset
                .SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(
            BindingOverridesKey,
            json);

        PlayerPrefs.Save();

        Debug.Log(
            "[INPUT] Binding overrides saved.");
    }


    // =========================================================
    // LOAD
    // =========================================================

    private void LoadBindingOverrides()
    {
        if (inputActions == null)
            return;

        if (!PlayerPrefs.HasKey(
                BindingOverridesKey))
        {
            return;
        }

        string json =
            PlayerPrefs.GetString(
                BindingOverridesKey);

        if (string.IsNullOrEmpty(json))
            return;

        inputActions.asset
            .LoadBindingOverridesFromJson(
                json);

        Debug.Log(
            "[INPUT] Binding overrides loaded.");
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetBindingOverrides()
    {
        if (inputActions == null)
            return;

        inputActions.asset
            .RemoveAllBindingOverrides();

        PlayerPrefs.DeleteKey(
            BindingOverridesKey);

        PlayerPrefs.Save();

        Debug.Log(
            "[INPUT] Binding overrides reset to defaults.");
    }

    public bool HasBindingConflict(
    UnityEngine.InputSystem.InputAction actionToCheck,
    int bindingIndex,
    out UnityEngine.InputSystem.InputAction conflictingAction)
    {
        conflictingAction = null;

        if (inputActions == null ||
            actionToCheck == null)
        {
            return false;
        }

        if (bindingIndex < 0 ||
            bindingIndex >= actionToCheck.bindings.Count)
        {
            return false;
        }

        string effectivePath =
            actionToCheck.bindings[bindingIndex].effectivePath;

        if (string.IsNullOrEmpty(effectivePath))
            return false;


        foreach (UnityEngine.InputSystem.InputActionMap map
                 in inputActions.asset.actionMaps)
        {
            foreach (UnityEngine.InputSystem.InputAction action
                     in map.actions)
            {
                // Nie porównujemy akcji z sam¹ sob¹.
                if (action == actionToCheck)
                    continue;

                foreach (UnityEngine.InputSystem.InputBinding binding
                         in action.bindings)
                {
                    if (string.IsNullOrEmpty(
                            binding.effectivePath))
                    {
                        continue;
                    }

                    if (string.Equals(
                            binding.effectivePath,
                            effectivePath,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        conflictingAction =
                            action;

                        return true;
                    }
                }
            }
        }

        return false;
    }
}