using Unity.Netcode;
using UnityEngine;

public class CarHealthRotator : MonoBehaviour
{
    private Transform Camera;

    private void LateUpdate()
    {
        if (NetworkManager.Singleton.IsServer) return;

        if(Camera == null)
        {
            Camera = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude).transform;
        }

        transform.LookAt(Camera.transform.position);
    }
}
