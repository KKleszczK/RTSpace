using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputBindingsPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject bindingHeaderPrefab;

    [Header("Bindings")]
    [SerializeField]
    private Transform content;

    [SerializeField]
    private InputBindingRow bindingRowPrefab;

    [Header("Controls")]
    [SerializeField]
    private Button resetDefaultsButton;

    [Header("Rebinding")]
    [SerializeField]
    private GameObject rebindCancelInfo;

    private readonly List<InputBindingRow>
    bindingRows = new();

    private InputBindingRow activeRebindingRow;


    private readonly HashSet<string> fixedActions = new()
        {
            "Move",
            "Queue command",
            "Set control group from selection",
            "Activate selection from control group",
            "Center camera on alert",
            "Select units sharing the same class",
            "Selector",
            "Deploy",
            "Clear modules from ship",
            "select module T2",
            "select module T3",
            "Change station tab"
        };

    private void Start()
    {
        SetRebindingInfoVisible(false);

        GenerateBindings();

        if (resetDefaultsButton != null)
        {
            resetDefaultsButton.onClick.AddListener(
                ResetToDefaults);
        }
    }


    private void OnDestroy()
    {
        if (resetDefaultsButton != null)
        {
            resetDefaultsButton.onClick.RemoveListener(
                ResetToDefaults);
        }
    }


    private void GenerateBindings()
    {
        AddMap(
            GameInputManager.Instance.InputActions.Ships.Get());

        AddMap(
            GameInputManager.Instance.InputActions.Base.Get());

        AddMap(
            GameInputManager.Instance.InputActions.CameraControls.Get());
        AddMap(
            GameInputManager.Instance.InputActions.Selections.Get());
    }

    private void AddMap(InputActionMap actionMap)
    {
        GameObject headerObject =
            Instantiate(bindingHeaderPrefab, content);

        TMP_Text headerText =
            headerObject.GetComponentInChildren<TMP_Text>();

        if (headerText != null)
            headerText.text = actionMap.name;

        // Bindy
        foreach (InputAction action in actionMap.actions)
        {
            InputBindingRow row =
                Instantiate(bindingRowPrefab, content);

            row.Initialize(
                action,
                this);

            if (fixedActions.Contains(action.name))
            {
                row.SetButtonInteractable(false);
            }

            bindingRows.Add(row);
        }
    }


    private void ResetToDefaults()
    {
        if (GameInputManager.Instance == null)
            return;

        GameInputManager.Instance
            .ResetBindingOverrides();

        foreach (InputBindingRow row
                 in bindingRows)
        {
            if (row != null)
            {
                row.Refresh();
            }
        }
    }

    public void SetRebindingInfoVisible(bool visible)
    {
        if (rebindCancelInfo != null)
        {
            rebindCancelInfo.SetActive(visible);
        }
    }

    public bool TryStartRebinding(InputBindingRow row)
    {
        // Jakiœ inny wiersz ju¿ czeka na klawisz.
        if (activeRebindingRow != null)
            return false;

        activeRebindingRow = row;

        SetRebindingInfoVisible(true);

        // Wy³¹cz pozosta³e przyciski.
        foreach (InputBindingRow bindingRow in bindingRows)
        {
            if (bindingRow != null &&
                bindingRow != row)
            {
                bindingRow.SetButtonInteractable(false);
            }
        }

        return true;
    }


    public void FinishRebinding(InputBindingRow row)
    {
        // Zabezpieczenie - tylko aktualny wiersz
        // mo¿e zakoñczyæ rebinding.
        if (activeRebindingRow != row)
            return;

        activeRebindingRow = null;

        SetRebindingInfoVisible(false);

        // Ponownie w³¹cz wszystkie przyciski.
        foreach (InputBindingRow bindingRow in bindingRows)
        {
            if (bindingRow != null)
            {
                bindingRow.SetButtonInteractable(true);
            }
        }
    }
}