using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TopLeftHud : MonoBehaviour
{
    [SerializeField] private TMP_Text metalText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text unitsText;
    [SerializeField] private TMP_Text baseHpText;
    [SerializeField] private TMP_Text timeText;

    private PlayerResources resources;
    private PlayerUnits units;
    private PlayerBase playerBase;

    private float gameTime;

    private void Update()
    {
        if (resources == null || units == null || playerBase == null)
            FindLocalPlayer();

        gameTime += Time.deltaTime;

        if (resources != null)
        {
            metalText.text = "[Metal: " + resources.metal.Value.ToString() +" ]";
            energyText.text = "[Energy: " + resources.energy.Value.ToString() + " ]";
        }

        if (units != null)
        {
            unitsText.text = $"[Units: {units.currentUnits.Value}/{units.maxUnits.Value} ]";
        }

        if (playerBase != null)
        {
            NetworkHealth baseHealth = playerBase.GetBaseHealth();

            if (baseHealth != null)
                baseHpText.text = $"[Base HP: {baseHealth.GetHealth()}/{baseHealth.GetMaxHealth()}]";
            else
                baseHpText.text = "[Base HP: -/- ]";
        }

        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        timeText.text = $"[Time: {minutes:00}:{seconds:00} ]";
    }

    private void FindLocalPlayer()
    {
        PlayerResources[] allResources =
            FindObjectsByType<PlayerResources>(FindObjectsSortMode.None);

        foreach (PlayerResources r in allResources)
        {
            if (r.OwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                resources = r;
                units = r.GetComponent<PlayerUnits>();
                playerBase = r.GetComponent<PlayerBase>();
                return;
            }
        }
    }
}