using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplay;
using UnityEngine;
using System.Threading.Tasks;

public class ServerNetworkManager : NetworkBehaviour
{
    public delegate void OnConnectDelegate(int count);
    public OnConnectDelegate OnConnectHandler;

    private NetworkManager _networkManager;
    private GamePlayManager _gamePlayManager; // TODO: change to interface
    private MultiplayManager _multiplayManager;

    private bool _endingGame = false;

    //public delegate void OnDisconnectDelegate();
    //public OnDisconnectDelegate OnDisconnectHandler;

    //private Dictionary<ulong, PlayerSessionStatus> _playerSessionStatus;

    // NOTE: Function set to NetworkManager.ConnectionApprovalCallback.
    // Perform any approval checks required for you game here, set initial position and rotation, and whether or not connection is approved
    // TODO: consider moving the bulk logic to GamePlayManager
    private void ConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;
        Debug.Log("Approval check for clientId: " + clientId);

        Dictionary<ulong, PlayerSessionStatus> playerSessioStatuses = _gamePlayManager.GetPlayerSessionStatuses();

        // Track our player sessions.
        // First to join is player one, second to join is player two, etc...
        int playerDesignation = playerSessioStatuses.Count + 1; // because this is before we add our player, designation needs to start at 1
        playerSessioStatuses.Add(clientId, new PlayerSessionStatus(clientId, "player" + playerDesignation));

        // NOTE: Leaving rotation as default here, as we set initial rotation in ThirdPersonCameraController
        // Debug.Log("Player for client id: " + _playerSessionStatus[clientId].Designation);
        if (playerSessioStatuses[clientId].Designation == "player1")
        {
            response.Position = new Vector3(UnityEngine.Random.Range(-9.5f, 9.5f), 1, UnityEngine.Random.Range(5f, 9.5f));
            // Debug.Log($"Setting player 1 position: {response.Position}, rotation: {response.Rotation}");
        }
        else if (playerSessioStatuses[clientId].Designation == "player2")
        {
            response.Position = new Vector3(UnityEngine.Random.Range(-9.5f, 9.5f), 1, UnityEngine.Random.Range(-9.5f, -5f));
            // Debug.Log($"Setting player 2 position: {response.Position}, rotation: {response.Rotation}");
        }

        response.CreatePlayerObject = true;
        response.Approved = true;

