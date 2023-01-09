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
using System.Net;


public class ClientGameManager : MonoBehaviour, IGameManager
{
    private GameStateManager _gameStateManager;

    private NetworkManager _networkManager;
    //private MessagingManager _messagingManager; // for calling to the server

    /// <summary>
    /// Time in seconds before the client considers a lack of server response a timeout
    /// </summary>
    const int k_TimeoutDuration = 10;

    private Button _findMatchButton;
    private enum MatchButtonStatusEnum
    {
        FindMatch,
        Searching,
        Found,
        HoldLastStatus
    }
    private MatchButtonStatusEnum _matchButtonStatus = MatchButtonStatusEnum.FindMatch;
    public IMatchmaker Matchmaker { get; private set; }
    //public MatchplayMatchmaker Matchmaker { get; private set; }

    public void Init(string setting, GameStateManager gameStateManager)//, MessagingManager messagingManager)
    {
        Debug.Log($"Init {setting}");
        _gameStateManager = gameStateManager;

        // TODO: can provably remove after testing
        if (_gameStateManager == null)
        {
            Debug.Log("_gameStateManager is null");
        }
        _gameStateManager.CurrentGameState.OnValueChanged += OnGameStateChange;


        //_messagingManager = messagingManager; // (ServerMessagingManager) messagingManager;

        _networkManager = NetworkManager.Singleton;
        _networkManager.OnClientDisconnectCallback += RemoteDisconnect;
    }

    public void OnGameStateChange(GameState previousState, GameState currentState)
    {
        Debug.Log($"Game state changed from {previousState} to {currentState}.");
        if (currentState == GameState.started)
        {
            //OnStartGameEvent();
        }
    }

    void RemoteDisconnect(ulong clientId)
    {
        Debug.Log($"Got Client Disconnect callback for {clientId}");
        //if (clientId == m_NetworkManager.LocalClientId)
        //    return;
        //NetworkShutdown();
    }

    private async Task<MatchmakingResult> FindMatchAsync()
    {
        Debug.Log("start matchmaking");

        UserData mockUserData = MockGetUserData();

        Debug.Log($"Start matchmaking with {mockUserData}");
        MatchmakingResult matchmakingResult = await Matchmaker.FindMatch(mockUserData);

        if (matchmakingResult.result == MatchmakerPollingResult.Success)
        {
            // Here you can see the matchmaker returns the server ip and port the client needs to connect to
            Debug.Log($"{matchmakingResult.result}, {matchmakingResult.resultMessage}, {matchmakingResult.ip}:{matchmakingResult.port}  ");
        }
        else
        {
            Debug.LogWarning($"{matchmakingResult.result} : {matchmakingResult.resultMessage}");
        }

        return matchmakingResult;
    }

    private void LocalTestingSetup()
    {
        Matchmaker = new MockMatchmaker();
    }

    private async Task Setup()
    {
        try
        {
            // Call Initialize async from SDK
            await UnityServices.InitializeAsync(); // SDK 1
        }
        catch (Exception e)
        {
            Debug.Log("UnityServices.InitializeAsync failed!");
            Debug.Log(e);
        }

        // Not using any authentication in this demo, so must set anonymous authentication
        await MockSignIn();

        Matchmaker = new MatchplayMatchmaker();
    }

    // Start is called before the first frame update
    async void Start()
    {
        Debug.Log("ClientGameManager start");

        // setup the find match button 
        _findMatchButton = GameObject.Find("FindMatch").GetComponent<Button>();
        _findMatchButton.onClick.AddListener(OnFindMatchPressed);

        if (!ApplicationController.IsLocalTesting)
        {
            await Setup();
        }
        else
        {
            LocalTestingSetup();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_matchButtonStatus == MatchButtonStatusEnum.Searching)
        {
            SetNonInteractiveFindMatchButtonStatus("Searching...");
        }
        if (_matchButtonStatus == MatchButtonStatusEnum.Found)
        {
            SetNonInteractiveFindMatchButtonStatus("Match Found!");
        }
    }

