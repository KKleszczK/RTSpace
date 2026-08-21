using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShipModuleIconsUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private ShipUnit ship;

    [Header("Module Icons")]
    [SerializeField] private Image normalModuleIcon1;
    [SerializeField] private Image normalModuleIcon2;
    [SerializeField] private Image normalModuleIcon3;
    [SerializeField] private Image classModuleIcon;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (ship == null)
        {
            ship =
                GetComponentInParent<ShipUnit>();
        }

        ClearIcons();
    }

    private void OnEnable()
    {
        if (ship == null)
        {
            ship =
                GetComponentInParent<ShipUnit>();
        }

        if (ship == null)
        {
            Debug.LogError(
                "[MODULE UI] Nie znaleziono ShipUnit.",
                this);

            return;
        }

        // Nas³uchujemy zmian wszystkich czterech slotów.
        ship.normalModule1.OnValueChanged +=
            OnModuleChanged;

        ship.normalModule2.OnValueChanged +=
            OnModuleChanged;

        ship.normalModule3.OnValueChanged +=
            OnModuleChanged;

        ship.classModule.OnValueChanged +=
            OnModuleChanged;

        RefreshIcons();
    }

    private void OnDisable()
    {
        if (ship == null)
            return;

        ship.normalModule1.OnValueChanged -=
            OnModuleChanged;

        ship.normalModule2.OnValueChanged -=
            OnModuleChanged;

        ship.normalModule3.OnValueChanged -=
            OnModuleChanged;

        ship.classModule.OnValueChanged -=
            OnModuleChanged;
    }

    // =========================================================
    // NETWORK CHANGE
    // =========================================================

    private void OnModuleChanged(
        FixedString64Bytes oldValue,
        FixedString64Bytes newValue)
    {
        RefreshIcons();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void RefreshIcons()
    {
        if (ship == null)
            return;

        SetIcon(
            normalModuleIcon1,
            ship.normalModule1.Value);

        SetIcon(
            normalModuleIcon2,
            ship.normalModule2.Value);

        SetIcon(
            normalModuleIcon3,
            ship.normalModule3.Value);

        SetIcon(
            classModuleIcon,
            ship.classModule.Value);
    }

    // =========================================================
    // SET ICON
    // =========================================================

    private void SetIcon(
    Image image,
    FixedString64Bytes moduleId)
    {
        if (image == null)
            return;

        // Pusty slot.
        if (moduleId.IsEmpty)
        {
            ClearIcon(image);
            return;
        }

        if (ModuleDatabase.Instance == null)
        {
            Debug.LogWarning(
                "[MODULE UI] ModuleDatabase.Instance == null",
                this);

            ClearIcon(image);
            return;
        }

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        if (module == null)
        {
            Debug.LogWarning(
                $"[MODULE UI] Nie znaleziono modu³u: {moduleId}",
                this);

            ClearIcon(image);
            return;
        }

        if (module.icon == null)
        {
            Debug.LogWarning(
                $"[MODULE UI] Modu³ {module.moduleId} nie ma ikony.",
                this);

            ClearIcon(image);
            return;
        }

        // Ustawia jednoczeœnie:
        // - sprite modu³u
        // - kolor zale¿ny od tieru
        ModuleTierColorHelper.ApplyToImage(
            image,
            module);

        image.enabled = true;
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void ClearIcons()
    {
        ClearIcon(normalModuleIcon1);
        ClearIcon(normalModuleIcon2);
        ClearIcon(normalModuleIcon3);
        ClearIcon(classModuleIcon);
    }

    private void ClearIcon(
        Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.enabled = false;
    }
}