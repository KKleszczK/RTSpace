using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private NetworkHealth health;
    [SerializeField] private RectTransform currentHpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private float maxBarWidth = 1080f;

    private void Update()
    {
        if (health == null)
            return;

        float percent = (float)health.GetHealth() / health.GetMaxHealth();

        Vector2 size = currentHpBar.sizeDelta;
        size.x = maxBarWidth * percent;
        currentHpBar.sizeDelta = size;

        hpText.text = health.GetHealth().ToString();
    }
}