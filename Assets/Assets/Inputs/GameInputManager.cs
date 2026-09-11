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

    public bool StopPressed =>
        inputActions != null &&
        inputActions.Gameplay.Stop.WasPressedThisFrame();

    public bool CameraUpPressed =>
        inputActions != null &&
        inputActions.Gameplay.CameraUp.IsPressed();

    public bool CameraDownPressed =>
        inputActions != null &&
        inputActions.Gameplay.CameraDown.IsPressed();

    public bool CameraLeftPressed =>
        inputActions != null &&
        inputActions.Gameplay.CameraLeft.IsPressed();

    public bool CameraRightPressed =>
        inputActions != null &&
        inputActions.Gameplay.CameraRight.IsPressed();

    public bool AttackMovePressed =>
    inputActions != null &&
    inputActions.Gameplay.AttackMove.WasPressedThisFrame();

    public bool GuardPressed =>
    inputActions != null &&
    inputActions.Gameplay.Guard.WasPressedThisFrame();

    public bool FollowPressed =>
    inputActions != null &&
    inputActions.Gameplay.Follow.WasPressedThisFrame();

    public bool EscortPressed =>
    inputActions != null &&
    inputActions.Gameplay.Escort.WasPressedThisFrame();

    public bool BackToBasePressed =>
    inputActions != null &&
    inputActions.Gameplay.BackToBase.WasPressedThisFrame();

    public bool DockPressed =>
    inputActions != null &&
    inputActions.Gameplay.Dock.WasPressedThisFrame();

    public bool AllArmyPressed =>
    inputActions != null &&
    inputActions.Gameplay.AllArmy.WasPressedThisFrame();

    public bool BaseCameraJumpPressed =>
    inputActions != null &&
    inputActions.Gameplay.BaseCameraJump.WasPressedThisFrame();

    public bool DeployPressed =>
    inputActions != null &&
    inputActions.Gameplay.Deploy.WasPressedThisFrame();

    public bool MinerCraftPressed =>
        inputActions != null &&
        inputActions.Gameplay.MinerCraft.WasPressedThisFrame();

    public bool FighterCraftPressed =>
        inputActions != null &&
        inputActions.Gameplay.FighterCraft.WasPressedThisFrame();

    public bool UtilityCraftPressed =>
        inputActions != null &&
        inputActions.Gameplay.UtilityCraft.WasPressedThisFrame();


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