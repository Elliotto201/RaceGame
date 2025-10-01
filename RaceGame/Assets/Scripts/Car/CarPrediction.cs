using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarPrediction : NetworkBehaviour
{
    private const int CAR_BUFFER_SIZE = 512;
    private CarPositionData[] serverCarBuffer = new CarPositionData[CAR_BUFFER_SIZE];
    private Queue<CarInput> serverInputBuffer = new Queue<CarInput>();

    private PrometeoCarController carController;
    private Rigidbody carRb;
    private CarInput latestClientInput;

    private NetworkVariable<Vector3> authPosition;
    private NetworkVariable<Quaternion> authRotation;

    private float checkPositionTimer;
    private const float checkPositionInterval = 0.05f;

    private void Awake()
    {
        carController = GetComponent<PrometeoCarController>();
        carRb = GetComponent<Rigidbody>();

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

            int tick = (int)NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick;
            serverCarBuffer[tick % CAR_BUFFER_SIZE] = new CarPositionData
            {
                Position = transform.position,
                Rotation = transform.rotation,
            };

            ApplyInputPrediction(tick);
        }

        if (IsOwner)
        {
            checkPositionTimer += Time.deltaTime;
            if (checkPositionTimer >= checkPositionInterval)
            {
                checkPositionTimer = 0f;
                SendCheckPosition();
            }
        }
    }

    private void ApplyInputPrediction(int tick)
    {
        CarInput inputToApply = latestClientInput;
        serverInputBuffer.TryDequeue(out inputToApply);

        carController.SteerLeft = inputToApply.SteerLeft;
        carController.SteerRight = inputToApply.SteerRight;
        carController.SteerBackwards = inputToApply.SteerBack;
    }

    private void SendCheckPosition()
    {
        CheckPositionServerRpc(transform.position, transform.rotation, NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick);
    }

    public void SetInput(CarInput carInput)
    {
        if (!IsOwner) return;

        carController.SteerLeft = carInput.SteerLeft;
        carController.SteerRight = carInput.SteerRight;
        carController.SteerBackwards = carInput.SteerBack;

        SendInputToServerRpc(carInput);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void SendInputToServerRpc(CarInput carInput)
    {
        int tick = NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick;
        serverInputBuffer.Enqueue(carInput);
        latestClientInput = carInput;
    }

    [Rpc(SendTo.Owner)]
    private void ReconcileOwnerRpc(ReconcileData data)
    {
        transform.position = Vector3.Lerp(transform.position, data.authPos, 0.4f * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, data.authRot, 0.4f * Time.deltaTime);

        carRb.linearVelocity = data.linearVelocity;
        carRb.angularVelocity = data.angularVelocity;

        carController.SetReconciledDataLocal(data.carSpeed, data.linearVelocityX, data.linearVelocityZ, data.steerAxis);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void CheckPositionServerRpc(Vector3 position, Quaternion rotation, int tick)
    {
        var carData = serverCarBuffer[tick % CAR_BUFFER_SIZE];
        if (Vector3.Distance(carData.Position, position) > 0.1f || Quaternion.Angle(carData.Rotation, rotation) > 2f)
        {
            ReconcileOwnerRpc(new ReconcileData(transform.position, transform.rotation, carRb.linearVelocity, carRb.angularVelocity, 
                carController.carSpeed, carController.localVelocityX, carController.localVelocityZ, carController.steeringAxis
                ));
        }
    }

    private struct CarPositionData
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private struct ReconcileData : INetworkSerializable
    {
        public Vector3 authPos;
        public Quaternion authRot;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public float carSpeed;
        public float linearVelocityX;
        public float linearVelocityZ;
        public float steerAxis;

        public ReconcileData(Vector3 pos, Quaternion rot, Vector3 lin, Vector3 ang, float carSpe, float linVelX, float linVelZ, float steAxi)
        {
            authPos = pos;
            authRot = rot;
            linearVelocity = lin;
            angularVelocity = ang;
            carSpeed = carSpe;
            linearVelocityX = linVelX;
            linearVelocityZ = linVelZ;
            steerAxis = steAxi;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref authPos);
            serializer.SerializeValue(ref authRot);
            serializer.SerializeValue(ref linearVelocity);
            serializer.SerializeValue(ref angularVelocity);
            serializer.SerializeValue(ref carSpeed);
            serializer.SerializeValue(ref linearVelocityX);
            serializer.SerializeValue(ref linearVelocityZ);
            serializer.SerializeValue(ref steerAxis);
        }
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
