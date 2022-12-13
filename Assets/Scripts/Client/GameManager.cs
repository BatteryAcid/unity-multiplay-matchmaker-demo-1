using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Unity.Services.Matchmaker.Models;
using Unity.Netcode;

// TODO: removing this class in favor of ServerGameManager
public class GameManager : MonoBehaviour
{
    private Button _findMatchButton;
    private enum MatchButtonStatusEnum
    {
        FindMatch,
        Searching,
        Found,
        HoldLastStatus
    }
    private MatchButtonStatusEnum _matchButtonStatus = MatchButtonStatusEnum.FindMatch;
    //private NetworkClient _networkClient;

    public MatchplayMatchmaker Matchmaker { get; private set; }

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

    async void Start()
    {
        //_networkClient = GetComponent<NetworkClient>();
        //_networkClient.StartGameEventHandler += OnStartGameEvent;

        // setup the find match button 
        _findMatchButton = GameObject.Find("FindMatch").GetComponent<Button>();
        _findMatchButton.onClick.AddListener(OnFindMatchPressed);

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

    private void StartConnectionToServer(MatchmakingResult matchmakingResult)
    {
        //_networkClient.ConnectToServer(matchmakingResult.ip, matchmakingResult.port, "");
    }

    // handles start game events propagated from the servers start message
    void OnStartGameEvent(object sender, StartGameArgs startGameArgs)
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene("GamePlay");
    }

    // Kick off the match making service
    public async void OnFindMatchPressed()
    {
        _matchButtonStatus = MatchButtonStatusEnum.Searching;

        Matchmaker = new MatchplayMatchmaker();
        MatchmakingResult matchmakingResult = await FindMatchAsync();

        // once we get the match ip and port we can connect to server
        StartConnectionToServer(matchmakingResult);
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

    private void SetNonInteractiveFindMatchButtonStatus(string buttonText)
    {
        _matchButtonStatus = MatchButtonStatusEnum.HoldLastStatus;
        _findMatchButton.enabled = false;
        _findMatchButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = buttonText;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationQuit()
    {
        if (Matchmaker != null)
        {
            // must do this to kill the matchmaking polling
            Matchmaker.Dispose();
        }
    }
}

//public class StartGameArgs : EventArgs
//{
//    public StartGameArgs() { }
//}