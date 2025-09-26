using Unity.Netcode;
using UnityEngine;

public class Car : NetworkBehaviour
{
    private PrometeoCarController CarController;
    [SerializeField] private GameObject PlayerCam;

    private void Awake()
    {
        CarController = GetComponent<PrometeoCarController>();
    }

    private void Start()
    {
        if (IsOwner)
        {
            var go = Instantiate(PlayerCam);
            go.GetComponent<CameraFollow>().carTransform = transform;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool steerLeft = false;
        bool steerRight = false;
        bool steerBackwards = false;

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.A))
        {
            steerLeft = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            steerRight = true;
        }
        if (Input.GetKey(KeyCode.S))
        {
            steerBackwards = true;
        }
#else
        
#endif

        CarController.SteerLeft = steerLeft;
        CarController.SteerRight = steerRight;
        CarController.SteerBackwards = steerBackwards;
    }
}
