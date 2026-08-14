using Unity.Netcode;
using UnityEngine;

public class BaseEnergyProduction : NetworkBehaviour
{
    [SerializeField] private int baseEnergyPerSecond = 10;

    private UnitOwner owner;
    private BaseEnergyGenerator[] generators;
    private BaseSafeZone safeZone;

    private float timer;

    public override void OnNetworkSpawn()
    {
        owner = GetComponent<UnitOwner>();
        generators = GetComponents<BaseEnergyGenerator>();
        safeZone = GetComponent<BaseSafeZone>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            timer = 0f;
            AddEnergyToOwner();
        }
    }

    public int GetEnergyPerSecond()
    {
        int flatBonus = 0;
        float percentBonus = 0f;

        foreach (BaseEnergyGenerator generator in generators)
        {
            if (generator == null)
                continue;

            flatBonus +=
                generator.GetFlatBonus();

            percentBonus +=
                generator.GetPercentBonus();
        }

        float generatorResult =
            (baseEnergyPerSecond + flatBonus) *
            (1f + percentBonus);

        float safeZoneMultiplier =
            safeZone != null
                ? safeZone.GetEnergyProductionMultiplier()
                : 1f;

        float finalResult =
            generatorResult *
            safeZoneMultiplier;

        return Mathf.RoundToInt(
            finalResult);
    }

    private void AddEnergyToOwner()
    {
        PlayerResources[] all =
            FindObjectsByType<PlayerResources>(FindObjectsSortMode.None);

        foreach (PlayerResources r in all)
        {
            if (r.OwnerClientId == owner.ownerId.Value)
            {
                r.AddEnergy(GetEnergyPerSecond());
                return;
            }
        }
    }
}