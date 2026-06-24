using UnityEngine;
using UnityEngine.InputSystem;

public class RtsCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float zoomSpeed = 5f;

    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 30f;

    private void Update()
    {
        Vector3 move = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) move.z += 1f;
        if (Keyboard.current.sKey.isPressed) move.z -= 1f;
        if (Keyboard.current.aKey.isPressed) move.x -= 1f;
        if (Keyboard.current.dKey.isPressed) move.x += 1f;

        transform.position += move.normalized * moveSpeed * Time.deltaTime;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0)
        {
            Vector3 pos = transform.position;
            pos.y -= Mathf.Sign(scroll) * zoomSpeed;
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            transform.position = pos;
        }
    }
}