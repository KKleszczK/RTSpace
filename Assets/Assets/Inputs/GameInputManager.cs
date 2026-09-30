using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }

    private const string BindingOverridesKey =
        "InputBindingOverrides";

    private GameInputActions inputActions;


    public GameInputActions InputActions =>
        inputActions;

    /*
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

    */

    // =========================================================
    // Camera Controls [CameraControls]
    // =========================================================

    public bool CameraUpPressed =>
        inputActions != null &&
        inputActions.CameraControls.Cameraup.IsPressed();


    public bool CameraRightPressed =>
        inputActions != null &&
        inputActions.CameraControls.Cameraright.IsPressed();


    public bool CameraDownPressed =>
        inputActions != null &&
        inputActions.CameraControls.Cameradown.IsPressed();


    public bool CameraLeftPressed =>
        inputActions != null &&
        inputActions.CameraControls.Cameraleft.IsPressed();

    public bool CenterCameraOnAlert =>
        inputActions != null &&
        inputActions.CameraControls.Centercameraonalert.WasPressedThisFrame();

    // =========================================================
    // Selections
    // =========================================================

    public bool SelectAllArmy =>
        inputActions != null &&
        inputActions.Selections.Selectallarmy.WasPressedThisFrame();

    public bool SelectStation =>
        inputActions != null &&
        inputActions.Selections.Selectstation.WasPressedThisFrame();

    public bool ActivateSelectionFromControlGroup2 =>
        inputActions != null &&
        inputActions.Selections.Activateselectionfromcontrolgroup2.WasPressedThisFrame();

    public bool ActivateSelectionFromControlGroup3 =>
        inputActions != null &&
        inputActions.Selections.Activateselectionfromcontrolgroup3.WasPressedThisFrame();

    public bool ActivateSelectionFromControlGroup4 =>
        inputActions != null &&
        inputActions.Selections.Activateselectionfromcontrolgroup4.WasPressedThisFrame();

    public bool ActivateSelectionFromControlGroup5 =>
        inputActions != null &&
        inputActions.Selections.Activateselectionfromcontrolgroup5.WasPressedThisFrame();

    public bool SelectorPressed =>
        inputActions != null &&
        inputActions.Selections.Selector.WasPressedThisFrame();

    public bool SelectorHeld =>
        inputActions != null &&
        inputActions.Selections.Selector.IsPressed();

    public bool SelectorReleased =>
        inputActions != null &&
        inputActions.Selections.Selector.WasReleasedThisFrame();

    public bool SelectUnitsSharingThesameClass =>
        inputActions != null &&
        inputActions.Selections.Selectunitssharingthesameclass.IsPressed();

    // =========================================================
    // Base
    // =========================================================

    public bool Deplay =>
        inputActions != null &&
        inputActions.Base.Deplay.WasPressedThisFrame();

    public bool ClearModulesFromShip =>
        inputActions != null &&
        inputActions.Base.Clearmodulesfromship.WasPressedThisFrame();

    public bool selectModuleT2 =>
            inputActions != null &&
            inputActions.Base.selectmoduleT2.IsPressed();

    public bool selectModuleT3 =>
            inputActions != null &&
            inputActions.Base.selectmoduleT3.IsPressed();

    public bool ChangeStationTab =>
            inputActions != null &&
            inputActions.Base.Changestationtab.WasPressedThisFrame();

    public bool machineGun =>
            inputActions != null &&
            inputActions.Base.machinegun.WasPressedThisFrame();

    public bool MiningLaser =>
            inputActions != null &&
            inputActions.Base.Mininglaser.WasPressedThisFrame();

    public bool RampUpLaser =>
            inputActions != null &&
            inputActions.Base.Rampuplaser.WasPressedThisFrame();

    public bool MobileAssembly =>
            inputActions != null &&
            inputActions.Base.Mobileassembly.WasPressedThisFrame();

    public bool HullReinforcement =>
            inputActions != null &&
            inputActions.Base.Hullreinforcement.WasPressedThisFrame();

    public bool ShieldBattery =>
            inputActions != null &&
            inputActions.Base.Shieldbattery.WasPressedThisFrame();

    public bool PlasmaGun =>
            inputActions != null &&
            inputActions.Base.Plasmagun.WasPressedThisFrame();

    public bool QueueCancel =>
            inputActions != null &&
            inputActions.Base.Queuecancel.WasPressedThisFrame();

    public bool TorpedoLauncher =>
            inputActions != null &&
            inputActions.Base.Torpedolauncher.WasPressedThisFrame();

    public bool MiningSlavager =>
            inputActions != null &&
            inputActions.Base.Miningslavager.WasPressedThisFrame();

    public bool FighterShip =>
            inputActions != null &&
            inputActions.Base.FighterShip.WasPressedThisFrame();

    public bool UtilityShip =>
            inputActions != null &&
            inputActions.Base.UtilityShip.WasPressedThisFrame();

    public bool MinerShip =>
            inputActions != null &&
            inputActions.Base.MinerShip.WasPressedThisFrame();

    public bool SpeedEngines =>
            inputActions != null &&
            inputActions.Base.Speedengines.WasPressedThisFrame();

    public bool ReconstructionModule =>
            inputActions != null &&
            inputActions.Base.Reconstructionmodule.WasPressedThisFrame();

    public bool MiningExplosives =>
            inputActions != null &&
            inputActions.Base.Miningexplosives.WasPressedThisFrame();

    public bool LightningTurret =>
            inputActions != null &&
            inputActions.Base.Lightningturret.WasPressedThisFrame();

    public bool PierceLaser =>
            inputActions != null &&
            inputActions.Base.Piercelaser.WasPressedThisFrame();

    public bool ScienceLabModule =>
            inputActions != null &&
            inputActions.Base.Sciencelabmodule.WasPressedThisFrame();

    public bool MicroReactor =>
            inputActions != null &&
            inputActions.Base.Microreactor.WasPressedThisFrame();

    public bool DencityScanner =>
            inputActions != null &&
            inputActions.Base.Dencityscanner.WasPressedThisFrame();

    public bool RangeBooster =>
            inputActions != null &&
            inputActions.Base.Rangebooster.WasPressedThisFrame();

    public bool ShieldBraker =>
            inputActions != null &&
            inputActions.Base.Shieldbraker.WasPressedThisFrame();

    // =========================================================
    // Ships
    // =========================================================

    public bool Guard =>
            inputActions != null &&
            inputActions.Ships.Guard.WasPressedThisFrame();

    public bool Stop =>
            inputActions != null &&
            inputActions.Ships.Stop.WasPressedThisFrame();

    public bool Attack =>
            inputActions != null &&
            inputActions.Ships.Attack.WasPressedThisFrame();

    public bool AttackMove =>
            inputActions != null &&
            inputActions.Ships.AttackMove.WasPressedThisFrame();

    public bool Move =>
            inputActions != null &&
            inputActions.Ships.Move.WasPressedThisFrame();

    public bool QueueCommand =>
            inputActions != null &&
            inputActions.Ships.Queuecommand.IsPressed();

    public bool UseSkill1 =>
            inputActions != null &&
            inputActions.Ships.Useskill1.WasPressedThisFrame();

    public bool UseSkill2 =>
            inputActions != null &&
            inputActions.Ships.Useskill2.WasPressedThisFrame();

    public bool UseSkill3 =>
            inputActions != null &&
            inputActions.Ships.Useskill3.WasPressedThisFrame();

    public bool UseSkill4 =>
            inputActions != null &&
            inputActions.Ships.Useskill4.WasPressedThisFrame();

    public bool Escort =>
            inputActions != null &&
            inputActions.Ships.Escort.WasPressedThisFrame();

    public bool BackToBase =>
            inputActions != null &&
            inputActions.Ships.Backtobase.WasPressedThisFrame();

    public bool Dock =>
            inputActions != null &&
            inputActions.Ships.Dock.WasPressedThisFrame();

    public bool SetControlGroupFromSelection =>
            inputActions != null &&
            inputActions.Ships.Setcontrolgroupfromselection.IsPressed();





    public void SetShipsInputActive(bool active)
    {
        if (inputActions == null)
            return;

        if (active)
        {
            inputActions.Ships.Enable();
            inputActions.Base.Disable();
        }
        else
        {
            inputActions.Ships.Disable();
        }
    }

    public void SetBaseInputActive(bool active)
    {
        if (inputActions == null)
            return;

        if (active)
        {
            inputActions.Base.Enable();
            inputActions.Ships.Disable();
        }
        else
        {
            inputActions.Base.Disable();
        }
    }

    public void ClearContextInput()
    {
        if (inputActions == null)
            return;

        inputActions.Ships.Disable();
        inputActions.Base.Disable();
    }





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
        if (inputActions == null)
            return;

        inputActions.CameraControls.Enable();
        inputActions.Selections.Enable();

        inputActions.Ships.Disable();
        inputActions.Base.Disable();
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
    InputAction actionToCheck,
    int bindingIndex,
    out InputAction conflictingAction)
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

        InputActionMap sourceMap =
            actionToCheck.actionMap;

        foreach (InputActionMap map
                 in inputActions.asset.actionMaps)
        {
            // Ships i Base nigdy nie s¹ aktywne jednoczeœnie,
            // wiêc mog¹ u¿ywaæ tych samych klawiszy.
            bool shipsBaseException =
                (sourceMap.name == "Ships" &&
                 map.name == "Base") ||
                (sourceMap.name == "Base" &&
                 map.name == "Ships");

            if (shipsBaseException)
                continue;

            foreach (InputAction action in map.actions)
            {
                if (action == actionToCheck)
                    continue;

                foreach (InputBinding binding
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
                        conflictingAction = action;
                        return true;
                    }
                }
            }
        }

        return false;
    }


}