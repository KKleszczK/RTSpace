using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class RtsCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 20f;

    [Header("Map Bounds")]
    [SerializeField] private float mapSize = 100f;
    [SerializeField] private float movementMargin = 10f;
    [SerializeField] private float topViewMargin = 30f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 30f;

    [Header("Edge Scrolling")]
    [SerializeField] private bool edgeScrollingEnabled = true;
    [SerializeField] private float edgeScrollSize = 15f;

    private void Update()
    {
        UpdateMovement();
        UpdateZoom();
        ClampCameraPosition();
    }

    private void UpdateMovement()
    {
        Vector3 move = Vector3.zero;

        // =====================================================
        // KEYBOARD
        // =====================================================

        if (GameInputManager.Instance != null)
        {
            if (GameInputManager.Instance.CameraUpPressed)
                move.z += 1f;

            if (GameInputManager.Instance.CameraDownPressed)
                move.z -= 1f;

            if (GameInputManager.Instance.CameraLeftPressed)
                move.x -= 1f;

            if (GameInputManager.Instance.CameraRightPressed)
                move.x += 1f;
        }

        // =====================================================
        // EDGE SCROLLING
        // =====================================================

        if (edgeScrollingEnabled &&
            Mouse.current != null)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            // Lewa krawêdŸ
            if (mousePosition.x <= edgeScrollSize)
                move.x -= 1f;

            // Prawa krawêdŸ
            if (mousePosition.x >=
                Screen.width - edgeScrollSize)
            {
                move.x += 1f;
            }

            // Dolna krawêdŸ
            if (mousePosition.y <= edgeScrollSize)
                move.z -= 1f;

            // Górna krawêdŸ
            if (mousePosition.y >=
                Screen.height - edgeScrollSize)
            {
                move.z += 1f;
            }
        }

        // =====================================================
        // MOVEMENT
        // =====================================================

        if (move.sqrMagnitude <= 0f)
            return;

        transform.position +=
            move.normalized *
            moveSpeed *
            Time.deltaTime;
    }

    private void UpdateZoom()
    {
        if (Mouse.current == null)
            return;

        // =====================================================
        // BLOCK ZOOM OVER UI
        // =====================================================

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // =====================================================
        // SCROLL
        // =====================================================

        float scroll =
            Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) <= 0.01f)
            return;

        float scrollDirection =
            Mathf.Sign(scroll);

        /*
         * Zoom po osi kamery:
         * scroll w górê = do przodu i w dó³,
         * scroll w dó³ = do ty³u i w górê.
         */
        Vector3 zoomMovement =
            transform.forward *
            scrollDirection *
            zoomSpeed;

        Vector3 newPosition =
            transform.position +
            zoomMovement;

        /*
         * Nie pozwalamy przekroczyæ
         * minimalnej/maksymalnej wysokoœci.
         */
        if (newPosition.y < minHeight ||
            newPosition.y > maxHeight)
        {
            return;
        }

        transform.position =
            newPosition;
    }

    private void ClampCameraPosition()
    {
        float halfMapSize =
            mapSize * 0.5f;

        float minX =
            -halfMapSize - movementMargin;

        float maxX =
            halfMapSize + movementMargin;

        float minZ =
            -halfMapSize - movementMargin;

        float maxZ =
            halfMapSize - topViewMargin;

        Vector3 position =
            transform.position;

        position.x =
            Mathf.Clamp(
                position.x,
                minX,
                maxX);

        position.z =
            Mathf.Clamp(
                position.z,
                minZ,
                maxZ);

        position.y =
            Mathf.Clamp(
                position.y,
                minHeight,
                maxHeight);

        transform.position =
            position;
    }

    public void MoveViewToWorldPosition(Vector3 worldPosition)
    {
        // P³aszczyzna mapy.
        Plane mapPlane =
            new Plane(
                Vector3.up,
                Vector3.zero);

        // Promieñ przez œrodek ekranu.
        Camera cameraComponent =
            GetComponent<Camera>();

        if (cameraComponent == null)
            return;

        Ray centerRay =
            cameraComponent.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f));

        if (!mapPlane.Raycast(
                centerRay,
                out float distance))
        {
            return;
        }

        // Punkt mapy, na który kamera patrzy TERAZ.
        Vector3 currentViewCenter =
            centerRay.GetPoint(distance);

        // O ile musimy przesun¹æ kamerê,
        // aby œrodek widoku znalaz³ siê
        // w klikniêtym miejscu.
        Vector3 offset =
            worldPosition -
            currentViewCenter;

        // Kamera porusza siê tylko po X/Z.
        offset.y = 0f;

        transform.position += offset;

        ClampCameraPosition();
    }

    public bool TryGetViewCornersOnMap(
    out Vector3[] corners)
    {
        corners =
            new Vector3[4];

        Camera cam =
            GetComponent<Camera>();

        if (cam == null)
            return false;

        Plane mapPlane =
            new Plane(
                Vector3.up,
                Vector3.zero);

        Vector2[] viewportCorners =
        {
        new Vector2(0f, 0f), // bottom left
        new Vector2(1f, 0f), // bottom right
        new Vector2(1f, 1f), // top right
        new Vector2(0f, 1f)  // top left
    };

        for (int i = 0; i < 4; i++)
        {
            Ray ray =
                cam.ViewportPointToRay(
                    new Vector3(
                        viewportCorners[i].x,
                        viewportCorners[i].y,
                        0f));

            if (!mapPlane.Raycast(
                    ray,
                    out float distance))
            {
                return false;
            }

            corners[i] =
                ray.GetPoint(distance);
        }

        return true;
    }
}