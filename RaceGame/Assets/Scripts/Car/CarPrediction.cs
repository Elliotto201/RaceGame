using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarPrediction : NetworkBehaviour
{
    private PrometeoCarController carController;

    private NetworkVariable<Vector3> authPosition;
    private NetworkVariable<Quaternion> authRotation;

    private void Awake()
    {
        carController = GetComponent<PrometeoCarController>();


        authPosition = new(transform.position, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        authRotation = new(transform.rotation, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    private void Update()
    {
        if (!IsOwner && !IsServer)
        {
            transform.position = Vector3.Lerp(transform.position, authPosition.Value, 0.4f * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, authRotation.Value, 0.4f * Time.deltaTime);
        }
        else if (!IsOwner && IsServer)
        {
            authPosition.Value = transform.position;
            authRotation.Value = transform.rotation;
        }

        if (IsOwner)
        {
            SendPositionToServerRpc(transform.position, transform.rotation);
        }
    }

    public void SetInput(CarInput carInput)
    {
        if (!IsOwner) return;

        carController.SteerLeft = carInput.SteerLeft;
        carController.SteerRight = carInput.SteerRight;
        carController.SteerBackwards = carInput.SteerBack;
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void SendPositionToServerRpc(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }
}

public struct CarInput : INetworkSerializable
{
    public bool SteerLeft;
    public bool SteerRight;
    public bool SteerBack;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SteerLeft);
        serializer.SerializeValue(ref SteerRight);
        serializer.SerializeValue(ref SteerBack);
    }
}
