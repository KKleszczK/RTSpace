using UnityEngine;

public class CooldownBarUI : MonoBehaviour
{
    [SerializeField] private UnitAttack attack;
    [SerializeField] private RectTransform currentCooldownBar;
    [SerializeField] private float maxBarWidth = 1920f;

    private void Update()
    {
        if (attack == null)
            return;

        Vector2 size = currentCooldownBar.sizeDelta;
        size.x = maxBarWidth * attack.attackProgress.Value;
        currentCooldownBar.sizeDelta = size;
    }
}