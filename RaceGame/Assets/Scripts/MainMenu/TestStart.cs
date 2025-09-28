using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestStart : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private GameManager ServerGameManager;
    [SerializeField] private string SceneName;
    [SerializeField] private LevelConfig[] LevelConfigs;
    [Header("Client Settings")]
    [SerializeField] private Loadout Loadout;
    [Space]
    [SerializeField] private Button ServerButton;
    [SerializeField] private Button ClientButton;

    private void Awake()
    {
        ServerButton.onClick.AddListener(Server);
        ClientButton.onClick.AddListener(Client);
    }

    private void Server()
    {
        var levelConfig = LevelConfigs.First(t => t.SceneName == SceneName);
        var spawnPoints = levelConfig.SpawnPoints.Select(t => t.transform);

        ServerGameManager.PrepareForJoins(spawnPoints.ToArray(), SceneName);
    }

    private void Client()
    {
        var joinRequest = new JoinRequest
        {
            IsTest = true,
            Loadout = Loadout,
        };

        var data = joinRequest.Serialize();

        NetworkManager.Singleton.NetworkConfig.ConnectionData = data;
        NetworkManager.Singleton.StartClient();
    }
}

[Serializable]
public class LevelConfig
{
    public string SceneName;
    public GameObject[] SpawnPoints;
}

[Serializable]
public class Loadout
{
    public string[] _Loadout;

    public Loadout(string[] loadout)
    {
        _Loadout = loadout;
    }
}
