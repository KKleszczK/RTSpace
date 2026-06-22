using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MoneyHud : MonoBehaviour
{
    public TMP_Text myMoneyText;
    public TMP_Text enemyMoneyText;

    public Button addButton;
    public Button removeButton;

    private PlayerMoney myMoney;
    private PlayerMoney enemyMoney;

    private void Start()
    {
        addButton.onClick.AddListener(() =>
        {
            if (myMoney != null)
                myMoney.AddMoneyServerRpc(100);
        });

        removeButton.onClick.AddListener(() =>
        {
            if (myMoney != null)
                myMoney.RemoveMoneyServerRpc(100);
        });
    }

    private void Update()
    {
        if (myMoney == null || enemyMoney == null)
            FindPlayers();

        if (myMoney != null)
            myMoneyText.text = "Moja kasa: " + myMoney.money.Value;

        if (enemyMoney != null)
            enemyMoneyText.text = "Wróg: " + enemyMoney.money.Value;
    }

    private void FindPlayers()
    {
        PlayerMoney[] players = FindObjectsByType<PlayerMoney>(FindObjectsSortMode.None);

        foreach (PlayerMoney p in players)
        {
            if (p.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                myMoney = p;
            else
                enemyMoney = p;
        }
    }
}