using UnityEngine;

public abstract class BaseAttack : MonoBehaviour
{
    public abstract void Attack(NetworkHealth target);
}