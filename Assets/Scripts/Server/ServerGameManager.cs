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

public class PlayerSessionStatus
{
    public bool IsReady = false;
    public string Designation = "";

    public PlayerSessionStatus(string designation)
    {
        Designation = designation;
    }
}

// TODO: split out networking and make this more about game play management... or have another class to manage that
public class ServerGameManager : NetworkBehaviour, IGameManager
{
    public GameObject PlayerPrefab;

    private GameStateManager _gameStateManager;
    private NetworkManager _networkManager;
    //private MessagingManager _messagingManager; // for calling to the client
    //private Dictionary<ulong, string> _playerSessions;
    //private Dictionary<ulong, string> _playerClientMapping; // TODO: roll this into an object with playerSessions
    private Dictionary<ulong, PlayerSessionStatus> _playerSessionStatus;

    private static int MaxPlayersPerSession = 2;

    //private MultiplayManager _multiplayManager;

    //public void Setup(MultiplayManager multiplayManager)
    //{
    //    // set reference to the multiplay manager
    //    _multiplayManager = multiplayManager;

    //    if (_multiplayManager == null)
    //    {
    //        Debug.Log("MultiplayManager is null, did you forget to add it to the GameManager game object?");
    //    }
    //}

    // TODO: for now this is just for testing of spawing objects, remove.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("ServerGameManager.OnNetworkSpawn...");

