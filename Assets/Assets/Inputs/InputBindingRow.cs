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


    public void Initialize(
        InputAction action)
    {
        inputAction =
            action;

        Refresh();
    }


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


    private string GetDisplayActionName(
        string actionName)
    {
        // "QueueCommand" -> "Queue Command"

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
}