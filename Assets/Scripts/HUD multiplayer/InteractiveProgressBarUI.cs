using TMPro;
using UnityEngine;

public class InteractiveProgressBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform fillMask;
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private RectTransform bonusArea;
    [SerializeField] private TMP_Text bonusText;

    [Header("Settings")]
    [SerializeField, Range(0f, 99f)]
    private float maxBonusPercent = 90f;

    [SerializeField, Range(0f, 100f)]
    private float showBonusTextFromPercent = 10f;


    private float currentBonusPercent;

    // Pozycja fizyczna paska 0..1
    private float visualProgress;

    // Punkt, od którego liczymy aktualny odcinek
    private float anchorGameProgress;
    private float anchorVisualProgress;

    private float lastGameProgress;
    private bool initialized;


    private void Awake()
    {
        ResetBar();
    }


    public void SetBonus(float value)
    {
        float newBonus =
            Mathf.Clamp(
                value,
                0f,
                maxBonusPercent);

        if (!Mathf.Approximately(
                newBonus,
                currentBonusPercent))
        {
            // Bonus siê zmieni³.
            // Aktualna pozycja paska staje siê
            // pocz¹tkiem nowego odcinka.
            anchorGameProgress =
                lastGameProgress;

            anchorVisualProgress =
                visualProgress;

            currentBonusPercent =
                newBonus;
        }

        RefreshBonusArea();
    }


    public void SetProgress(float value)
    {
        float gameProgress =
            Mathf.Clamp01(value);


        // =====================================================
        // NOWA PRODUKCJA / RESET
        // =====================================================

        if (!initialized ||
            gameProgress < lastGameProgress)
        {
            visualProgress = 0f;

            anchorGameProgress = 0f;
            anchorVisualProgress = 0f;

            initialized = true;
        }


        // =====================================================
        // POZYCJA KOÑCOWA
        // =====================================================

        float finishPosition =
            1f -
            currentBonusPercent / 100f;


        // Granica bonusu nie mo¿e znaleŸæ siê
        // za ju¿ przejechanym paskiem.
        finishPosition =
            Mathf.Max(
                finishPosition,
                anchorVisualProgress);


        // =====================================================
        // PROGRESS OD OSTATNIEJ ZMIANY BONUSU
        // =====================================================

        float remainingGameProgress =
            1f - anchorGameProgress;

        if (remainingGameProgress > 0.0001f)
        {
            float t =
                (gameProgress -
                 anchorGameProgress) /
                remainingGameProgress;

            t = Mathf.Clamp01(t);

            visualProgress =
                Mathf.Lerp(
                    anchorVisualProgress,
                    finishPosition,
                    t);
        }
        else
        {
            visualProgress =
                finishPosition;
        }


        lastGameProgress =
            gameProgress;

        RefreshProgressFill();
    }


    private void RefreshProgressFill()
    {
        if (progressFill == null ||
            fillMask == null)
        {
            return;
        }

        float width =
            fillMask.rect.width;

        Vector2 size =
            progressFill.sizeDelta;

        size.x =
            width *
            visualProgress;

        progressFill.sizeDelta =
            size;
    }


    private void RefreshBonusArea()
    {
        if (bonusArea == null ||
            fillMask == null)
        {
            return;
        }

        float width =
            fillMask.rect.width;

        float bonusWidth =
            width *
            (currentBonusPercent / 100f);

        Vector2 size =
            bonusArea.sizeDelta;

        size.x =
            bonusWidth;

        bonusArea.sizeDelta =
            size;


        if (bonusText != null)
        {
            bool show =
                currentBonusPercent >=
                showBonusTextFromPercent;

            bonusText.gameObject.SetActive(
                show);

            if (show)
            {
                bonusText.text =
                    $"{currentBonusPercent:0.#}%";
            }
        }
    }


    private void ResetBar()
    {
        currentBonusPercent = 0f;

        visualProgress = 0f;

        anchorGameProgress = 0f;
        anchorVisualProgress = 0f;

        lastGameProgress = 0f;

        initialized = false;

        RefreshProgressFill();
        RefreshBonusArea();
    }
}