        // skip functionality if on client side
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        //SetPlayerDesignations();
        //SpawnObjects();
    }

    private void ConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;
        Debug.Log("Approval check for clientId: " + clientId);

        // track our player sessions
        // first to join is player one, second to join is player two, etc...
        int playerDesignation = _playerSessionStatus.Count + 1; // because this is before we add our player, designation needs to start at 1
        _playerSessionStatus.Add(clientId, new PlayerSessionStatus("p" + playerDesignation));

        Debug.Log("Player for client id: " + _playerSessionStatus[clientId].Designation);
        if (_playerSessionStatus[clientId].Designation == "p1")
        {
            Debug.Log("setting player 1 position");
            response.Position = new Vector3(UnityEngine.Random.Range(-9.5f, 9.5f), 1, UnityEngine.Random.Range(-9.5f, -5f));
        }
        else if (_playerSessionStatus[clientId].Designation == "p2")
        {
            Debug.Log("setting player 2 position");
            response.Position = new Vector3(UnityEngine.Random.Range(-9.5f, 9.5f), 1, UnityEngine.Random.Range(5f, 9.5f));
        }

        response.CreatePlayerObject = true;
        response.Approved = true;
        response.Rotation = Quaternion.identity;

        // NOTE: post-connection logic cannot go here, the client is not connected here yet.
    }

    private void OnClientConnected(ulong networkId)
    {
        Debug.Log("Connection ID: " + networkId + " CONNECTED.");
        Debug.Log("Current player count: " + _playerSessionStatus.Count);

        ReadyPlayerForGamePlay(networkId, _playerSessionStatus[networkId].Designation);

        CheckAndSendGameReadyToStartMsg(0);
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
                    Debug.Log("Found the playerController...");
                    // Update some value on the player's prefab
                    ((PlayerController)networkBehaviour).playerId.Value = playerDesignation;
                }
            }

            _playerSessionStatus[clientId].IsReady = true;
        }
        else
        {
            Debug.Log("player object was null");
        }
    }

    // TODO: remove param if not necessary
    private void CheckAndSendGameReadyToStartMsg(int connectionId)
    {
        int readyCount = 0;
        foreach(KeyValuePair<ulong, PlayerSessionStatus> playerSessionStatus in _playerSessionStatus)
        {
            if (playerSessionStatus.Value.IsReady)
            {
                readyCount++;
            }
        }

        Debug.Log("Current player count: " + readyCount + ", Max Players: " + MaxPlayersPerSession);
        //if (_playerSessions.Count == MaxPlayersPerSession)
        if (readyCount == MaxPlayersPerSession)
        {
            Debug.Log("Game is full and is ready to start.");

            // TODO: verify this calls all clients: It does!
            if (_gameStateManager != null)
            {
                Debug.Log("Loading GamePlay scene...");
                var status = _networkManager.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning($"Failed to load GamePlay " +
                          $"with a {nameof(SceneEventProgressStatus)}: {status}");
                }
                Debug.Log("GamePlay scene started.");

                _gameStateManager.CurrentGameState.Value = GameState.started;

                //SpawnObjects();
            }
            else
            {
                Debug.Log("_gameStateManager is null");
            }



            //// TODO: verify this calls all clients
            //if (_messagingManager != null)
            //{
            //    _messagingManager.StartGameClientRpc();
            //} else
            //{
            //    Debug.Log("Messaging manager is null");
            //}


            // tell all players the game is ready to start
            //foreach (KeyValuePair<ulong, string> playerSession in _playerSessions)
            //{
            //    //GameSessionState = "STARTED";
            //    //NetworkMessage responseMessage = new NetworkMessage("START", playerSession.Value);
            //    //SendMessage(playerSession.Key, responseMessage);
            //}
        }
    }

    // TODO: just used for testing right now, don't remove logic, keep for reference at least.

    //private void SetPlayerDesignations()
    //{
    //    Debug.Log("Connected clients: " + NetworkManager.Singleton.ConnectedClients.Count);

    //    int playerCount = 1;
    //    foreach (KeyValuePair<ulong, NetworkClient> connectedClient in NetworkManager.Singleton.ConnectedClients)
    //    {
    //        Debug.Log(connectedClient.Key);
    //        _playerClientMapping.Add(connectedClient.Value.ClientId, "player" + playerCount);

    //        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[connectedClient.Value.ClientId].PlayerObject;
    //        Debug.Log(playerObject.IsLocalPlayer);

    //        foreach (NetworkBehaviour networkBehaviour in playerObject.GetComponentsInChildren<NetworkBehaviour>())
    //        {
    //            // The PlayerController is a script attached to the Player Prefab.
    //            // Only change the server side representation of PlayerController, as the network variable will sync
    //            if (networkBehaviour is PlayerController && !networkBehaviour.IsLocalPlayer)
    //            {
    //                Debug.Log("Found the playerController...");
    //                // Update some value on the player's prefab
    //                ((PlayerController)networkBehaviour).playerId.Value = "p" + playerCount;
    //                playerCount++;
    //            }
    //        }
    //    }




    //    //int playerCount = 1;
    //    //// go through all the spawned objects (is there a "PlayerPrefabObjects" method?)
    //    //foreach (KeyValuePair<ulong, NetworkObject> networkObject in NetworkManager.SpawnManager.SpawnedObjects)
    //    //{
    //    //    // find the ones that are NetworkBehaviours (because that's what's attached to our Player Prefabs)
    //    //    foreach (NetworkBehaviour networkBehaviour in networkObject.Value.GetComponentsInChildren<NetworkBehaviour>())
    //    //    {
    //    //        // The PlayerController is a script attached to the Player Prefab.
    //    //        // Only change the server side representation of PlayerController, as the network variable will sync
    //    //        if (networkBehaviour is PlayerController && !networkBehaviour.IsLocalPlayer)
    //    //        {
    //    //            Debug.Log("Found the playerController...");
    //    //            // Update some value on the player's prefab
    //    //            ((PlayerController)networkBehaviour).playerId.Value = "p" + playerCount;
    //    //            playerCount++;
    //    //        }
    //    //    }
    //    //}
    //}

    // TODO: For testing, remove
    //private void SpawnObjects()
    //{
    //    Debug.Log("Spawing objects");


    //    GameObject go = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
    //    go.GetComponent<NetworkObject>().Spawn();

    //    //go.GetComponent<NetworkObject>().GetComponentInChildren<PlayerController>().playerId = "plyaer" + 1;

    //    SetPlayerDesignations();
    //}

    void Start()
    {
        Debug.Log("ServerGameManager start");

        // because of how this class is manually instantiated, don't put anything important here, use Init 
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

    void OnNetworkReady()
    {
        Debug.Log("server started!");
    }

    /// <summary>
    /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
    /// and the server is stopped."
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
        Debug.Log("CheckForGameEnd");

        // TODO: perform game over checks
        // TODO: probably remove player who disconnected
    }

    void Update()
    {
        // TODO: remove this
        //if (_gameStateManager.CurrentGameState.Value == GameState.started)
        //{
        //    // game started
        //}
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        //_playerSessions = new Dictionary<ulong, string>();
        //_playerClientMapping = new Dictionary<ulong, string>();

        _playerSessionStatus = new Dictionary<ulong, PlayerSessionStatus>();
    }

    void OnApplicationQuit()
    {

    }

    public void Init(string setting, GameStateManager gameStateManager)//, MessagingManager messagingManager)
    {
        Debug.Log(setting);

        _gameStateManager = gameStateManager;

        //_messagingManager = messagingManager;//(ClientMessagingManager) messagingManager;

        _networkManager = NetworkManager.Singleton;

        // We add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious Netcode for GameObjects (Netcode)
        // warning: "No ConnectionApproval callback defined. Connection approval will timeout"

        _networkManager.ConnectionApprovalCallback += ConnectionApprovalCheck; // assumes all connections are valid
        _networkManager.OnServerStarted += OnNetworkReady;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        _networkManager.OnClientConnectedCallback += OnClientConnected;

    }

    public void Dispose()
    {
        if (_networkManager == null)
            return;

        //_networkManager.ConnectionApprovalCallback -= ApprovalCheck; // TODO: not implemented
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        _networkManager.OnServerStarted -= OnNetworkReady;
        if (_networkManager.IsListening)
            _networkManager.Shutdown();
    }
}

// TODO: move to shared
public class StartGameArgs : EventArgs
{
    public StartGameArgs() { }
}