using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputBindingsPanel : MonoBehaviour
{
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
        if (GameInputManager.Instance == null)
        {
            Debug.LogError(
                "[INPUT] Brak GameInputManager.");

            return;
        }

        if (content == null ||
            bindingRowPrefab == null)
        {
            Debug.LogError(
                "[INPUT] Brak Content lub BindingRowPrefab.");

            return;
        }

        InputActionMap gameplay =
            GameInputManager.Instance
                .InputActions
                .Gameplay
                .Get();

        foreach (InputAction action in gameplay.actions)
        {
            InputBindingRow row =
                Instantiate(
                    bindingRowPrefab,
                    content);

                row.Initialize(
                    action,
                    this);

                bindingRows.Add(
                    row);
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