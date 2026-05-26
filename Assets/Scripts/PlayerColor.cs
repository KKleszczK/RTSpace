using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerColor : NetworkBehaviour
{
    public float moveSpeed = 5f;

    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();

        if (OwnerClientId == 0)
        {
            rend.material.color = Color.blue;
        }
        else
        {
            rend.material.color = Color.red;
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        Vector3 move = Vector3.zero;

        if (Keyboard.current.aKey.isPressed) move.x -= 1f;
        if (Keyboard.current.dKey.isPressed) move.x += 1f;
        if (Keyboard.current.wKey.isPressed) move.z += 1f;
        if (Keyboard.current.sKey.isPressed) move.z -= 1f;

        transform.position += move.normalized * moveSpeed * Time.deltaTime;
    }
}