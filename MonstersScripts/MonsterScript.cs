using Unity.Netcode;
using UnityEngine;

public class MonsterScript : NetworkBehaviour
{
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float speed;
    [SerializeField] protected float damage;

    private NetworkVariable<float> health = new NetworkVariable<float>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
        }
    }

    private void Update()
    {
        if (IsServer && health.Value <= 0)
        {
            Dead();
        }
    }

    protected void DropObject()
    {
    }

    private void Dead()
    {
        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }
}