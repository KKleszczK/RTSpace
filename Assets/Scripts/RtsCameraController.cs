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

    private void Update()
    {
        UpdateMovement();
        UpdateZoom();
        ClampCameraPosition();
    }

    private void UpdateMovement()
    {
        if (Keyboard.current == null)
            return;

        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            move.z += 1f;

        if (Keyboard.current.sKey.isPressed)
            move.z -= 1f;

        if (Keyboard.current.aKey.isPressed)
            move.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            move.x += 1f;

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
}