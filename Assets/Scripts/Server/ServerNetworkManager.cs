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
    private ServerGamePlayManager _serverGamePlayManager;
    private ServerMultiplayManager _serverMultiplayManager;

    private bool _endingGame = false;

    // NOTE: Function set to NetworkManager.ConnectionApprovalCallback.
    // Perform any approval checks required for you game here, set initial position and rotation, and whether or not connection is approved
    private void ConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;
        Debug.Log("Approval check for clientId: " + clientId);

        Dictionary<ulong, PlayerSessionStatus> playerSessioStatuses = _serverGamePlayManager.GetPlayerSessionStatuses();

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
        response.Approved = true;  // assumes all connections are valid

        // NOTE: post-connection logic cannot go here, the client is not connected here yet.
    }

    private void OnClientConnected(ulong networkId)
    {
        Debug.Log("Connection ID: " + networkId + " CONNECTED. Current player count: " + _serverGamePlayManager.GetPlayerSessionStatuses().Count);

        _serverGamePlayManager.ReadyPlayerForGamePlay(networkId);

        // this may be null if local testing
        if (OnConnectHandler != null)
        {
            OnConnectHandler(_serverGamePlayManager.GetPlayerSessionStatuses().Count);
        }

        if (_serverGamePlayManager.IsMatchReadyToStart())
        {
            _serverGamePlayManager.LoadSingleModeScene(_networkManager.SceneManager, ServerGamePlayManager.GAME_PLAY_SCENE);
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

    public void Init(ServerGamePlayManager serverGamePlayManager, ServerMultiplayManager serverMultiplayManager, OnConnectDelegate onConnectDelegate)
    {
        _networkManager = NetworkManager.Singleton;
        _serverGamePlayManager = serverGamePlayManager;
        _serverMultiplayManager = serverMultiplayManager;

        OnConnectHandler = onConnectDelegate;

        // We add ApprovalCheck callback BEFORE OnNetworkSpawn to avoid spurious Netcode for GameObjects (Netcode)
        // warning: "No ConnectionApproval callback defined. Connection approval will timeout"

        _networkManager.ConnectionApprovalCallback += ConnectionApprovalCheck;
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
        if (sceneEvent.SceneName == ServerGamePlayManager.GAME_PLAY_SCENE && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            Debug.Log("InitializePool");
            NetworkObjectPool.Singleton.InitializePool();
        }
        else if (sceneEvent.SceneName == ServerGamePlayManager.GAME_OVER_SCENE && sceneEvent.SceneEventType == SceneEventType.LoadComplete)
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
        if (_serverMultiplayManager != null)
        {
            // According to docs this should also disconnect remaining clients.
            // https://docs.unity.com/game-server-hosting/sdk/game-server-sdk-for-unity.html#Unready_the_game_server
            await _serverMultiplayManager.UnReadyServer();
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
            _serverGamePlayManager.LoadSingleModeScene(_networkManager.SceneManager, ServerGamePlayManager.GAME_OVER_SCENE);
        }
        else
        {
            Debug.Log("SceneManager was null");
        }
    }

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
