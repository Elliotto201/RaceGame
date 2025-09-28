using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Singleton { get; private set; }

    private bool GameStarted = false;

    private Dictionary<ulong, Loadout> playerLoadouts = new();
    private Transform[] SpawnPositions;

    [SerializeField] private bool IsTest;
    [Header("Settings")]
    [SerializeField] private GameObject TestCanvas;
    [SerializeField] private GameObject PlayerCarPrefab;
    [SerializeField] private AbilityList AbilityList;

    private void Awake()
    {
        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void PrepareForJoins(Transform[] playerSpawnPositions, string sceneName)
    {
        SpawnPositions = playerSpawnPositions;

        NetworkManager.Singleton.ConnectionApprovalCallback += OnClientConnect;
        NetworkManager.Singleton.StartServer();

        await Task.Delay(50);
#if UNITY_EDITOR
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
#endif
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        var go = Instantiate(TestCanvas);
        go.transform.GetChild(0).gameObject.GetComponent<Button>().onClick.AddListener(StartGameTest);

#if UNITY_EDITOR
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
#endif
    }

#if UNITY_EDITOR

    private void StartGameTest()
    {
        StartGame();
    }
#endif

    public void StartGame()
    {
        GameStarted = true;

        int currentSpawnPositionIndex = 0;

        foreach(var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if(playerLoadouts.TryGetValue(clientId, out var loadout))
            {
                Transform spawnPos = SpawnPositions[currentSpawnPositionIndex];

                Quaternion rot = Quaternion.Euler(0, GetYRotationFromForward(spawnPos.forward), 0);

                var playerCar = Instantiate(PlayerCarPrefab, spawnPos.position, rot);
                playerCar.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

                var abilityArray = new Ability[4];
                for(int i = 0; i < 4; i++)
                {
                    abilityArray[i] = AbilityList.Abilities.First(t => t.Name == loadout._Loadout[i]);
                }

                SetCarStatsSpawn(playerCar.GetComponent<Car>(), abilityArray);

                currentSpawnPositionIndex++;
            }
            else
            {
                NetworkManager.Singleton.DisconnectClient(clientId, "Loadout could not be found");
            }
        }
    }

    private void SetCarStatsSpawn(Car car, Ability[] abilities)
    {
        car.SetAbilities(abilities[0], abilities[1], abilities[2], abilities[3]);
    }

    public void Stop()
    {
        NetworkManager.Singleton.Shutdown();
    }

    private void OnClientConnect(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (GameStarted)
        {
            response.Approved = false;
            response.Pending = false;
        }

        if (IsTest)
        {
            try
            {
                var joinRequest = JoinRequest.Deserialize(request.Payload);

                if (!joinRequest.IsTest)
                {
                    throw new Exception("Match config was set to !isEditor when GameManager IsEditor is set to true!");
                }

                var loadout = joinRequest.Loadout;
                playerLoadouts.Add(request.ClientNetworkId, loadout);

                response.Approved = true;
                response.Pending = false;
            }
            catch(Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
    }

    private static float GetYRotationFromForward(Vector3 forward)
    {
        forward.y = 0;
        forward.Normalize();
        return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
    }
}
