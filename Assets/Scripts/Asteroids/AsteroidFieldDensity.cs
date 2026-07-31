using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AsteroidFieldDensity : NetworkBehaviour
{
    [Header("Density")]
    [SerializeField] private float startingDensity = 100f;
    [SerializeField] private float minimumDensity = 20f;
    [SerializeField] private float maximumDensity = 200f;

    [Header("UI")]
    [SerializeField] private TMP_Text densityText;
    [SerializeField] private Transform densityCanvas;

    [Header("Visual")]
    [SerializeField] private AsteroidFieldVisual fieldVisual;

    public NetworkVariable<float> Density = new(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentDensity => Density.Value;
    public float MinimumDensity => minimumDensity;
    public float MaximumDensity => maximumDensity;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        Density.OnValueChanged += OnDensityChanged;

        if (IsServer)
        {
            Density.Value = Mathf.Clamp(
                startingDensity,
                minimumDensity,
                maximumDensity
            );
        }

        RefreshText(Density.Value);
    }

    public override void OnNetworkDespawn()
    {
        Density.OnValueChanged -= OnDensityChanged;
    }

    private void LateUpdate()
    {
        if (densityCanvas == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        densityCanvas.rotation = Quaternion.LookRotation(
            mainCamera.transform.forward,
            Vector3.up
        );
    }

    private void OnDensityChanged(float oldValue, float newValue)
    {
        RefreshText(newValue);
    }

    private void RefreshText(float value)
    {
        if (densityText == null)
            return;

        densityText.text = $"{value:0.##}%";

        if (fieldVisual != null)
            densityText.color = fieldVisual.borderColor;
    }

    public void IncreaseDensity(float amount)
    {
        if (!IsServer || amount <= 0f)
            return;

        Density.Value = Mathf.Min(
            maximumDensity,
            Density.Value + amount
        );
    }

    public float GetDensityPercent()
    {
        return Density.Value;
    }

    public void RemoveDensity(float amount)
    {
        if (!IsServer)
            return;

        amount = Mathf.Max(0f, amount);

        Density.Value = Mathf.Max(
            20f,
            Density.Value - amount
        );
    }
}