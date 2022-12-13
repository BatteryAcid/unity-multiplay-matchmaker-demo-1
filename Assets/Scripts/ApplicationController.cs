using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    private IGameManager _gameManager;
    private GameStateManager _gameStateManager;
    //private IMessagingManager _messagingManager;
    //private MessagingManager _messagingManager;
    public static bool IsServer = false;

    // TODO: SERVER ONLY
    MultiplayManager _multiplayManager;

    void Start()
    {
        //_messagingManager = FindObjectOfType<MessagingManager>();
        //if (_messagingManager == null)
        //{
        //    Debug.Log("messaging manager is null");
        //}

        // If this is a build and we are headless, we are a server
        // Note: could also add an ENV variable
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            // server
            Debug.Log("Server build running.");
            IsServer = true;

            _gameManager = FindObjectOfType<ServerGameManager>();//gameObject.AddComponent<ServerGameManager>();

            // TODO: probably better way to do this...
            _multiplayManager = gameObject.AddComponent<MultiplayManager>();
            _multiplayManager.Setup((ServerGameManager)_gameManager);

            //_messagingManager = gameObject.AddComponent<ClientMessagingManager>();



            //((ServerGameManager)_gameManager).Setup(_multiplayManager);

            // TODO: remove, only to test below line which wont be here.
            // _gameManager.Init("server"); 
            //((ServerGameManager)_gameManager).StartServer();
        }
        else
        {
            // client
            Debug.Log("Client build running.");
            _gameManager = gameObject.AddComponent<ClientGameManager>();

            //_messagingManager = gameObject.AddComponent<ServerMessagingManager>();
        }

        _gameStateManager = FindObjectOfType<GameStateManager>();
        _gameManager.Init(IsServer ? "server manager" : "client manager", _gameStateManager);//, _messagingManager);
    }

    void Update()
    {

    }
}
