using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    public TMP_Text myMoneyText;
    public TMP_Text enemyMoneyText;

    public Button add10Button;
    public Button remove10Button;

    private PlayerMoney myPlayer;
    private PlayerMoney enemyPlayer;

    private void Start()
    {
        add10Button.onClick.AddListener(() => AddMoney(10));
        remove10Button.onClick.AddListener(() => AddMoney(-10));
    }

    private void Update()
    {
        FindPlayersIfNeeded();

        if (myPlayer == null || enemyPlayer == null)
            return;

        myMoneyText.text = "Player: " + myPlayer.money.Value;
        enemyMoneyText.text = "Enmey: " + enemyPlayer.money.Value;
    }

    private void FindPlayersIfNeeded()
    {
        PlayerMoney[] players = FindObjectsByType<PlayerMoney>();

        //Debug.Log("FOUND PLAYERS: " + players.Length);

        foreach (PlayerMoney player in players)
        {
            //Debug.Log(
            //    "PlayerMoney: OwnerClientId=" + player.OwnerClientId +
            //    " IsOwner=" + player.IsOwner +
            //    " IsSpawned=" + player.IsSpawned
            //);

            if (player.IsOwner)
                myPlayer = player;
            else
                enemyPlayer = player;
        }
    }


    private void AddMoney(int amount)
    {
        FindPlayersIfNeeded();

        if (myPlayer == null)
        {
            Debug.LogWarning("Nie znaleziono lokalnego gracza.");
            return;
        }

        if (amount >= 0)
            myPlayer.AddMoneyServerRpc(amount);
        else
            myPlayer.RemoveMoneyServerRpc(-amount);
    }
}