    private void StartConnectionToServer(MatchmakingResult matchmakingResult)
    {
        // TODO: old telepathy remove
        //_networkClient.ConnectToServer(matchmakingResult.ip, matchmakingResult.port, "");

        Debug.Log($"Starting server at {matchmakingResult.ip}:{matchmakingResult.port}"); //\nWith: {startingGameInfo}");

        // TODO: need to add client connection here
        var unityTransport = _networkManager.gameObject.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData(matchmakingResult.ip, (ushort)matchmakingResult.port);
        ConnectClient();
    }

    /// <summary>
    /// Sends some additional data to the server about the client and begins connecting them.
    /// </summary>
    void ConnectClient()
    {
        //var userData = ClientSingleton.Instance.Manager.User.Data;
        //var payload = JsonUtility.ToJson(userData);

        //var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        //_networkManager.NetworkConfig.ConnectionData = payloadBytes;
        _networkManager.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

        //  If the socket connection fails, we'll hear back by getting an ReceiveLocalClientDisconnectStatus callback for ourselves and get a message telling us the reason
        //  If the socket connection succeeds, we'll get our  ReceiveLocalClientConnectStatus callback This is where game-layer failures will be reported.
        if (_networkManager.StartClient())
        {
            Debug.Log("Starting Client!");
            //MatchplayNetworkMessenger.RegisterListener(NetworkMessage.LocalClientConnected,
            //    ReceiveLocalClientConnectStatus);
            //MatchplayNetworkMessenger.RegisterListener(NetworkMessage.LocalClientDisconnected,
            //    ReceiveLocalClientDisconnectStatus);
        }
        else
        {
            Debug.LogWarning($"Could not Start Client!");
            //OnLocalDisconnection?.Invoke(ConnectStatus.Undefined);
        }
    }

    // TODO: this can be removed, our server will init the scene change
    // TODO: if this works out, rename
    // handles start game events propagated from the servers start message
    public void OnStartGameEvent() //TODO remove if not using events... object sender, StartGameArgs startGameArgs)
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene("GamePlay"); // TODO: make constant 

        //_messagingManager.ConfirmGameStartServerRpc();
    }

    // Kick off the match making service
    public async void OnFindMatchPressed()
    {
        _matchButtonStatus = MatchButtonStatusEnum.Searching;

        //Matchmaker = new MatchplayMatchmaker();
        MatchmakingResult matchmakingResult = await FindMatchAsync();

        // once we get the match ip and port we can connect to server
        StartConnectionToServer(matchmakingResult);
    }

    private void SetNonInteractiveFindMatchButtonStatus(string buttonText)
    {
        _matchButtonStatus = MatchButtonStatusEnum.HoldLastStatus;
        _findMatchButton.enabled = false;
        _findMatchButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = buttonText;
    }

    void OnApplicationQuit()
    {
        if (Matchmaker != null)
        {
            // must do this to kill the matchmaking polling
            Matchmaker.Dispose();
        }
        Dispose();
    }

    public void Dispose()
    {
        if (_networkManager != null)
        {
            _networkManager.OnClientDisconnectCallback -= RemoteDisconnect;
        }
    }

    private UserData MockGetUserData()
    {
        // Note: this random number is just for illustrative purposes, in production you would use the actual
        // username and whatever authorization id your project requires.
        System.Random rnd = new System.Random();
        int num = rnd.Next();
        return new UserData("username" + num, "authid" + num, (ulong)num);
    }

    async Task MockSignIn()
    {
        // NOTE: not addressing authentication in this demo, users will be anonymous
        if (AuthenticationService.Instance.IsSignedIn) { return; }
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
        // Debug.Log($"Player token: {AuthenticationService.Instance.AccessToken}");
    }
}
