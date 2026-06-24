using UnityEngine;

public class LaserAttack : BaseAttack
{
    [SerializeField] private int damage = 10;

    public override void Attack(NetworkHealth target)
    {
        if (target == null)
            return;

        target.TakeDamage(damage);
    }
}