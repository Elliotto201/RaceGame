using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class CarAbilities : NetworkBehaviour
{
    [Header("Self Destruct")]
    [SerializeField] private GameObject ExplosionEffect;

    public void SelfExplode()
    {
        var colliders = Physics.OverlapSphere(transform.position, 8, LayerMask.GetMask("Player"));
        var selfCar = GetComponent<Car>();

        foreach(var collider in colliders)
        {
            if(collider.TryGetComponent(out Car car))
            {
                if(selfCar != car)
                {
                    car.Damage(int.MaxValue - 1);
                }
                else
                {
                    car.Damage(selfCar.CurrentHealth - 1);
                }
            }
        }

        SpawnExplosionEffectRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnExplosionEffectRpc()
    {
        var effect = Instantiate(ExplosionEffect, transform.position, Quaternion.identity);
    }
}
