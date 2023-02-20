using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

// Handles the matchmaker and client UnityService calls
public class ClientMultiplayManager : MonoBehaviour
{
    private AuthService _authService;

    private async Task SetupUnityServices()
    {
        try
        {
            // required to use Unity services like Matchmaker
            await UnityServices.InitializeAsync(); // Client SDK 1
        }
        catch (Exception e)
        {
            Debug.Log("UnityServices.InitializeAsync failed!");
            Debug.Log(e);
        }

        // Not using any authentication in this demo, so must set anonymous authentication
        await _authService.MockSignIn();
    }

    public async void Init(AuthService authService)
    {
        _authService = authService;


        // we basically avoid using this class if we are local testing without
        // using the Unity services.
        if (!ApplicationController.IsLocalTesting)
        {
            await SetupUnityServices();
        }
    }
}
