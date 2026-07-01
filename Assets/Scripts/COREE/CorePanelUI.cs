using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CorePanelUI : MonoBehaviour
{
    [SerializeField] private Button constructButton;
    [SerializeField] private TMP_Text constructButtonText;
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

        if (tier == 1)
            constructButtonText.text = "CONSTRUCT T2\nM: 100 E: 200 T: 10s";
        else if (tier == 2)
            constructButtonText.text = "CONSTRUCT T3\nM: 200 E: 500 T: 30s";
        else
            constructButtonText.text = "CORE MAX";

        constructButton.interactable =
            tier < 3 && !selectedCore.isUpgrading.Value;

        Vector2 size = progressBar.sizeDelta;
        size.x = maxProgressWidth * selectedCore.progress.Value;
        progressBar.sizeDelta = size;
    }
}