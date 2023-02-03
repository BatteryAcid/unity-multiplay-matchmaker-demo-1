using UnityEngine;
using UnityEngine.SceneManagement;

// Client side dependency setup
public class ClientGameManager : MonoBehaviour, IGameManager
{
    private ClientGamePlayManager _clientGamePlayManager;
    private ClientNetworkManager _clientNetworkManager;
    private AuthService _authService;
    private ClientMultiplayManager _clientMultiplayManager;

    public void Init()
    {
        Debug.Log("ClientGameManager Init");

        // Here we add all our client dependencies to the GameManger game object.
        // You could use a dependency injection framework to streamline all this.
        _authService = gameObject.AddComponent<AuthService>();
        _clientGamePlayManager = gameObject.AddComponent<ClientGamePlayManager>();
        _clientNetworkManager = gameObject.AddComponent<ClientNetworkManager>();
        _clientMultiplayManager = gameObject.AddComponent<ClientMultiplayManager>();

        _clientGamePlayManager.Init(_authService, _clientNetworkManager, _clientMultiplayManager);
        _clientMultiplayManager.Init(_authService);
    }

    void Start()
    {
        Debug.Log("ClientGameManager start");

        // NOTE: because we manually add this class to a game object, we
        // can call Init() before Start is called.
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