        // NOTE: post-connection logic cannot go here, the client is not connected here yet.
    }

    private void OnClientConnected(ulong networkId)
    {
        Debug.Log("Connection ID: " + networkId + " CONNECTED. Current player count: " + _gamePlayManager.GetPlayerSessionStatuses().Count);

        _gamePlayManager.ReadyPlayerForGamePlay(networkId);

        // this may be null if local testing
        if (OnConnectHandler != null)
        {
            OnConnectHandler(_gamePlayManager.GetPlayerSessionStatuses().Count);
        }

        if (_gamePlayManager.IsMatchReadyToStart())
        {
            _gamePlayManager.LoadSingleModeScene(_networkManager.SceneManager, GamePlayManager.GAME_PLAY_SCENE);
            //_gamePlayManager.LoadGamePlayScene(_networkManager.SceneManager);
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

    public void Init(GamePlayManager gamePlayManager, MultiplayManager multiplayManager, OnConnectDelegate onConnectDelegate)//, OnDisconnectDelegate onDisconnectDelegate)
    {
        _networkManager = NetworkManager.Singleton;
        _gamePlayManager = gamePlayManager;
        _multiplayManager = multiplayManager;

        OnConnectHandler = onConnectDelegate;
        //OnDisconnectHandler = onDisconnectDelegate;

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

            unityTransport.SetConnectionData("0.0.0.0", (ushort)9011);

            _networkManager.StartServer();
        }
    }

    /// <summary>
    /// Handles the case where NetworkManager has told us a client has disconnected. This includes ourselves, if we're the host,
    /// and the server is stopped.
    /// </summary>
    private void OnClientDisconnect(ulong networkId)
    {
        Debug.Log("Connection ID: " + networkId + " Disconnected.");
        if (!_endingGame)
        {
            _endingGame = true;
            EndGameAfterDisconnect();
        }
        else
        {
            Debug.Log("Server is already cleaning up after game over, skipping duplicate clean up calls.");
        }
    }

    private async void OnSceneLoaded(SceneEvent sceneEvent)
    {
        Debug.Log($"OnSceneLoaded: {sceneEvent.SceneName}, event: {sceneEvent.SceneEventType}");
        if (sceneEvent.SceneName == GamePlayManager.GAME_PLAY_SCENE && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            Debug.Log("InitializePool");
            NetworkObjectPool.Singleton.InitializePool();
        }
        else if (sceneEvent.SceneName == GamePlayManager.GAME_OVER_SCENE && sceneEvent.SceneEventType == SceneEventType.LoadComplete) //SceneEventType.LoadEventCompleted)
        {
            // NOTE: using LoadComplete here means our single remaining client has loaded the Game Over scene.
            // If you had more than 2 player game, you'd have to refactor how you determine when it's appropriate to quit the game.
            // The scene event also contains a list of clients that have completed the scene change which could be helpful.

            Debug.Log("Quitting after game over scene loaded, by client: " + sceneEvent.ClientId);
            await UnreadyAndQuitServer();
        }
    }

    private async Task UnreadyAndQuitServer()
    {
        // could be null if local testing
        if (_multiplayManager != null)
        {
            // According to docs this should also disconnect remaining clients.
            // https://docs.unity.com/game-server-hosting/sdk/game-server-sdk-for-unity.html#Unready_the_game_server
            await _multiplayManager.UnReadyServer();
        }

        // we have reached game over, quit the server-side application.
        // Since it is a clean 0 exit code quit, Game Server Hosting will queue a deallocation request for us.
        // https://docs.unity.com/game-server-hosting/concepts/deallocations.html
        Application.Quit();
    }

    private void EndGameAfterDisconnect()
    {
        Debug.Log("Ending game after disconnect.");
        EndGame();
    }

    // For the sake of simplicity for this demo, if any player disconnects, just end the game. 
    // That means if only one player joins, then disconnects, the game session ends.
    // Your game may remain open to receiving new players, without ending the game session, up to you.
    public void EndGame()
    {
        Debug.Log("Ending game...");

        // change scene while network manager is still active
        if (_networkManager.SceneManager != null)
        {
            // We still get to perform the server cleanup afterwards as this script is not destroyed on scene change.
            _gamePlayManager.LoadSingleModeScene(_networkManager.SceneManager, GamePlayManager.GAME_OVER_SCENE);

            // TODO: is this still necessary if disconnecting from using UnReadyServer already happens? No, they all get removed
            //DespawnRemainingPlayerObjects();
        }
        else
        {
            Debug.Log("SceneManager was null");
        }
    }

    //private void DespawnRemainingPlayerObjects()
    //{
    //    foreach (KeyValuePair<ulong, NetworkClient> connectedClient in NetworkManager.Singleton.ConnectedClients)
    //    {
    //        Debug.Log("Despawning player with connectedClient clientid: " + connectedClient.Value.ClientId);

    //        try
    //        {
    //            NetworkManager.Singleton.ConnectedClients[connectedClient.Key].PlayerObject.Despawn();
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.Log("Error while trying to despawn connected clients, maybe they already despawned. " + e.Message);
    //        }
    //    }
    //}

    public void Dispose()
    {
        Debug.Log("Dispose ServerNetworkManager");
        if (_networkManager == null)
            return;

        _networkManager.ConnectionApprovalCallback -= ConnectionApprovalCheck;
        _networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        _networkManager.OnClientConnectedCallback -= OnClientConnected;
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

    void OnNetworkReady()
    {
        Debug.Log("server started!");
        _networkManager.SceneManager.OnSceneEvent += OnSceneLoaded;
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
