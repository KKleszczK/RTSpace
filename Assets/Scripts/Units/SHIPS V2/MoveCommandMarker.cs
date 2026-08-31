using UnityEngine;

public class MoveCommandMarker : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private float lifetime = 0.8f;

    [SerializeField]
    private float pulseSpeed = 6f;

    [SerializeField]
    private float minScale = 0.7f;

    [SerializeField]
    private float maxScale = 1.2f;

    private Vector3 baseScale;
    private float spawnTime;


    private void Awake()
    {
        baseScale =
            transform.localScale;

        spawnTime =
            Time.time;
    }


    private void Update()
    {
        float pulse =
            (Mathf.Sin(
                Time.time * pulseSpeed) + 1f)
            * 0.5f;

        float scale =
            Mathf.Lerp(
                minScale,
                maxScale,
                pulse);

        transform.localScale =
            baseScale * scale;

        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}