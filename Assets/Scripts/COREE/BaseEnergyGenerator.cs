using Unity.Netcode;
using UnityEngine;

public class BaseEnergyGenerator : NetworkBehaviour
{
    [Header("Generator")]
    [SerializeField] private int generatorIndex = 1;
    [SerializeField] private int requiredCoreTier = 1;

    [Header("Charge")]
    [SerializeField] private float fullChargeTime = 60f;

    [Header("Bonus")]
    [SerializeField] private bool givesFlatBonus = false;
    [SerializeField] private int maxFlatBonus = 0;
    [SerializeField] private float maxPercentBonus = 0.5f;

    public NetworkVariable<float> chargeProgress = new(0f);

    private BaseCore core;

    public override void OnNetworkSpawn()
    {
        core = GetComponent<BaseCore>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (!IsUnlocked())
            return;

        if (chargeProgress.Value >= 1f)
            return;

        chargeProgress.Value += Time.deltaTime / fullChargeTime;

        if (chargeProgress.Value > 1f)
            chargeProgress.Value = 1f;
    }

    public bool IsUnlocked()
    {
        if (core == null)
            core = GetComponent<BaseCore>();

        if (core == null)
            return false;

        return core.tier.Value >= requiredCoreTier;
    }

    public int GetGeneratorIndex()
    {
        return generatorIndex;
    }

    public int GetActiveSegments()
    {
        return Mathf.FloorToInt(chargeProgress.Value * 5f);
    }

    public float GetPercentBonus()
    {
        if (!IsUnlocked() || givesFlatBonus)
            return 0f;

        float segmentProgress = GetActiveSegments() / 5f;
        return maxPercentBonus * segmentProgress;
    }

    public int GetFlatBonus()
    {
        if (!IsUnlocked() || !givesFlatBonus)
            return 0;

        float segmentProgress = GetActiveSegments() / 5f;
        return Mathf.RoundToInt(maxFlatBonus * segmentProgress);
    }
}