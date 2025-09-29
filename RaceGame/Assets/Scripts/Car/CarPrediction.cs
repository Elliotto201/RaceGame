using Unity.Netcode;
using UnityEngine;

public class CarPrediction : NetworkBehaviour
{
    private const int CAR_BUFFER_SIZE = 512;
    private CarPositionData[] serverCarBuffer = new CarPositionData[CAR_BUFFER_SIZE];
    private CarInput[] serverInputBuffer = new CarInput[CAR_BUFFER_SIZE];

    private PrometeoCarController carController;
    private Rigidbody carRb;
    private CarInput lastReceivedInput;

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
        CarInput inputToApply = serverInputBuffer[tick % CAR_BUFFER_SIZE];
        carController.SteerLeft = inputToApply.SteerLeft;
        carController.SteerRight = inputToApply.SteerRight;
        carController.SteerBackwards = inputToApply.SteerBack;
    }

    private void SendCheckPosition()
    {
        CheckPositionServerRpc(transform.position, transform.rotation, (int)NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick);
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
        lastReceivedInput = carInput;
        int tick = (int)NetworkManager.Singleton.NetworkTickSystem.ServerTime.Tick;
        serverInputBuffer[tick % CAR_BUFFER_SIZE] = carInput;

        carController.SteerLeft = carInput.SteerLeft;
        carController.SteerRight = carInput.SteerRight;
        carController.SteerBackwards = carInput.SteerBack;
    }

    [Rpc(SendTo.Owner)]
    private void ReconcileOwnerRpc(Vector3 authPos, Quaternion authRot, Vector3 linearVelocity, Vector3 angularVelocity, float carSpeed, float linearVelocityX, float linearVelocityZ)
    {
        transform.position = Vector3.Lerp(transform.position, authPos, 0.4f * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, authRot, 0.4f * Time.deltaTime);

        carRb.linearVelocity = linearVelocity;
        carRb.angularVelocity = angularVelocity;

        carController.SetReconciledDataLocal(carSpeed, linearVelocityX, linearVelocityZ);
    }

    [Rpc(SendTo.Server, RequireOwnership = true)]
    private void CheckPositionServerRpc(Vector3 position, Quaternion rotation, int tick)
    {
        var carData = serverCarBuffer[tick % CAR_BUFFER_SIZE];
        if (Vector3.Distance(carData.Position, position) > 0.2f || Quaternion.Angle(carData.Rotation, rotation) > 5f)
        {
            ReconcileOwnerRpc(transform.position, transform.rotation, carRb.linearVelocity, carRb.angularVelocity, 
                carController.carSpeed, carController.localVelocityX, carController.localVelocityZ
                );
        }
    }

    private struct CarPositionData
    {
        public Vector3 Position;
        public Quaternion Rotation;
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
