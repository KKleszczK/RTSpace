using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationPopupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 5f;

    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(
        string itemName,
        string tierText,
        Sprite icon,
        Color iconColor)
    {
        if (messageText != null)
        {
            messageText.text =
                itemName +
                "\nTIER - " +
                tierText;
        }

        if (iconImage != null)
        {
            iconImage.sprite =
                icon;

            iconImage.color =
                iconColor;

            iconImage.enabled =
                icon != null;
        }

        StartCoroutine(
            LifetimeRoutine());
    }

    // =========================================================
    // LIFETIME
    // =========================================================

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(
            lifetime);

        Destroy(gameObject);
    }
}