using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using System.Threading.Tasks;

public class PlayerController : NetworkBehaviour
{
    public NetworkVariable<bool> IsPlayerHit = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> Throwing = new NetworkVariable<bool>(false);
    public NetworkVariable<int> NumberOfHits = new NetworkVariable<int>(0);
    public NetworkVariable<bool> IsPlayerDesignationSet = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> PlayerDesignation = new NetworkVariable<FixedString32Bytes>();

    public Rigidbody Player;
    public GameObject referencePrefabBall;

    private ServerGamePlayManager _serverGamePlayManager;
    private Transform PlayerCamera;
    private ThirdPersonCameraController _thirdPersonCameraController;

    private BallActionHandler _ballActionHandler;
    private float baseBallThrust = 20.0f;
    private float _throwKeyPressedStartTime = 0f;

    private float _maxSpeed = 8;
    private float _movementSpeedWhileThrowing = 0.5f;
    private float _currentSpeedLimit = 0;
    private float inputHorX, inputVertY;
    private const float _minX = -9.5f, _maxX = 9.5f, _minZp1 = 5f, _maxZp1 = 10f, _minZp2 = -10f, _maxZp2 = -5f;

    private bool _isPlayerControllerReady = false;
    private bool _isGameOver = false;

    private Color _originalPlayerColor;
    private Renderer _playerRenderer;

    [ServerRpc]
    public void SetThrowingServerRpc()
    {
        Throwing.Value = true;
    }

    [ServerRpc]
    public void ThrowBallServerRpc(Vector3 cameraForwardVector, float throwKeyPressedTime, ServerRpcParams serverRpcParams = default)
    {
        // Debug.Log("ThrowBallServerRpc: " + cameraForwardVector.ToString() + ", throwKeyPressedTime: " + throwKeyPressedTime);
        _ballActionHandler.ThrowBall(cameraForwardVector, throwKeyPressedTime, Player, PlayerDesignation.Value.ToString());
        Throwing.Value = false;
    }

    private async void LocalPlayerClient_Update()
    {
        if (IsLocalPlayer)
        {
            if (!IsPlayerDesignationSet.Value && PlayerDesignation.Value != "")
            {
                Debug.Log("Player id set: " + PlayerDesignation.Value);

                IsPlayerDesignationSet.Value = true;
            }

            // limit player speed
            if (Player.velocity.magnitude > _maxSpeed)
            {
                Player.velocity = Vector3.ClampMagnitude(Player.velocity, _maxSpeed);
            }

            // actual player update is performed in FixedUpdate
            inputHorX = Input.GetAxis("Horizontal");
            inputVertY = Input.GetAxis("Vertical");

            if (_isPlayerControllerReady && Input.GetMouseButtonDown(0))
            {
                _throwKeyPressedStartTime = Time.time;
                SetThrowingServerRpc();
            }

            if (_isPlayerControllerReady && Input.GetMouseButtonUp(0))
            {
                // Server RPC throw ball
                float throwKeyPressedTime = Time.time - _throwKeyPressedStartTime;

                // This is here to allow for a couple extra frames of player movement at the "throw speed",
                // so the server has a chance to buffer a few frames of player position at that speed.  
                // This helps keep the ball spawn placement closer to the player versus if we tried to spawn
                // it while player was at full speed. In that case, it could be off by a meter or so on the
                // client's local screen because of the round trip latency.
                // You could hide this lag even more with a fun throw animation or something.
                await WaitThenThrow();

                ThrowBallServerRpc(PlayerCamera.forward, throwKeyPressedTime);
            }

            if (!_isPlayerControllerReady && IsPlayerDesignationSet.Value && PlayerCamera != null)
            {
                // we have our player designation and camera, ready for play
                _isPlayerControllerReady = true;
            }
        }
    }

