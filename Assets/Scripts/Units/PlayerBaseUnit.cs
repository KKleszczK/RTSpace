using UnityEngine;

public class PlayerBaseUnit : MonoBehaviour
{
    private UnitOwner owner;
    private NetworkHealth health;
    

    private void Awake()
    {
        owner = GetComponent<UnitOwner>();
        health = GetComponent<NetworkHealth>();
        
    }

    public UnitOwner Owner => owner;
    public NetworkHealth Health => health;
    
}