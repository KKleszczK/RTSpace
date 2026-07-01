using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoreGeneratorUI : MonoBehaviour
{
    private BaseEnergyGenerator generator;

    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text bonusText;

    [SerializeField] private Image[] segmentImages;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private Color activeColor;

    private void Update()
    {
        if (generator == null)
            return;

        float progress = generator.chargeProgress.Value;
        int percent = Mathf.FloorToInt(progress * 100f);

        progressText.text = percent + "%";

        float bonus = generator.GetPercentBonus();
        int bonusPercent = Mathf.RoundToInt(bonus * 100f);

        bonusText.text = "+" + bonusPercent + "%";

        int activeSegments = generator.GetActiveSegments();

        for (int i = 0; i < segmentImages.Length; i++)
        {
            segmentImages[i].color = i < activeSegments
                ? activeColor
                : inactiveColor;
        }
    }

    public void SetGenerator(BaseEnergyGenerator newGenerator)
    {
        generator = newGenerator;
    }
}