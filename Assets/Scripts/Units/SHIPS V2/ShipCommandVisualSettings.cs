using UnityEngine;

public class ShipCommandVisualSettings : MonoBehaviour
{
    public static ShipCommandVisualSettings Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [Header("Command Colors")]
    [SerializeField] private Color moveColor = Color.green;
    [SerializeField] private Color attackMoveColor = Color.red;
    [SerializeField] private Color attackColor = Color.red;

    [SerializeField] private Color guardColor = Color.cyan;
    [SerializeField] private Color followColor = Color.cyan;
    [SerializeField] private Color escortColor = Color.cyan;

    [SerializeField] private Color backToBaseColor = Color.yellow;
    [SerializeField] private Color dockColor = Color.yellow;

    [SerializeField] private Color mineColor = Color.blue;

    public Color GetColor(
        ShipUnit.ShipCommandType commandType)
    {
        return commandType switch
        {
            ShipUnit.ShipCommandType.Move =>
                moveColor,

            ShipUnit.ShipCommandType.AttackMove =>
                attackMoveColor,

            ShipUnit.ShipCommandType.Attack =>
                attackColor,

            ShipUnit.ShipCommandType.Guard =>
                guardColor,

            ShipUnit.ShipCommandType.Follow =>
                followColor,

            ShipUnit.ShipCommandType.Escort =>
                escortColor,

            ShipUnit.ShipCommandType.BackToBase =>
                backToBaseColor,

            ShipUnit.ShipCommandType.Dock =>
                dockColor,

            ShipUnit.ShipCommandType.Mine =>
                mineColor,

            _ =>
                Color.white
        };
    }
}