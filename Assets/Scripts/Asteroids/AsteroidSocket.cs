using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum AsteroidSocketState
{
    Empty,
    Blocked,
    Mining
}

public enum ShipSocketState
{
    None,
    Blocking,
    Mining
}

public enum ShipModuleSlot
{
    Normal1,
    Normal2,
    Normal3,
    Class
}

public class AsteroidSocket : NetworkBehaviour
{
    private sealed class MiningOperation
    {
        public ShipModuleSlot Slot;
        public FixedString64Bytes ModuleId;

        public float MineInterval;
        public float AmountAtFullDensity;
        public float DensityLossPerHit;

        public bool HasInfiniteDurability;
        public int HitsRemaining;

        public float NextHitTime;
        public bool ModuleConsumed;
    }

    [Header("References")]
    [SerializeField] private SphereCollider socketTrigger;
    [SerializeField] private AsteroidSocketVisual socketVisual;
    [SerializeField] private AsteroidFieldDensity asteroidFieldDensity;

    [Header("Socket")]
    [SerializeField] private float socketRadius = 3f;

    /*
     * Kolejnoœæ statków w sockecie.
     * Pierwszy statek na liœcie jest aktywny.
     */
    private readonly List<ShipUnit> shipsInside = new();

    /*
     * Jeden statek mo¿e mieæ kilka colliderów, np. g³ówny collider
     * oraz Selector. Dziêki temu wyjœcie jednego collidera nie usuwa
     * statku z socketu, jeœli drugi nadal jest w œrodku.
     */
    private readonly Dictionary<ShipUnit, HashSet<Collider>>
        shipCollidersInside = new();

    /*
     * Ka¿dy modu³ górniczy aktywnego statku ma w³asn¹ operacjê.
     * Dziêki temu maksymalnie cztery modu³y mog¹ kopaæ równolegle.
     */
    private readonly List<MiningOperation> miningOperations = new();

