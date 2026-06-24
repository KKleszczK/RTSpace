using UnityEngine;

public class SelectionTarget : MonoBehaviour
{
    [SerializeField] private GameObject selectionMarker;

    private void Awake()
    {
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionMarker != null)
            selectionMarker.SetActive(selected);
    }
}