    private void ClientAndServer_Update()
    {
        // Run on both client and server to make sure we clamp our player object
        if (_isPlayerControllerReady)
        {
            // clamp x and z position values to keep player in bounds of plane
            if (PlayerDesignation.Value == "player1")
            {
                // blue
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, _minX, _maxX), 1, Mathf.Clamp(transform.position.z, _minZp1, _maxZp1));
            }
            else if (PlayerDesignation.Value == "player2")
            {
                // orange
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, _minX, _maxX), 1, Mathf.Clamp(transform.position.z, _minZp2, _maxZp2));
            }
        }
    }

    void Update()
    {
        LocalPlayerClient_Update();

        ClientAndServer_Update();
    }

    private void PlayerMovement(float x, float y)
    {
        Vector3 playerMovementRotation = new Vector3(x, 0f, y) * _currentSpeedLimit;

        Vector3 camRotation = PlayerCamera.transform.forward;
        camRotation.y = 0f; // zero out camera's vertical axis so it doesn't make them fly

        // need to clamp camera rotation to x/z only and not y vertical 
        Vector3 playerMovementWithCameraRotation = Quaternion.LookRotation(camRotation) * playerMovementRotation;

        // rounded to two decimal places
        Vector3 roundedVelocity
           = new Vector3(Mathf.Round(playerMovementWithCameraRotation.x * 100f) / 100f, 0f, Mathf.Round(playerMovementWithCameraRotation.z * 100f) / 100f);

        // Debug.Log("velocity to send: " + roundedVelocity.ToString("f6"));

        Player.AddForce(roundedVelocity, ForceMode.VelocityChange);
    }

    // This is linked to the project settings under Time > Fixed Timestamp
    // Currently set to .02 seconds, which is 20ms
    void FixedUpdate()
    {
        if (Throwing.Value == true)
        {
            _currentSpeedLimit = _movementSpeedWhileThrowing;
        }
        else
        {
            _currentSpeedLimit = _maxSpeed;
        }

        if (IsLocalPlayer && _isPlayerControllerReady && !_isGameOver)
        {
            PlayerMovement(inputHorX, inputVertY);
        }
    }


    private void LocalPlayerClient_Start()
    {
        if (IsLocalPlayer)
        {
            Debug.Log("PlayerDesignation: " + PlayerDesignation.Value);
            _isGameOver = false;
        }
    }

    private void Client_Start()
    {
        if (IsClient)
        {
            // These on change callbacks need to be watched for both local player and client representation of other player
            NetworkManager.SceneManager.OnSceneEvent += OnSceneLoaded;
            IsPlayerHit.OnValueChanged += OnIsPlayerHitChanged;
        }
    }

    private void Server_Start()
    {
        if (IsServer)
        {
            _serverGamePlayManager = GameObject.Find("GameManager").GetComponent<ServerGamePlayManager>();

            _ballActionHandler = gameObject.GetComponent<BallActionHandler>();
            _ballActionHandler.Init(referencePrefabBall, baseBallThrust, _serverGamePlayManager.PlayerHit);
        }
    }

    void Start()
    {
        Debug.Log("PlayerController start");

        LocalPlayerClient_Start();
        Client_Start();
        Server_Start();
    }

    // client side only
    private void OnSceneLoaded(SceneEvent sceneEvent)
    {
        Debug.Log($"OnSceneLoaded: {sceneEvent.SceneName}, event: {sceneEvent.SceneEventType}");
        if (sceneEvent.SceneName == "GamePlay" && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            if (IsLocalPlayer)
            {
                SetupCamera();
            }

            // we don't have access to this until the scene loads
            _playerRenderer = Player.GetComponent<Renderer>();
            _originalPlayerColor = _playerRenderer.material.color;
        }
        else if (sceneEvent.SceneName == ServerGamePlayManager.GAME_OVER_SCENE && sceneEvent.SceneEventType == SceneEventType.Load)
        {
            // Prevents the PlayerMovement function from handling player input so we don't get errors client side
            // once the player object is despawned by server.
            _isGameOver = true;
        }
    }

    private void SetupCamera()
    {
        Camera[] allCams = Camera.allCameras;

        // find the game play camera
        foreach (Camera camera in allCams)
        {
            // Debug.Log($"camera tag: {camera.tag}");

            if (camera.tag == "GamePlayCamera")
            {
                // Debug.Log("GamePlayCamera found");
                if (!camera.isActiveAndEnabled)
                {
                    // Debug.Log("Camera was not active and enabled, enabling...");
                    camera.enabled = true;
                }

                PlayerCamera = camera.transform;
                break;
            }
        }

        // Manually add the ThirdPersonCameraController to the Camera object
        _thirdPersonCameraController = PlayerCamera.gameObject.AddComponent<ThirdPersonCameraController>();
        _thirdPersonCameraController.PlayerTransform = Player.transform;
        _thirdPersonCameraController.PlayerDesignation = PlayerDesignation.Value.ToString();
    }


    // client side only
    public void OnIsPlayerHitChanged(bool previous, bool current)
    {
        if (current)
        {
            ApplyHitColor(true);
        }
        else
        {
            ApplyHitColor(false);
        }
    }

    private void ApplyHitColor(bool isHit)
    {
        if (isHit)
        {
            _playerRenderer.material.SetColor("_Color", Color.red);
        }
        else
        {
            _playerRenderer.material.SetColor("_Color", _originalPlayerColor);
        }
    }

    private async Task WaitThenThrow()
    {
        await Task.Delay(300);
    }
}
