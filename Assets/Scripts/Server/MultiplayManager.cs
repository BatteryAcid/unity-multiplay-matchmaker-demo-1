using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Services.Matchmaker.Models;
using Unity.Netcode;
using UnityEngine;

public class MultiplayManager : MonoBehaviour
{
    public bool QueryProtocol = false;
    public bool Matchmaking = false;
    public bool Backfill = false;

    private string _allocationId;
    private int _maxPlayers = 2; // TODO: This can be hardcoded or dynamic based on your games requirements.
    private bool _sqpInitialized = false;
    private MultiplayEventCallbacks _multiplayEventCallbacks;
    private IServerEvents _serverEvents;
    private IServerQueryHandler _serverQueryHandler;
    private ServerGameManager _serverGameManager; // server used to communicate with client

    private async void StartGame()
    {
        if (_serverGameManager == null)
        {
            // TODO: remove this check once testing is over
            Debug.Log("server is null");
        }
        else
        {
            Debug.Log("Starting server...");

            _serverGameManager.StartServer();

            // this example game doesn't have anything else to setup or assets to load, inform multiplay we are ready to accept connections
            await MultiplayService.Instance.ReadyServerForPlayersAsync();
        }
    }

    // Unity Multiplay-matchmaking services calls this function once the server is ready.
    // At this point we can start the game, which means starting our network server.
    private void OnAllocate(MultiplayAllocation allocation)
    {
        Debug.Log("Server Allocated.");

        if (allocation != null)
        {
            if (string.IsNullOrEmpty(allocation.AllocationId))
            {
                Debug.LogError("Allocation id was null");
                return;
            }
            _allocationId = allocation.AllocationId;
            Debug.Log("Allocation id: " + _allocationId);

            StartGame();
        }
        else
        {
            Debug.LogError("Allocation was null");
        }
    }

    private async void InitializeSqp()
    {
        Debug.Log("BAD InitializeSqp");

        // Note: looks like this can be set with default values first, then updated if the values come in later,
        // which should be followed by a call to UpdateServerCheck
        // SDK 3
        try
        {
            _serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
                (ushort)_maxPlayers, //(ushort)_sessionRequest.MaxPlayers,
                "DisplayName", //_sessionRequest.DisplayName,
                "BADGameType", //_sessionRequest.GameplayType.ToString().ToLowerInvariant(),
                "0",//Application.version,
                "BADMap");

            // TODO: for testing just added one player
            // TODO: refactor this to an actual count based on players joining
            _serverQueryHandler.CurrentPlayers = (ushort)1;

            _sqpInitialized = true; // triggers a call to UpdateServerCheck
        }
        catch (Exception e)
        {
            Debug.LogError("Exception why running StartServerQueryHandlerAsync: " + e.Message);
        }
    }

    private void Update()
    {
        // should just run once
        if (!_sqpInitialized) InitializeSqp();

        // always gets called here
        if (_serverQueryHandler != null)
        {
            _serverQueryHandler.UpdateServerCheck(); // SDK 4
        }
    }

    // TODO: maybe the SDK calls shouldn't be here, probably should be on "Find match" click
    // TODO: this should probably be an interface where we don't care which server implementation we're using, just used to start server.
    public async void Init(ServerGameManager server)
    {
        // set reference to our network server
        _serverGameManager = server;

        if (_serverGameManager == null)
        {
            Debug.Log("BADNetworkManager is null, did you forget to add it to the GameManager game object?");
        }

        // Moved this here above the SQP setup just to try it out
        // SDK 2
        _serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(_multiplayEventCallbacks);


        // It's possible this is a cold start allocation (no event will fire)
        if (!string.IsNullOrEmpty(MultiplayService.Instance.ServerConfig.AllocationId))
        {
            Debug.Log("Looks like we have a 'cold start allocation', where no event will fire...");
            Debug.Log(MultiplayService.Instance.ServerConfig.AllocationId);

            // in this case start game here instead of waiting on allocation event
            StartGame();
        }
    }

    private void Start()
    {
        Debug.Log("MultiplayManager.Start");

        //Debug.Log(Application.targetFrameRate);
        //Debug.Log(QualitySettings.vSyncCount);

        // Fixes issue with excessive CPU, not sure if vSyncCount is necessary.
        // https://docs.unity.com/game-server-hosting/guides/troubleshooting.html#Servers_using_too_much_CPU
        // TODO: Try setting NetworkManager to match this tick rate 60 and see if latency improves
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        // Debug.Log(Application.targetFrameRate);
        // Debug.Log(QualitySettings.vSyncCount);

        // Setup allocations
        _multiplayEventCallbacks = new MultiplayEventCallbacks();
        _multiplayEventCallbacks.Allocate += OnAllocate;
        _multiplayEventCallbacks.Deallocate += OnDeallocate;
        _multiplayEventCallbacks.Error += OnError;
        _multiplayEventCallbacks.SubscriptionStateChanged += OnSubscriptionStateChanged;
    }

    private async void Awake()
    {
        Debug.Log("MultiplayManager.Awake");

        try
        {
            // Call Initialize async from SDK
            await UnityServices.InitializeAsync(); // SDK 1
        }
        catch (Exception e)
        {
            Debug.Log("InitializeAsync failed!");
            Debug.Log(e);
        }
    }

    // TODO: not sure what this is for yet...
    private void OnSubscriptionStateChanged(MultiplayServerSubscriptionState state)
    {
        switch (state)
        {
            case MultiplayServerSubscriptionState.Unsubscribed: /* The Server Events subscription has been unsubscribed from. */
                Debug.Log("Unsubscribed");
                break;
            case MultiplayServerSubscriptionState.Synced: /* The Server Events subscription is up to date and active. */
                Debug.Log("Synced");
                break;
            case MultiplayServerSubscriptionState.Unsynced: /* The Server Events subscription has fallen out of sync, the subscription will attempt to automatically recover. */
                Debug.Log("Unsynced");
                break;
            case MultiplayServerSubscriptionState.Error: /* The Server Events subscription has fallen into an errored state and will not recover automatically. */
                Debug.Log("Error");
                break;
            case MultiplayServerSubscriptionState.Subscribing: /* The Server Events subscription is attempting to sync. */
                Debug.Log("Subscribing");
                break;
        }
    }

    private void OnError(MultiplayError error)
    {
        Debug.Log("MultiplayManager OnError: " + error.Reason);
    }

    private void OnDeallocate(MultiplayDeallocation deallocation)
    {
        Debug.Log("Deallocated");

        //TODO: Hack for now, just exit the application on deallocate
        Application.Quit();
    }
}
