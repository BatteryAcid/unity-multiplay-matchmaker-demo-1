using UnityEngine;
using Unity.Netcode;

// Server side dependency setup
public class ServerGameManager : MonoBehaviour, IGameManager
{
    private ServerGamePlayManager _serverGamePlayManager;
    private ServerNetworkManager _serverNetworkManager;
    private ServerMultiplayManager _serverMultiplayManager;

    public void Init()
    {
        Debug.Log("ServerGameManager Init");

        // Here we add all our server dependencies to the GameManger game object.
        // You could use a dependency injection framework to streamline all this.
        _serverGamePlayManager = gameObject.AddComponent<ServerGamePlayManager>();
        _serverNetworkManager = gameObject.AddComponent<ServerNetworkManager>();

        // Don't need Multiplay services when local testing
        if (!ApplicationController.IsLocalTesting)
        {
            _serverMultiplayManager = gameObject.AddComponent<ServerMultiplayManager>();
            _serverMultiplayManager.Init(_serverNetworkManager.StartServer);

            _serverNetworkManager.Init(_serverGamePlayManager, _serverMultiplayManager, _serverMultiplayManager.UpdatePlayerCount);
        }
        else
        {
            // local testing, so not using Unity Multiplay services
            _serverNetworkManager.Init(_serverGamePlayManager, null, null);
        }
    }

    void Start()
    {
        Debug.Log("ServerGameManager start");

        // NOTE: because we manually add this class to a game object, we
        // can call Init() before Start is called.
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
