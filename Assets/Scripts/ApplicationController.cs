using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    // NOTE: this probably isn't the best way to do this, you'd want to use a
    // command line argument or a Define Symbol to toggle deployment environment.
    public static bool IsLocalTesting = true;
    public static bool IsServer = false;

    // shared
    private IGameManager _gameManager;
    private GameStateManager _gameStateManager;

    // server only
    private MultiplayManager _multiplayManager;

    private void ServerSetup()
    {
        Debug.Log("Server build running.");
        IsServer = true;

        _gameManager = FindObjectOfType<ServerGameManager>();

        // Don't need Multiplay services when local testing
        if (!IsLocalTesting)
        {
            // TODO: probably better way to do this...
            _multiplayManager = gameObject.AddComponent<MultiplayManager>();
            _multiplayManager.Setup((ServerGameManager)_gameManager);
        }
    }

    private void ClientSetup()
    {
        Debug.Log("Client build running.");
        _gameManager = gameObject.AddComponent<ClientGameManager>();
    }

    void Start()
    {
        // NOTE: this is one way to test if we are a server build or not.
        // You can also use environment variables or Define Symbols
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            ServerSetup();
        }
        else
        {
            ClientSetup();
        }

        _gameStateManager = FindObjectOfType<GameStateManager>();
        _gameManager.Init(IsServer ? "server manager" : "client manager", _gameStateManager);
    }
}
