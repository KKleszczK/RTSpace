using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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


    private void Start()
    {
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
                action);
        }
    }


    private void ResetToDefaults()
    {
        Debug.Log(
            "[INPUT] Reset to defaults - do implementacji.");
    }
}