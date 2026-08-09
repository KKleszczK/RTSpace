using UnityEngine;

public class AoeImpactVisual : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f;

    public void Initialize(float radius)
    {
        float diameter = radius * 2f;

        transform.localScale =
            new Vector3(
                diameter,
                0.02f,
                diameter);

        Destroy(
            gameObject,
            lifetime);
    }
}