using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class InputBindingRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text actionNameText;

    [SerializeField]
    private Button bindingButton;

    [SerializeField]
    private TMP_Text bindingText;

    private InputAction inputAction;

    private InputActionRebindingExtensions.RebindingOperation
        rebindingOperation;

    private InputBindingsPanel bindingsPanel;


    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(
    InputAction action,
    InputBindingsPanel panel)
    {
        inputAction = action;
        bindingsPanel = panel;

        if (bindingButton != null)
        {
            bindingButton.onClick.AddListener(
                StartRebinding);
        }

        Refresh();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (inputAction == null)
            return;

        if (actionNameText != null)
        {
            actionNameText.text =
                GetDisplayActionName(
                    inputAction.name);
        }

        if (bindingText != null)
        {
            bindingText.text =
                inputAction.GetBindingDisplayString();
        }
    }


    // =========================================================
    // REBIND
    // =========================================================

    private void StartRebinding()
    {
        if (inputAction == null)
            return;

        if (rebindingOperation != null)
            return;

        bindingsPanel?.SetRebindingInfoVisible(true);

        if (bindingText != null)
        {
            bindingText.text =
                "PRESS KEY...";
        }

        if (bindingButton != null)
        {
            bindingButton.interactable =
                false;
        }

        inputAction.Disable();

        rebindingOperation =
            inputAction
                .PerformInteractiveRebinding(0)

                // ESC anuluje zmianê.
                .WithCancelingThrough(
                    "<Keyboard>/escape")

                // Interesuje nas klawiatura.
                .WithControlsHavingToMatchPath(
                    "<Keyboard>")

                .OnCancel(
                    operation =>
                    {
                        FinishRebinding(false);
                    })
                
                .OnComplete(
                    operation =>
                    {
                        FinishRebinding(true);
                    });
                
                        rebindingOperation.Start();
                    }


    // =========================================================
    // FINISH REBIND
    // =========================================================

    private void FinishRebinding()
    {
        rebindingOperation?.Dispose();

        rebindingOperation = null;

        inputAction?.Enable();

        if (bindingButton != null)
        {
            bindingButton.interactable = true;
        }

        bindingsPanel?.SetRebindingInfoVisible(false);

        Refresh();
    }


    // =========================================================
    // DISPLAY NAME
    // =========================================================

    private string GetDisplayActionName(
        string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return "";

        System.Text.StringBuilder result =
            new();

        for (int i = 0;
             i < actionName.Length;
             i++)
        {
            char c =
                actionName[i];

            if (i > 0 &&
                char.IsUpper(c))
            {
                result.Append(' ');
            }

            result.Append(c);
        }

        return result.ToString();
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        rebindingOperation?.Dispose();

        rebindingOperation =
            null;

        if (bindingButton != null)
        {
            bindingButton.onClick.RemoveListener(
                StartRebinding);
        }
    }


    private void FinishRebinding(
    bool saveChanges)
    {
        rebindingOperation?.Dispose();

        rebindingOperation = null;

        inputAction?.Enable();

        if (saveChanges &&
            GameInputManager.Instance != null)
        {
            GameInputManager.Instance
                .SaveBindingOverrides();
        }

        if (bindingButton != null)
        {
            bindingButton.interactable =
                true;
        }

        bindingsPanel?
            .SetRebindingInfoVisible(false);

        Refresh();
    }
}