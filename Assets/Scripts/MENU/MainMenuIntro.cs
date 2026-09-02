using System.Collections;
using UnityEngine;

public class MainMenuIntro : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private CanvasGroup blackFadeGroup;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup buttonsGroup;

    // =========================================================
    // TIMING
    // =========================================================

    [Header("Black Screen")]
    [SerializeField] private float blackHoldTime = 0.5f;
    [SerializeField] private float backgroundFadeTime = 1.5f;

    [Header("Title")]
    [SerializeField] private float titleDelay = 0.3f;
    [SerializeField] private float titleFadeTime = 1f;

    [Header("Buttons")]
    [SerializeField] private float buttonsDelay = 0.3f;
    [SerializeField] private float buttonsFadeTime = 1f;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        StartCoroutine(
            PlayIntro());
    }

    // =========================================================
    // INTRO
    // =========================================================

    private IEnumerator PlayIntro()
    {
        // =====================================================
        // INITIAL STATE
        // =====================================================

        blackFadeGroup.alpha = 1f;
        blackFadeGroup.interactable = true;
        blackFadeGroup.blocksRaycasts = true;

        titleGroup.alpha = 0f;

        buttonsGroup.alpha = 0f;
        buttonsGroup.interactable = false;
        buttonsGroup.blocksRaycasts = false;

        // =====================================================
        // 1. BLACK SCREEN
        // =====================================================

        yield return new WaitForSeconds(
            blackHoldTime);

        // =====================================================
        // 2. REVEAL BACKGROUND
        // =====================================================

        float time = 0f;

        while (time < backgroundFadeTime)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time /
                    backgroundFadeTime);

            blackFadeGroup.alpha =
                1f - progress;

            yield return null;
        }

        blackFadeGroup.alpha = 0f;
        blackFadeGroup.interactable = false;
        blackFadeGroup.blocksRaycasts = false;

        // =====================================================
        // 3. TITLE DELAY
        // =====================================================

        yield return new WaitForSeconds(
            titleDelay);

        // =====================================================
        // 4. TITLE FADE IN
        // =====================================================

        time = 0f;

        while (time < titleFadeTime)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time /
                    titleFadeTime);

            titleGroup.alpha =
                progress;

            yield return null;
        }

        titleGroup.alpha = 1f;

        // =====================================================
        // 5. BUTTONS DELAY
        // =====================================================

        yield return new WaitForSeconds(
            buttonsDelay);

        // =====================================================
        // 6. BUTTONS FADE IN
        // =====================================================

        buttonsGroup.interactable = true;
        buttonsGroup.blocksRaycasts = true;

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

    }
}