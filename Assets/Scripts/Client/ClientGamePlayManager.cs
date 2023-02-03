using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script handles the game play specific functionality
public class ClientGamePlayManager : MonoBehaviour
{
    private AuthService _authService;
    private UIManager _UIManager;
    private ClientNetworkManager _clientNetworkManager;
    private ClientMultiplayManager _clientMultiplayManager;

    public async void FindMatch()
    {
        MatchmakingResult matchmakingResult = await FindMatchAsync();

        if (matchmakingResult.result == MatchmakerPollingResult.Success)
        {
            // once we get the match ip and port we can connect to server
            _clientNetworkManager.StartConnectionToServer(matchmakingResult);
        }
        else
        {
            Debug.Log("Failed to find match.");
        }
    }

    private async Task<MatchmakingResult> FindMatchAsync()
    {
        UserData mockUserData = _authService.MockGetUserData();

        Debug.Log($"Start matchmaking with {mockUserData}");
        MatchmakingResult matchmakingResult = await _clientMultiplayManager.FindMatch(mockUserData);

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

    private void OnNonNetworkSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene")
        {
            // Wait until main scene is loaded to look for the UIManager, as the
            // game starts with the Startup scene loaded, which doesn't have the UiManager.
            _UIManager = GameObject.Find("UIManager").GetComponent<UIManager>();
            _UIManager.Init(FindMatch);
        }
    }

    public void Init(AuthService authService, ClientNetworkManager clientNetworkManager, ClientMultiplayManager clientMultiplayManager)
    {
        SceneManager.sceneLoaded += OnNonNetworkSceneLoaded;

        _authService = authService;
        _clientNetworkManager = clientNetworkManager;
        _clientMultiplayManager = clientMultiplayManager;
    }
}
