using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Unity.Services.Matchmaker.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplay;
using UnityEngine.Networking.Types;

// TODO: split out networking and make this more about game play management... or have another class to manage that
public class ServerGameManager : NetworkBehaviour, IGameManager
{
    public GameObject PlayerPrefab;

    private static int MinPlayersPerSession = 2; // Rename min players for game start
    private NetworkManager _networkManager;
    private Dictionary<ulong, PlayerSessionStatus> _playerSessionStatus;

    // NOTE: Function set to NetworkManager.ConnectionApprovalCallback.
    // Perform any approval checks required for you game here, set initial position and rotation, and whether or not connection is approved
    private void ConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;
        Debug.Log("Approval check for clientId: " + clientId);

        // Track our player sessions.
        // First to join is player one, second to join is player two, etc...
        int playerDesignation = _playerSessionStatus.Count + 1; // because this is before we add our player, designation needs to start at 1
        _playerSessionStatus.Add(clientId, new PlayerSessionStatus("p" + playerDesignation));

        // NOTE: Leaving rotation as default here, as we set initial rotation in ThirdPersonCameraController
        // Debug.Log("Player for client id: " + _playerSessionStatus[clientId].Designation);
        if (_playerSessionStatus[clientId].Designation == "p1")
        {
            response.Position = new Vector3(UnityEngine.Random.Range(-9.5f, 9.5f), 1, UnityEngine.Random.Range(5f, 9.5f));
            // Debug.Log($"Setting player 1 position: {response.Position}, rotation: {response.Rotation}");
        }
        else if (_playerSessionStatus[clientId].Designation == "p2")
        {
            response.Position = new Vector3(UnityEngine.Random.Range(-9.5f, 9.5f), 1, UnityEngine.Random.Range(-9.5f, -5f));
            // Debug.Log($"Setting player 2 position: {response.Position}, rotation: {response.Rotation}");
        }

        response.CreatePlayerObject = true;
        response.Approved = true;

