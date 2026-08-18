using UnityEngine;

public interface IDamageable
{
    ulong OwnerId { get; }

    bool IsDead { get; }

    Transform DamageTransform { get; }

    void TakeWeaponDamage(
        float hullDamage,
        float shieldDamage);
}