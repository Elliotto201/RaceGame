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


        SpawnExplosionEffectRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnExplosionEffectRpc()
    {
        var effect = Instantiate(ExplosionEffect, transform.position, Quaternion.identity);
    }
}
