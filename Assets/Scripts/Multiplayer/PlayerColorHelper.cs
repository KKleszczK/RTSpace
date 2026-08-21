using Unity.Netcode;
using UnityEngine;

public static class PlayerColorHelper
{
    private static readonly Color OwnColor =
        new Color(
            0f / 255f,
            115f / 255f,
            223f / 255f,
            1f);

    private static readonly Color EnemyColor =
        new Color(
            223f / 255f,
            22f / 255f,
            46f / 255f,
            1f);

    public static Color GetColor(
        ulong ownerId)
    {
        if (NetworkManager.Singleton == null)
            return Color.white;

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        return ownerId == localClientId
            ? OwnColor
            : EnemyColor;
    }
}