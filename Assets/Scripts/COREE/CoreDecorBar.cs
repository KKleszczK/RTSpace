using UnityEngine;

public class CoreDecorBar : MonoBehaviour
{
    [SerializeField] private RectTransform lightBar;
    [SerializeField] private RectTransform darkBar;

    [SerializeField] private float baseHeight = 40f;
    [SerializeField] private float randomRange = 30f;
    [SerializeField] private float maxHeight = 200f;

    [SerializeField] private float changeSpeed = 80f;
    [SerializeField] private float darkExtraHeight = 20f;
    [SerializeField] private float darkFollowSpeed = 40f;

    private float targetLightHeight;
    private float currentLightHeight;
    private float currentDarkHeight;

    private void Start()
    {
        currentLightHeight = baseHeight;
        currentDarkHeight = baseHeight + darkExtraHeight;

        PickNewTarget();
        ApplyHeights();
    }

    private void Update()
    {
        currentLightHeight = Mathf.MoveTowards(
            currentLightHeight,
            targetLightHeight,
            changeSpeed * Time.deltaTime
        );

        float targetDarkHeight = currentLightHeight + darkExtraHeight;

        currentDarkHeight = Mathf.MoveTowards(
            currentDarkHeight,
            targetDarkHeight,
            darkFollowSpeed * Time.deltaTime
        );

        ApplyHeights();

        if (Mathf.Abs(currentLightHeight - targetLightHeight) < 0.5f)
            PickNewTarget();
    }

    private void PickNewTarget()
    {
        targetLightHeight = baseHeight + Random.Range(-randomRange, randomRange);
        targetLightHeight = Mathf.Clamp(targetLightHeight, 0f, maxHeight);
    }

    private void ApplyHeights()
    {
        SetHeight(lightBar, currentLightHeight);
        SetHeight(darkBar, Mathf.Clamp(currentDarkHeight, 0f, maxHeight));
    }

    private void SetHeight(RectTransform rect, float height)
    {
        if (rect == null)
            return;

        Vector2 size = rect.sizeDelta;
        size.y = height;
        rect.sizeDelta = size;
    }
}