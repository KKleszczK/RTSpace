using UnityEngine;

public class ResearchTierColors : MonoBehaviour
{
    public static ResearchTierColors Instance
    {
        get;
        private set;
    }


    // =========================================================
    // AVAILABLE
    // =========================================================

    [Header("Available Research Colors")]

    [SerializeField]
    private Color tier1Color =
        new Color(
            36f / 255f,
            149f / 255f,
            40f / 255f,
            1f);

    [SerializeField]
    private Color tier2Color =
        new Color(
            0f / 255f,
            115f / 255f,
            223f / 255f,
            1f);

    [SerializeField]
    private Color tier3Color =
        new Color(
            223f / 255f,
            22f / 255f,
            46f / 255f,
            1f);


    // =========================================================
    // LOCKED
    // =========================================================

    [Header("Locked Research Colors")]

    [SerializeField]
    private Color tier1LockedColor =
        Color.gray;

    [SerializeField]
    private Color tier2LockedColor =
        Color.gray;

    [SerializeField]
    private Color tier3LockedColor =
        Color.gray;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Instance =
            this;
    }


    // =========================================================
    // COLOR
    // =========================================================

    public Color GetColor(
        ResearchTier tier,
        bool locked)
    {
        if (locked)
        {
            return tier switch
            {
                ResearchTier.Tier1 =>
                    tier1LockedColor,

                ResearchTier.Tier2 =>
                    tier2LockedColor,

                ResearchTier.Tier3 =>
                    tier3LockedColor,

                _ =>
                    Color.gray
            };
        }

        return tier switch
        {
            ResearchTier.Tier1 =>
                tier1Color,

            ResearchTier.Tier2 =>
                tier2Color,

            ResearchTier.Tier3 =>
                tier3Color,

            _ =>
                Color.white
        };
    }
}