using UnityEngine;
using UnityEngine.SceneManagement;

// Loads as part of the startup scene
public class ApplicationController : MonoBehaviour
{
    // NOTE: Using IsLocalTesting to signal that we are not using Multiplay or
    // matchmaking services, probably isn't the best way. Instead you'd want to
    // use a command line argument or a Define Symbol to toggle deployment environment.
    public static bool IsLocalTesting = false;

    // The server or client IGameManager implementation script will be added to
    // the GameManager object in the editor and establish the respective support scripts.
    private IGameManager _gameManager;

    public const string QueueName = "bad-queue-1";

    void Start()
    {
        GameObject gameManager = GameObject.Find("GameManager");

        // NOTE: this is one way to test if we are a server build or not.
        // You can also use environment variables or Define Symbols.
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("Server build running.");
            // Add necessary server scripts to GameManager
            _gameManager = gameManager.AddComponent<ServerGameManager>();
        }
        else
        {
            Debug.Log("Client build running.");
            // Add necessary client scripts to GameManager
            _gameManager = gameManager.AddComponent<ClientGameManager>();
        }

        // Initialize our classes instead of relying on start
        _gameManager.Init();

        // Load main scene after startup is complete
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
