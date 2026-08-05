using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CorePanelUI : MonoBehaviour
{
    [SerializeField] private Button constructButton;
    [SerializeField] private TMP_Text constructButtonText;
    [SerializeField] private TMP_Text constructButtonTextMetal;
    [SerializeField] private TMP_Text constructButtonTextEnergy;
    [SerializeField] private TMP_Text constructButtonTextTime;
    [SerializeField] private Image iconM;
    [SerializeField] private Image iconE;
    [SerializeField] private TMP_Text iconT;
    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float maxProgressWidth = 500f;

    private BaseCore selectedCore;

    public void SetCore(BaseCore core)
    {
        selectedCore = core;
    }

    private void Start()
    {
        constructButton.onClick.AddListener(() =>
        {
            if (selectedCore != null)
                selectedCore.RequestUpgrade();
        });
    }

    private void Update()
    {
        if (selectedCore == null)
            return;

        int tier = selectedCore.tier.Value;

        if (tier < 3)
        {
            constructButtonText.text =
                $"CONSTRUCT T{tier + 1}";

            constructButtonTextMetal.text =
                selectedCore.GetNextUpgradeMetalCost().ToString();

            constructButtonTextEnergy.text =
                selectedCore.GetNextUpgradeEnergyCost().ToString();

            constructButtonTextTime.text =
                $"{selectedCore.GetNextUpgradeTime():0.#}";
        }
        else
        {
            constructButtonText.text = "CORE MAX";

            constructButtonTextMetal.gameObject.SetActive(false);
            constructButtonTextEnergy.gameObject.SetActive(false);
            constructButtonTextTime.gameObject.SetActive(false);

            iconM.gameObject.SetActive(false);
            iconE.gameObject.SetActive(false);
            iconT.gameObject.SetActive(false);
        }


        constructButton.interactable =
            tier < 3 && !selectedCore.isUpgrading.Value;

        Vector2 size = progressBar.sizeDelta;
        size.x = maxProgressWidth * selectedCore.progress.Value;
        progressBar.sizeDelta = size;
    }
}