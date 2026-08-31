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
    [SerializeField] private Color supportColor = Color.cyan;
    [SerializeField] private Color attackColor = Color.red;
    [SerializeField] private Color mineColor = Color.blue;
    [SerializeField] private Color dockColor = Color.yellow;

    public Color GetColor(
        ShipUnit.ShipCommandType commandType)
    {
        return commandType switch
        {
            ShipUnit.ShipCommandType.Move =>
                moveColor,

            ShipUnit.ShipCommandType.AttackMove =>
                attackMoveColor,

            ShipUnit.ShipCommandType.Support =>
                supportColor,

            ShipUnit.ShipCommandType.Attack =>
                attackColor,

            ShipUnit.ShipCommandType.Mine =>
                mineColor,

            ShipUnit.ShipCommandType.Dock =>
                dockColor,

            _ =>
                Color.white
        };
    }
}