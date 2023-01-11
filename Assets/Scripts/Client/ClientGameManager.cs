using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Matchmaker.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;

public class ClientGameManager : MonoBehaviour, IGameManager
{
    public IMatchmaker Matchmaker { get; private set; }

    private Button _findMatchButton;
    private MatchButtonStatusEnum _matchButtonStatus = MatchButtonStatusEnum.FindMatch;
    private NetworkManager _networkManager;
    private enum MatchButtonStatusEnum
    {
        FindMatch,
        Searching,
        Found,
        HoldLastStatus
    }

    // Time in seconds before the client considers a lack of server response a timeout
    private const int k_TimeoutDuration = 10;

    private async Task<MatchmakingResult> FindMatchAsync()
    {
        Debug.Log("Start matchmaking...");

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

    private void StartConnectionToServer(MatchmakingResult matchmakingResult)
    {
        Debug.Log($"Connecting to server at {matchmakingResult.ip}:{matchmakingResult.port}");

        // Set the server ip and port to connect to, received from our match making result.
        var unityTransport = _networkManager.gameObject.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData(matchmakingResult.ip, (ushort) matchmakingResult.port);

        ConnectClient();
    }

    private void ConnectClient()
    {
        // NOTE: For this demo no data is being sent upon connection, but you could do it with the example code below.
        // var userData = <some_user_data_source>;
        // var payload = JsonUtility.ToJson(userData);
        // var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        // _networkManager.NetworkConfig.ConnectionData = payloadBytes;

        _networkManager.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

        //  If the socket connection fails, we'll hear back by getting an ReceiveLocalClientDisconnectStatus callback
        //  for ourselves and get a message telling us the reason. If the socket connection succeeds, we'll get our
        //  ReceiveLocalClientConnectStatus callback This is where game-layer failures will be reported.
        if (_networkManager.StartClient())
        {
            Debug.Log("Starting Client!");
        }
        else
        {
            Debug.LogWarning($"Could not Start Client!");
            //TODO: ?? OnLocalDisconnection?.Invoke(ConnectStatus.Undefined);
        }
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

    public void Init(string setting)
    {
        Debug.Log($"Init {setting}");

        _networkManager = NetworkManager.Singleton;
        _networkManager.OnClientDisconnectCallback += RemoteDisconnect;
    }

    private async Task SetupUnityServices()
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
            await SetupUnityServices();
        }
        else
        {
            LocalTestingSetup();
        }
    }

    private void SetNonInteractiveFindMatchButtonStatus(string buttonText)
    {
        _matchButtonStatus = MatchButtonStatusEnum.HoldLastStatus;
        _findMatchButton.enabled = false;
        _findMatchButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = buttonText;
    }

    void RemoteDisconnect(ulong clientId)
    {
        Debug.Log($"Got Client Disconnect callback for {clientId}");
        //if (clientId == m_NetworkManager.LocalClientId)
        //    return;
        //NetworkShutdown();
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

    // TODO: move to it's own script
    private UserData MockGetUserData()
    {
        // Note: this random number is just for illustrative purposes, in production you would use the actual
        // username and whatever authorization id your project requires.
        System.Random rnd = new System.Random();
        int num = rnd.Next();
        return new UserData("username" + num, "authid" + num, (ulong)num);
    }

    private async Task MockSignIn()
    {
        // NOTE: not addressing authentication in this demo, users will be anonymous
        if (AuthenticationService.Instance.IsSignedIn) { return; }
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
        // Debug.Log($"Player token: {AuthenticationService.Instance.AccessToken}");
    }

    private void LocalTestingSetup()
    {
        Matchmaker = new MockMatchmaker();
    }
}
