using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShipUnit : NetworkBehaviour
{
    [SerializeField] private RectTransform currentHpBar;
    [SerializeField] private TMP_Text HP_texxxt;
    [SerializeField] private float maxBarWidth = 1080f;


    public float moveSpeed = 5f;

    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );



    public NetworkVariable<int> hp = new NetworkVariable<int>(
    100,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    [SerializeField] private int maxHp = 100;

    private NetworkVariable<Vector3> targetPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    //attack
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private int attackDamage = 10;

    [SerializeField] private RectTransform attackCooldownBar;
    [SerializeField] private float maxAttackBarWidth = 1920f;

    [SerializeField] private float attackCooldown = 2f;

    private NetworkVariable<float> attackProgress = new(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool CanAttack => attackProgress.Value >= 1f;

    public NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    



    private Renderer rend;


    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        hp.Value -= damage;

        if (hp.Value < 0)
            hp.Value = 0;
    }



    public override void OnNetworkSpawn()
    {
        rend = GetComponent<Renderer>();

        ownerId.OnValueChanged += OnOwnerChanged;

        ApplyColor();
        if (IsServer)
        {
            targetPosition.Value = transform.position;
        }
        SetSelectedLocal(false);

        hp.OnValueChanged += OnHpChanged;
        UpdateHpBar();
    }

    public override void OnNetworkDespawn()
    {
        ownerId.OnValueChanged -= OnOwnerChanged;
        hp.OnValueChanged -= OnHpChanged;
    }

    private void Update()
    {
        if (IsServer)
        {
            if (attackProgress.Value < 1f)
            {
                attackProgress.Value += Time.deltaTime / attackCooldown;

                if (attackProgress.Value > 1f)
                    attackProgress.Value = 1f;
            }
            else
            {
                ShipUnit target = FindEnemyInRange();

                if (target != null)
                {
                    target.TakeDamage(attackDamage);

                    attackProgress.Value = 0f;
                }
            }
        }

        UpdateAttackBar();

        if (!IsServer)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition.Value,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnOwnerChanged(ulong oldValue, ulong newValue)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (ownerId.Value == 0)
            rend.material.color = Color.blue;
        else
            rend.material.color = Color.red;
    }

    public bool IsMine()
    {
        return ownerId.Value == NetworkManager.Singleton.LocalClientId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetOwnerServerRpc(ulong newOwnerId)
    {
        ownerId.Value = newOwnerId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void MoveToServerRpc(Vector3 pos, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId != ownerId.Value)
            return;

        targetPosition.Value = pos;
    }


    [SerializeField] private GameObject selectionMarker;

    public void SetSelectedLocal(bool selected)
    {
        if (selectionMarker != null)
            selectionMarker.SetActive(selected);
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        UpdateHpBar();
    }

    private void UpdateHpBar()
    {
        float percent = (float)hp.Value / maxHp;

        Vector2 size = currentHpBar.sizeDelta;
        size.x = maxBarWidth * percent;
        currentHpBar.sizeDelta = size;
        HP_texxxt.text = hp.Value.ToString();
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer)
            return;

        hp.Value -= damage;

        if (hp.Value <= 0 && !isDead.Value)
        {
            hp.Value = 0;
            isDead.Value = true;
            SetSelectedLocal(false);

            NetworkObject.Despawn(true);
        }
    }


    private void UpdateAttackBar()
    {
        if (attackCooldownBar == null)
            return;

        Vector2 size = attackCooldownBar.sizeDelta;
        size.x = maxAttackBarWidth * attackProgress.Value;
        attackCooldownBar.sizeDelta = size;
    }

    private ShipUnit FindEnemyInRange()
    {
        ShipUnit[] ships = FindObjectsByType<ShipUnit>(FindObjectsSortMode.None);

        ShipUnit closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (ShipUnit ship in ships)
        {
            if (ship == this)
                continue;

            if (ship.isDead.Value)
                continue;

            if (ship.ownerId.Value == ownerId.Value)
                continue;

            if (ship.hp.Value <= 0)
                continue;

            float distance = Vector3.Distance(transform.position, ship.transform.position);

            if (distance <= attackRange && distance < closestDistance)
            {
                closestEnemy = ship;
                closestDistance = distance;
            }
        }

        return closestEnemy;
    }

   
}