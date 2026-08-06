using UnityEngine;

public class SpaceBackgroundParallax : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Follow Offset")]
    [SerializeField]
    private Vector3 followOffset =
        new Vector3(0f, -20f, 0f);

    [Header("Camera Bounds")]
    [SerializeField] private float mapSize = 100f;
    [SerializeField] private float cameraMovementMargin = 10f;

    [Header("Parallax")]
    [SerializeField] private float maxParallaxX = 3f;
    [SerializeField] private float maxParallaxZ = 3f;

    [SerializeField] private bool invertX = true;
    [SerializeField] private bool invertZ = true;

    private void Awake()
    {
        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        float halfMovementArea =
            mapSize * 0.5f +
            cameraMovementMargin;

        float normalizedX =
            Mathf.Clamp(
                cameraTransform.position.x /
                halfMovementArea,
                -1f,
                1f);

        float normalizedZ =
            Mathf.Clamp(
                cameraTransform.position.z /
                halfMovementArea,
                -1f,
                1f);

        if (invertX)
            normalizedX *= -1f;

        if (invertZ)
            normalizedZ *= -1f;

        Vector3 parallaxOffset =
            new Vector3(
                normalizedX * maxParallaxX,
                0f,
                normalizedZ * maxParallaxZ
            );

        transform.position =
            cameraTransform.position +
            followOffset +
            parallaxOffset;
    }
}