        // NOTE: post-connection logic cannot go here, the client is not connected here yet.
    }

    private void ReadyPlayerForGamePlay(ulong clientId, string playerDesignation)
    {
        Debug.Log($"ReadyPlayerForGamePlay: {clientId}, {playerDesignation}");

        // Find the client we want to 'ready'
        NetworkObject playerObject = null;
        foreach (KeyValuePair<ulong, NetworkClient> connectedClient in NetworkManager.Singleton.ConnectedClients)
        {

            Debug.Log("ConnectedClient key: " + connectedClient.Key + ", Assumed clientId which should be key: " + clientId);
            Debug.Log("ConnectedClient clientid: " + connectedClient.Value.ClientId);

            if (connectedClient.Value.ClientId == clientId)
            {
                Debug.Log("ConnectedClient found");
                playerObject = NetworkManager.Singleton.ConnectedClients[connectedClient.Key].PlayerObject;
                break;
            }
        }

        // Update the client's player prefab object with whatever is required for your game.
        // Here we set the player 1/2 designation.
        if (playerObject != null)
        {
            Debug.Log(playerObject.IsLocalPlayer);

            foreach (NetworkBehaviour networkBehaviour in playerObject.GetComponentsInChildren<NetworkBehaviour>())
            {
                // The PlayerController is a script attached to the Player Prefab.
                // Only change the server side representation of PlayerController, as the network variable will sync
                if (networkBehaviour is PlayerController && !networkBehaviour.IsLocalPlayer)
                {
                    Debug.Log("Found the playerController, is spawned: " + networkBehaviour.IsSpawned);

                    // Update some value on the player's prefab
                    ((PlayerController)networkBehaviour).playerDesignation.Value = playerDesignation;
                }
            }

            _playerSessionStatus[clientId].IsReady = true;
        }
        else
        {
            Debug.Log("player object was null");
        }
    }

    // TODO: rename and remove param if not necessary
    private void CheckAndSendGameReadyToStartMsg(int connectionId)
    {
        int readyCount = 0;
        foreach (KeyValuePair<ulong, PlayerSessionStatus> playerSessionStatus in _playerSessionStatus)
        {
            if (playerSessionStatus.Value.IsReady)
            {
                readyCount++;
            }
        }

        Debug.Log("Current player count: " + readyCount + ", Min Players: " + MinPlayersPerSession);

        if (readyCount == MinPlayersPerSession)
        {
            Debug.Log("Game is full and is ready to start.");
            Debug.Log("Loading GamePlay scene...");

            var status = _networkManager.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning($"Failed to load GamePlay with a {nameof(SceneEventProgressStatus)}: {status}");
            }
            else
            {
                Debug.Log("GamePlay scene started.");
            }
        }
    }

    private void OnClientConnected(ulong networkId)
    {
        Debug.Log("Connection ID: " + networkId + " CONNECTED. Current player count: " + _playerSessionStatus.Count);

        ReadyPlayerForGamePlay(networkId, _playerSessionStatus[networkId].Designation);
        CheckAndSendGameReadyToStartMsg(0);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        if (scene.name == "GamePlay")
        {
            NetworkObjectPool.Singleton.InitializePool();
        }
    }

    public void StartServer()
    {
        Debug.Log($"Starting server on port {MultiplayService.Instance.ServerConfig.Port}");

        var unityTransport = _networkManager.gameObject.GetComponent<UnityTransport>();
        _networkManager.NetworkConfig.NetworkTransport = unityTransport;

        // set port based on server config
        unityTransport.SetConnectionData("0.0.0.0", (ushort)MultiplayService.Instance.ServerConfig.Port);

        _networkManager.StartServer();
    }

    public void Init(string setting)
    {
        Debug.Log(setting);

        _networkManager = NetworkManager.Singleton;

        // We add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious Netcode for GameObjects (Netcode)
        // warning: "No ConnectionApproval callback defined. Connection approval will timeout"

        _networkManager.ConnectionApprovalCallback += ConnectionApprovalCheck; // assumes all connections are valid
        _networkManager.OnServerStarted += OnNetworkReady;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        _networkManager.OnClientConnectedCallback += OnClientConnected;

        if (ApplicationController.IsLocalTesting)
        {
            // Start server here when local testing
            var unityTransport = _networkManager.gameObject.GetComponent<UnityTransport>();
            _networkManager.NetworkConfig.NetworkTransport = unityTransport;

            // set port based on server config
            unityTransport.SetConnectionData("0.0.0.0", (ushort)9011);

            _networkManager.StartServer();
        }
    }

    void OnNetworkReady()
    {
        Debug.Log("server started!");
    }

    void Start()
    {
        Debug.Log("ServerGameManager start");

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (ApplicationController.IsLocalTesting)
        {
            MinPlayersPerSession = 1;
        }
        // because of how this class is manually instantiated, don't put anything important here, use Init 
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _playerSessionStatus = new Dictionary<ulong, PlayerSessionStatus>();
    }

    /// <summary>
    /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
    /// and the server is stopped.
    /// </summary>
    private void OnClientDisconnect(ulong networkId)
    {
        Debug.Log("Connection ID: " + networkId + " Disconnected.");
        EndGameAfterDisconnect(networkId);
    }

    // For the sake of simplicity for this demo, if any player disconnects, just end the game. 
    // That means if only one player joins, then disconnects, the game session ends.
    // Your game may remain open to receiving new players, without ending the game session, up to you.
    private void EndGameAfterDisconnect(ulong disconnectingId)
    {
        Debug.Log("Ending game after disconnect");

        // TODO: perform game over checks
        // TODO: probably remove player who disconnected
    }

    public void Dispose()
    {
        if (_networkManager == null)
            return;

        _networkManager.ConnectionApprovalCallback -= ConnectionApprovalCheck;
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        _networkManager.OnServerStarted -= OnNetworkReady;

        if (_networkManager.IsListening)
        {
            _networkManager.Shutdown();
        }
    }

    void OnApplicationQuit()
    {
        Dispose();
    }
}