    public NetworkVariable<AsteroidSocketState> State = new(
        AsteroidSocketState.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<NetworkObjectReference>
        activeShipReference = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public ShipUnit ActiveShip { get; private set; }

    private ShipUnit miningOperationsShip;

    private void Awake()
    {
        RefreshSocketSize();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshSocketSize();
    }
#endif

    private void Update()
    {
        if (!IsServer)
            return;

        CleanupShips();

        if (ActiveShip == null)
        {
            SelectNextShip();
            return;
        }

        RefreshMiningOperations();
        RefreshActiveShipState();

        if (State.Value == AsteroidSocketState.Mining)
            ProcessMining();
    }

    private void RefreshSocketSize()
    {
        socketRadius = Mathf.Max(0.1f, socketRadius);

        if (socketTrigger != null)
        {
            socketTrigger.isTrigger = true;
            socketTrigger.radius = socketRadius;
        }

        if (socketVisual != null)
            socketVisual.SetRadius(socketRadius);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        ShipUnit ship = other.GetComponentInParent<ShipUnit>();

        if (ship == null)
            return;

        if (!ship.IsSpawned || ship.isDead.Value)
            return;

        if (!shipCollidersInside.TryGetValue(
                ship,
                out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            shipCollidersInside.Add(ship, colliders);
        }

        if (!colliders.Add(other))
            return;

        if (!shipsInside.Contains(ship))
        {
            shipsInside.Add(ship);

            Debug.Log(
                $"[SOCKET] Dodano {ship.name}. " +
                $"Pozycja w kolejce: {shipsInside.Count}",
                ship
            );
        }

        if (ActiveShip == null)
            SelectNextShip();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer)
            return;

        ShipUnit ship = other.GetComponentInParent<ShipUnit>();

        if (ship == null)
            return;

        if (!shipCollidersInside.TryGetValue(
                ship,
                out HashSet<Collider> colliders))
        {
            return;
        }

        colliders.Remove(other);

        if (colliders.Count > 0)
            return;

        shipCollidersInside.Remove(ship);
        shipsInside.Remove(ship);

        Debug.Log(
            $"[SOCKET] Statek {ship.name} ca³kowicie opuœci³ socket.",
            ship
        );

        if (ship != ActiveShip)
            return;

        CancelAllMiningOperations();
        ClearActiveShip();
        SelectNextShip();
    }

    private void SelectNextShip()
    {
        CleanupShips();

        if (shipsInside.Count == 0)
        {
            ClearActiveShip();
            return;
        }

        ActiveShip = shipsInside[0];

        activeShipReference.Value =
            new NetworkObjectReference(ActiveShip.NetworkObject);

        CancelAllMiningOperations();
        RefreshMiningOperations();
        RefreshActiveShipState();

        Debug.Log(
            $"[SOCKET] Aktywny statek: {ActiveShip.name}",
            ActiveShip
        );
    }

    private void RefreshActiveShipState()
    {
        if (ActiveShip == null)
        {
            State.Value = AsteroidSocketState.Empty;
            return;
        }

        if (miningOperations.Count > 0)
        {
            State.Value = AsteroidSocketState.Mining;
            ActiveShip.SocketState.Value = ShipSocketState.Mining;
        }
        else
        {
            State.Value = AsteroidSocketState.Blocked;
            ActiveShip.SocketState.Value = ShipSocketState.Blocking;
        }
    }

    private void RefreshMiningOperations()
    {
        if (ActiveShip == null)
        {
            CancelAllMiningOperations();
            return;
        }

        if (miningOperationsShip != ActiveShip)
        {
            CancelAllMiningOperations();
            miningOperationsShip = ActiveShip;
        }

        /*
         * Operacje permanentne s¹ wa¿ne tylko tak d³ugo,
         * jak ten sam modu³ nadal znajduje siê w tym samym slocie.
         */
        for (int i = miningOperations.Count - 1; i >= 0; i--)
        {
            MiningOperation operation = miningOperations[i];

            if (!operation.HasInfiniteDurability)
                continue;

            FixedString64Bytes currentModuleId =
                GetModuleIdFromSlot(ActiveShip, operation.Slot);

            if (currentModuleId.Equals(operation.ModuleId))
                continue;

            Debug.Log(
                $"[MINING] Zatrzymano permanentny modu³ ze slotu " +
                $"{operation.Slot}, poniewa¿ zosta³ usuniêty lub zmieniony.",
                ActiveShip
            );

            miningOperations.RemoveAt(i);
        }

        TryStartOperationFromSlot(
            ActiveShip,
            ShipModuleSlot.Normal1,
            ActiveShip.normalModule1.Value
        );

        TryStartOperationFromSlot(
            ActiveShip,
            ShipModuleSlot.Normal2,
            ActiveShip.normalModule2.Value
        );

        TryStartOperationFromSlot(
            ActiveShip,
            ShipModuleSlot.Normal3,
            ActiveShip.normalModule3.Value
        );

        TryStartOperationFromSlot(
            ActiveShip,
            ShipModuleSlot.Class,
            ActiveShip.classModule.Value
        );
    }

    private void TryStartOperationFromSlot(
        ShipUnit ship,
        ShipModuleSlot slot,
        FixedString64Bytes moduleId)
    {
        if (ship == null || moduleId.IsEmpty)
            return;

        if (HasOperationForSlot(slot))
            return;

        if (!TryGetMiningModuleFromId(
                moduleId,
                out ModuleDefinition module))
        {
            return;
        }

        int slotIndex =
            slot == ShipModuleSlot.Class
            ? 3
            : (int)slot;

        float moduleMultiplier =
            ship.GetModuleEffectMultiplier(
                slotIndex,
                module);

        MiningOperation operation = new()
        {
            Slot = slot,
            ModuleId = moduleId,

            MineInterval =
        Mathf.Max(0.01f, module.mineInterval),

            AmountAtFullDensity =
        Mathf.Max(0f, module.miningAmountAtFullDensity),

            DensityLossPerHit =
        Mathf.Max(0f, module.densityRemovedPerHit),

            HasInfiniteDurability =
        module.miningModuleDurability <= 0,

            HitsRemaining =
        module.miningModuleDurability <= 0
            ? 0
            : module.miningModuleDurability,

            ModuleConsumed =
        module.miningModuleDurability <= 0
        };

        operation.NextHitTime =
            Time.time + operation.MineInterval;

        miningOperations.Add(operation);

        /*
         * Modu³ zu¿ywalny zostaje usuniêty ze slotu od razu.
         * Operacja ma ju¿ skopiowane jego parametry i dokoñczy sesjê.
         */
        

        Debug.Log(
            $"[MINING] Uruchomiono modu³ ze slotu {slot}. " +
            $"Permanentny: {operation.HasInfiniteDurability}, " +
            $"hity: {operation.HitsRemaining}, " +
            $"interwa³: {operation.MineInterval:0.##} s.",
            ship
        );
    }

    private bool HasOperationForSlot(ShipModuleSlot slot)
    {
        foreach (MiningOperation operation in miningOperations)
        {
            if (operation.Slot == slot)
                return true;
        }

        return false;
    }

    private void ProcessMining()
    {
        if (ActiveShip == null)
            return;

        if (asteroidFieldDensity == null)
        {
            Debug.LogError(
                $"[MINING] Socket {name} nie ma przypisanego " +
                $"{nameof(AsteroidFieldDensity)}.",
                this
            );

            CancelAllMiningOperations();
            RefreshActiveShipState();
            return;
        }

        for (int i = miningOperations.Count - 1; i >= 0; i--)
        {
            MiningOperation operation = miningOperations[i];

            if (Time.time < operation.NextHitTime)
                continue;

            PerformMiningHit(operation);

            if (!operation.HasInfiniteDurability)
            {
                operation.HitsRemaining--;

                if (operation.HitsRemaining <= 0)
                {
                    Debug.Log(
                        $"[MINING] Zakoñczono operacjê modu³u " +
                        $"ze slotu {operation.Slot}.",
                        ActiveShip
                    );

                    miningOperations.RemoveAt(i);
                    continue;
                }
            }

            operation.NextHitTime =
                Time.time + operation.MineInterval;
        }

        /*
         * Po zakoñczeniu operacji zu¿ywalnej w danym slocie
         * mo¿e pojawiæ siê nowy modu³. Zostanie wykryty w kolejnym Update.
         */
        RefreshActiveShipState();
    }

    private void PerformMiningHit(MiningOperation operation)
    {
        if (!operation.HasInfiniteDurability &&
            !operation.ModuleConsumed)
        {
            RemoveModuleFromSlot(
                ActiveShip,
                operation.Slot
            );

            operation.ModuleConsumed = true;
        }

        if (operation == null || ActiveShip == null)
            return;

        float densityPercent =
            asteroidFieldDensity.GetDensityPercent();

        float densityMultiplier =
            Mathf.Clamp01(densityPercent / 100f);

        float minedAmount =
            operation.AmountAtFullDensity *
            densityMultiplier;

        GiveMiningResources(
            ActiveShip,
            minedAmount
        );

        asteroidFieldDensity.RemoveDensity(
            operation.DensityLossPerHit
        );
    }

    private void CancelAllMiningOperations()
    {
        if (miningOperations.Count > 0)
        {
            int unfinishedHits = 0;

            foreach (MiningOperation operation in miningOperations)
            {
                if (!operation.HasInfiniteDurability)
                    unfinishedHits += Mathf.Max(0, operation.HitsRemaining);
            }

            Debug.Log(
                $"[MINING] Przerwano wszystkie operacje. " +
                $"Liczba operacji: {miningOperations.Count}, " +
                $"niewykorzystane hity: {unfinishedHits}.",
                miningOperationsShip
            );
        }

        miningOperations.Clear();
        miningOperationsShip = null;
    }

    private bool TryGetMiningModuleFromId(
        FixedString64Bytes moduleId,
        out ModuleDefinition module)
    {
        module = null;

        if (moduleId.IsEmpty)
            return false;

        if (ModuleDatabase.Instance == null)
            return false;

        module = ModuleDatabase.Instance.GetModule(
            moduleId.ToString()
        );

        return module != null &&
               module.isSocketMiner;
    }

    private FixedString64Bytes GetModuleIdFromSlot(
        ShipUnit ship,
        ShipModuleSlot slot)
    {
        if (ship == null)
            return default;

        return slot switch
        {
            ShipModuleSlot.Normal1 => ship.normalModule1.Value,
            ShipModuleSlot.Normal2 => ship.normalModule2.Value,
            ShipModuleSlot.Normal3 => ship.normalModule3.Value,
            ShipModuleSlot.Class => ship.classModule.Value,
            _ => default
        };
    }

    private void RemoveModuleFromSlot(
        ShipUnit ship,
        ShipModuleSlot slot)
    {
        if (ship == null)
            return;

        switch (slot)
        {
            case ShipModuleSlot.Normal1:
                ship.normalModule1.Value = default;
                break;

            case ShipModuleSlot.Normal2:
                ship.normalModule2.Value = default;
                break;

            case ShipModuleSlot.Normal3:
                ship.normalModule3.Value = default;
                break;

            case ShipModuleSlot.Class:
                ship.classModule.Value = default;
                break;
        }

        Debug.Log(
            $"[MINING] Usuniêto zu¿ywalny modu³ ze slotu {slot} " +
            $"statku {ship.name}.",
            ship
        );
    }

    private void GiveMiningResources(
        ShipUnit ship,
        float amount)
    {
        if (!IsServer)
            return;

        if (ship == null)
            return;

        int metalAmount = Mathf.FloorToInt(amount);

        if (metalAmount <= 0)
            return;

        ulong ownerId = ship.OwnerClientId;

        PlayerResources[] allResources =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None
            );

        foreach (PlayerResources resources in allResources)
        {
            if (resources.OwnerClientId != ownerId)
                continue;

            resources.AddMetal(metalAmount);

            Debug.Log(
                $"[MINING] Dodano {metalAmount} metalu " +
                $"graczowi {ownerId}.",
                ship
            );

            return;
        }

        Debug.LogError(
            $"[MINING] Nie znaleziono PlayerResources " +
            $"dla gracza {ownerId}.",
            ship
        );
    }

