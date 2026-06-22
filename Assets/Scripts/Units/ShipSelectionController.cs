using UnityEngine;
using UnityEngine.InputSystem;

public class ShipSelectionController : MonoBehaviour
{
    [SerializeField] private float shipFlightHeight = 0.5f;

    private ShipUnit selectedShip;

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySelect();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryMove();


    }

    private void TrySelect()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ClearSelection();
            return;
        }

        ShipUnit ship = hit.collider.GetComponent<ShipUnit>();

        if (ship == null || !ship.IsMine())
        {
            ClearSelection();
            return;
        }

        ClearSelection();

        selectedShip = ship;
        selectedShip.SetSelectedLocal(true);
    }

    private void TryMove()
    {
        if (selectedShip == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Vector3 target = hit.point;
        target.y = shipFlightHeight;

        selectedShip.MoveToServerRpc(target);
    }

    private void ClearSelection()
    {
        if (selectedShip != null)
            selectedShip.SetSelectedLocal(false);

        selectedShip = null;
    }
}