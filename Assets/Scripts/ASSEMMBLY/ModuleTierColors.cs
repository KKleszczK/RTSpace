using UnityEngine;

public class ModuleTierColors : MonoBehaviour
{
    public static ModuleTierColors Instance { get; private set; }

    [Header("Tier 1")]
    [SerializeField] private Color tier1Available = Color.white;
    [SerializeField] private Color tier1Locked = Color.gray;

    [Header("Tier 2")]
    [SerializeField] private Color tier2Available = Color.white;
    [SerializeField] private Color tier2Locked = Color.gray;

    [Header("Tier 3")]
    [SerializeField] private Color tier3Available = Color.white;
    [SerializeField] private Color tier3Locked = Color.gray;

    private void Awake()
    {
        Instance = this;
    }

    public Color GetColor(
        ModuleTier tier,
        bool locked)
    {
        return tier switch
        {
            ModuleTier.Tier1 =>
                locked ? tier1Locked : tier1Available,

            ModuleTier.Tier2 =>
                locked ? tier2Locked : tier2Available,

            ModuleTier.Tier3 =>
                locked ? tier3Locked : tier3Available,

            _ => Color.white
        };
    }
}