    private void ClearActiveShip()
    {
        if (ActiveShip != null &&
            ActiveShip.NetworkObject != null &&
            ActiveShip.NetworkObject.IsSpawned)
        {
            ActiveShip.SocketState.Value =
                ShipSocketState.None;
        }

        ActiveShip = null;
        activeShipReference.Value = default;

        State.Value = AsteroidSocketState.Empty;

        CancelAllMiningOperations();
    }

    private void CleanupShips()
    {
        bool activeShipRemoved = false;

        for (int i = shipsInside.Count - 1; i >= 0; i--)
        {
            ShipUnit ship = shipsInside[i];

            bool invalid =
                ship == null ||
                ship.NetworkObject == null ||
                !ship.NetworkObject.IsSpawned ||
                ship.isDead.Value;

            if (!invalid)
                continue;

            if (ship == ActiveShip)
                activeShipRemoved = true;

            shipCollidersInside.Remove(ship);
            shipsInside.RemoveAt(i);
        }

        if (!activeShipRemoved)
            return;

        CancelAllMiningOperations();
        ClearActiveShip();
        SelectNextShip();
    }

    public bool IsOccupied()
    {
        return ActiveShip != null;
    }

    public bool IsMining()
    {
        return State.Value ==
               AsteroidSocketState.Mining;
    }

    public int GetShipsInsideCount()
    {
        return shipsInside.Count;
    }
}