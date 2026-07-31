using TMPro;
using UnityEngine;

public class ShipWorldUI : MonoBehaviour
{
    [Header("Ship")]
    [SerializeField] private ShipUnit ship;

    [Header("Texts")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text shieldText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text socketStateText;

    private Camera mainCamera;

    private void Awake()
    {
        if (ship == null)
            ship = GetComponentInParent<ShipUnit>();

        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (ship == null)
            ship = GetComponentInParent<ShipUnit>();

        if (ship == null)
            return;

        ship.SocketState.OnValueChanged += OnSocketStateChanged;

        // Ustawienie pocz¹tkowej wartoœci.
        UpdateSocketStateText(ship.SocketState.Value);
    }

    private void OnDisable()
    {
        if (ship != null)
            ship.SocketState.OnValueChanged -= OnSocketStateChanged;
    }

    private void LateUpdate()
    {
        if (ship == null)
            return;

        UpdateBasicTexts();
        FaceCamera();
    }

    private void UpdateBasicTexts()
    {
        if (hpText != null)
        {
            hpText.text =
                $"HP: {ship.hp.Value}/{ship.MaxHp}";
        }

        if (shieldText != null)
        {
            shieldText.text =
                $"SHIELD: {ship.shield.Value}/{ship.MaxShield}";
        }

        if (speedText != null)
        {
            speedText.text =
                $"SPEED: {ship.MoveSpeed:0.0}";
        }
    }

    private void OnSocketStateChanged(
        ShipSocketState previousState,
        ShipSocketState newState)
    {
        Debug.Log(
            $"[SHIP UI] {ship.name}: {previousState} -> {newState}",
            ship);

        UpdateSocketStateText(newState);
    }

    private void UpdateSocketStateText(ShipSocketState state)
    {
        if (socketStateText == null)
            return;

        switch (state)
        {
            case ShipSocketState.Mining:
                socketStateText.text = "MINING STATE: MINING";
                socketStateText.gameObject.SetActive(true);
                break;

            case ShipSocketState.Blocking:
                socketStateText.text = "MINING STATE: BLOCKING";
                socketStateText.gameObject.SetActive(true);
                break;

            default:
                socketStateText.text = "MINING STATE: NONE";
                socketStateText.gameObject.SetActive(true);
                break;
        }
    }

    private void FaceCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            mainCamera.transform.forward,
            Vector3.up
        );
    }
}