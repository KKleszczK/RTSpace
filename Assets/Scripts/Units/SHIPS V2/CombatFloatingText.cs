using TMPro;
using UnityEngine;

public class CombatFloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Header("Animation")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float initialUpSpeed = 2.5f;
    [SerializeField] private float gravity = 4f;
    [SerializeField] private float sideSpeed = 0.5f;

    private float age;

    private Vector3 velocity;

    private Color startColor;

    public void Initialize(
        string value,
        Color color)
    {
        if (text == null)
            return;

        text.text = value;
        text.color = color;

        startColor = color;

        float randomSide =
            Random.Range(
                -sideSpeed,
                sideSpeed);

        velocity =
            new Vector3(
                randomSide,
                initialUpSpeed,
                0f);

        age = 0f;
    }

    private void Update()
    {
        age += Time.deltaTime;

        velocity.y -=
            gravity * Time.deltaTime;

        transform.position +=
            velocity * Time.deltaTime;

        if (Camera.main != null)
        {
            transform.rotation =
                Camera.main.transform.rotation;
        }

        float progress =
            Mathf.Clamp01(
                age / lifetime);

        if (text != null)
        {
            Color color =
                startColor;

            color.a =
                1f - progress;

            text.color =
                color;
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}