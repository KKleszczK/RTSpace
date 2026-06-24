using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerColor : NetworkBehaviour
{

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
}