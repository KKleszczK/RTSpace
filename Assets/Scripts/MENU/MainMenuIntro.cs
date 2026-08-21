using System.Collections;
using UnityEngine;

public class MainMenuIntro : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private CanvasGroup blackFadeGroup;

    [Header("Timing")]
    [SerializeField] private float blackHoldTime = 0.5f;
    [SerializeField] private float backgroundFadeTime = 1.5f;
    [SerializeField] private float buttonsDelay = 0.3f;
    [SerializeField] private float buttonsFadeTime = 1f;

    private void Start()
    {
        StartCoroutine(
            PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        // =====================================================
        // INITIAL STATE
        // =====================================================

        blackFadeGroup.alpha = 1f;

        backgroundGroup.alpha = 0f;

        buttonsGroup.alpha = 0f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        // =====================================================
        // BLACK SCREEN
        // =====================================================

        yield return new WaitForSeconds(
            blackHoldTime);

        // =====================================================
        // BACKGROUND APPEARS
        // =====================================================

        float time = 0f;

        while (time < backgroundFadeTime)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time /
                    backgroundFadeTime);

            backgroundGroup.alpha =
                progress;

            blackFadeGroup.alpha =
                1f - progress;

            yield return null;
        }

        backgroundGroup.alpha = 1f;
        blackFadeGroup.alpha = 0f;

        // =====================================================
        // DELAY BEFORE BUTTONS
        // =====================================================

        yield return new WaitForSeconds(
            buttonsDelay);

        // =====================================================
        // BUTTONS APPEAR
        // =====================================================

        time = 0f;

        while (time < buttonsFadeTime)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time /
                    buttonsFadeTime);

            buttonsGroup.alpha =
                progress;

            yield return null;
        }

        buttonsGroup.alpha = 1f;

        // Dopiero teraz menu staje siê klikalne.
        buttonsGroup.interactable = true;
        buttonsGroup.blocksRaycasts = true;
    }
}