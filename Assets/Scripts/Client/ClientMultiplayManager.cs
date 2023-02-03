using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

// Handles the matchmaker and client UnityService calls
public class ClientMultiplayManager : MonoBehaviour
{
    private IMatchmaker Matchmaker;
    private AuthService _authService;

    public async Task<MatchmakingResult> FindMatch(UserData mockUserData)
    {
        return await Matchmaker.FindMatch(mockUserData);
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
        await _authService.MockSignIn();

        Matchmaker = new MatchplayMatchmaker();
    }

    public async void Init(AuthService authService)
    {
        _authService = authService;

        if (!ApplicationController.IsLocalTesting)
        {
            await SetupUnityServices();
        }
        else
        {
            LocalTestingSetup();
        }
    }

    // This matchmaker is a mock version for local testing when you want to
    // avoid hitting the real matchmaking service
    private void LocalTestingSetup()
    {
        Matchmaker = new MockMatchmaker();
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
