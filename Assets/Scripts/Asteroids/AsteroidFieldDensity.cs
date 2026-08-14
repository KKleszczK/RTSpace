using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class AsteroidFieldDensity : NetworkBehaviour
{
    // =========================================================
    // DENSITY
    // =========================================================

    [Header("Density")]
    [SerializeField]
    private float startingDensity = 100f;

    [SerializeField]
    private float minimumDensity = 20f;

    [SerializeField]
    private float maximumDensity = 200f;

    public NetworkVariable<float> Density = new(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentDensity =>
        GetEffectiveDensity();

    public float MinimumDensity =>
        minimumDensity;

    public float MaximumDensity =>
        maximumDensity;

    // =========================================================
    // FIELD RANGE
    // =========================================================

    

    [SerializeField, Min(0.05f)]
    private float boosterUpdateInterval = 0.5f;

    private float nextBoosterUpdateTime;

    // =========================================================
    // DENSITY BOOST
    // =========================================================

    private float strongestDensityBoost;

    public float DensityBoost =>
        strongestDensityBoost;

    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField]
    private TMP_Text densityText;

    [SerializeField]
    private Transform densityCanvas;

    // =========================================================
    // VISUAL
    // =========================================================

    [Header("Visual")]
    [SerializeField]
    private AsteroidFieldVisual fieldVisual;

    private Camera mainCamera;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        mainCamera =
            Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        Density.OnValueChanged +=
            OnDensityChanged;

        if (IsServer)
        {
            Density.Value =
                Mathf.Clamp(
                    startingDensity,
                    minimumDensity,
                    maximumDensity);

            strongestDensityBoost = 0f;
        }

        RefreshText(
            GetEffectiveDensity());
    }

    public override void OnNetworkDespawn()
    {
        Density.OnValueChanged -=
            OnDensityChanged;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!IsServer)
            return;

        if (Time.time <
            nextBoosterUpdateTime)
        {
            return;
        }

        nextBoosterUpdateTime =
            Time.time +
            boosterUpdateInterval;

        RecalculateDensityBoost();
    }

    private void LateUpdate()
    {
        if (densityCanvas == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        densityCanvas.rotation =
            Quaternion.LookRotation(
                mainCamera.transform.forward,
                Vector3.up);
    }

    // =========================================================
    // DENSITY BOOST
    // =========================================================

    private void RecalculateDensityBoost()
    {
        float newStrongestBoost = 0f;

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit ship in allShips)
        {
            if (!IsShipInsideField(ship))
                continue;

            CheckDensityBooster(
                ship,
                0,
                ship.normalModule1.Value,
                ref newStrongestBoost);

            CheckDensityBooster(
                ship,
                1,
                ship.normalModule2.Value,
                ref newStrongestBoost);

            CheckDensityBooster(
                ship,
                2,
                ship.normalModule3.Value,
                ref newStrongestBoost);

            CheckDensityBooster(
                ship,
                3,
                ship.classModule.Value,
                ref newStrongestBoost);
        }

        /*
         * DensityBoost nie stackuje siê.
         * Zawsze bierze najwiêksz¹ wartoœæ.
         */
        strongestDensityBoost =
            Mathf.Max(
                0f,
                newStrongestBoost);

        /*
         * Density.Value mog³o siê nie zmieniæ,
         * ale zmieni³ siê booster,
         * wiêc rêcznie odœwie¿amy tekst.
         */
        RefreshText(
            GetEffectiveDensity());
    }

    private void CheckDensityBooster(
        ShipUnit ship,
        int slotIndex,
        FixedString64Bytes moduleId,
        ref float strongestBoost)
    {
        if (ship == null)
            return;

        if (moduleId.IsEmpty)
            return;

        if (ModuleDatabase.Instance == null)
            return;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        if (module == null)
            return;

        if (!module.haveBaseBooster)
            return;

        if (module.baseBoosterEffect !=
            BaseBoosterEffectType.DencityBoost)
        {
            return;
        }

        float classMultiplier =
            ship.GetModuleEffectMultiplier(
                slotIndex,
                module);

        float finalBoost =
            module.baseBoosterValue *
            classMultiplier;

        if (finalBoost >
            strongestBoost)
        {
            strongestBoost =
                finalBoost;
        }
    }

    // =========================================================
    // FIELD CHECK
    // =========================================================

    private bool IsShipInsideField(
    ShipUnit ship)
    {
        if (ship == null)
            return false;

        if (!ship.IsSpawned)
            return false;

        if (ship.isDead.Value)
            return false;

        if (fieldVisual == null)
            return false;

        /*
         * Nie sprawdzamy w³aœciciela.
         *
         * Pole asteroid jest wspólne,
         * wiêc statki wszystkich graczy
         * mog¹ aktywowaæ Density Boost.
         */
        return fieldVisual.ContainsWorldPosition(
            ship.transform.position);
    }

    // =========================================================
    // EFFECTIVE DENSITY
    // =========================================================

    private float GetEffectiveDensity()
    {
        return Mathf.Clamp(
            Density.Value +
            strongestDensityBoost,
            minimumDensity,
            maximumDensity);
    }

    public float GetDensityPercent()
    {
        return GetEffectiveDensity();
    }

    // =========================================================
    // DENSITY CHANGES
    // =========================================================

    public void IncreaseDensity(
        float amount)
    {
        if (!IsServer ||
            amount <= 0f)
        {
            return;
        }

        /*
         * Zwiêkszamy prawdziwe Density,
         * a nie tymczasowy bonus modu³u.
         */
        Density.Value =
            Mathf.Min(
                maximumDensity,
                Density.Value + amount);
    }

    public void RemoveDensity(
        float amount)
    {
        if (!IsServer)
            return;

        amount =
            Mathf.Max(
                0f,
                amount);

        /*
         * Mining zu¿ywa prawdziwe Density.
         *
         * Bonus z modu³u nie jest
         * permanentn¹ czêœci¹ pola.
         */
        Density.Value =
            Mathf.Max(
                minimumDensity,
                Density.Value - amount);
    }

    // =========================================================
    // UI
    // =========================================================

    private void OnDensityChanged(
        float oldValue,
        float newValue)
    {
        RefreshText(
            GetEffectiveDensity());
    }

    private void RefreshText(
        float value)
    {
        if (densityText == null)
            return;

        densityText.text =
            $"{value:0.##}%";

        if (fieldVisual != null)
        {
            densityText.color =
                fieldVisual.borderColor;
        }
    }
}