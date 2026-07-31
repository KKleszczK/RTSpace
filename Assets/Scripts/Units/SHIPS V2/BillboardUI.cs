using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    private void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            cam.transform.forward,
            Vector3.up);
    }
}