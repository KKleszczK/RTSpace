using TMPro;
using UnityEngine;

public class CoreEnergyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text energyPerSecondText;
    [SerializeField] private RectTransform rotatingImage;

    private BaseEnergyProduction energyProduction;

    public void SetEnergyProduction(BaseEnergyProduction production)
    {
        energyProduction = production;
    }

    private void Update()
    {
        if (rotatingImage != null)
            rotatingImage.Rotate(0f, 0f, -360f * Time.deltaTime);

        if (energyProduction == null)
        {
            energyPerSecondText.text = "-/s";
            return;
        }

        energyPerSecondText.text = energyProduction.GetEnergyPerSecond() + "/s";
    }
}