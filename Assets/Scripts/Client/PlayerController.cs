using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

// TODO: can this be disabled for non-local player?
public class PlayerController : NetworkBehaviour
{
    public Rigidbody Player;
    // TODO: probably make private as we're no longer setting in editor.
    public Transform PlayerCamera;
    public GameObject referencePrefabBall;
    public float baseBallThrust = 20.0f;

    private float _throwKeyPressedStartTime = 0f;
    private BallActionHandler _ballActionHandler;

    private float maxSpeed = 10;
    private float inputHorX, inputVertY;

    public NetworkVariable<FixedString32Bytes> playerDesignation = new NetworkVariable<FixedString32Bytes>();
    private bool _isPlayerDesignationSet = false;
    private bool _isPlayerControllerReady = false;

    private const float _minX = -9.5f, _maxX = 9.5f, _minZp1 = 5f, _maxZp1 = 10f, _minZp2 = -10f, _maxZp2 = -5f;

    // NOTE: the local version of this object hits OnNetworkSpawn first, then Start.
    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkbehavior#spawning
    public override void OnNetworkSpawn()
    {
        Debug.Log("PlayerController OnNetworkSpawn");
        base.OnNetworkSpawn();

        if (IsLocalPlayer)
        {
            Debug.Log("LocalPlayer/OnNetworkSpawn: NetworkVariable Player id: " + playerDesignation.Value);

            // TODO: this doesn't seem to work here when using a custom non-main camera, it's just not ready here.
            //SetupCamera();
        }
    }

    private void SetupCamera()
    {
        // find the game play camera
        Camera[] allCams = Camera.allCameras;
        Debug.Log("AllCams: " + allCams.Length);
        foreach (Camera camera in allCams)
        {
            Debug.Log($"camera tag: {camera.tag}");

            if (!camera.isActiveAndEnabled)
            {
                Debug.Log("Camera was not active and enabled, enabling...");
                camera.enabled = true;
            }

            if (camera.tag == "GamePlayCamera")
            {
                Debug.Log("GamePlayCamera found");
                PlayerCamera = camera.transform;
                break;
            }
            else
            {
                // TODO: just for debuggging, set it to whatever camera is there bc it should only be one for now
                PlayerCamera = camera.transform;
            }
        }

        MonoBehaviour[] monoBehaviours = PlayerCamera.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in monoBehaviours)
        {
            if (behaviour is ThirdPersonCameraController)
            {
                Debug.Log("monoBehaviour is ThirdPersonCameraController with designation: " + playerDesignation.Value.ToString());
                ((ThirdPersonCameraController)behaviour).PlayerTransform = Player.transform;
                ((ThirdPersonCameraController)behaviour).PlayerDesignation = playerDesignation.Value.ToString();
                break;
            }
            else
            {
                Debug.Log("monoBehaviour NOT ThirdPersonCameraController");
            }
        }
    }

    // NOTE: only for local
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("PlayerController.OnSceneLoaded: " + scene.name);
        if (scene.name == "GamePlay")
        {
            SetupCamera();
        }
    }

    void Start()
    {
        // NetworkManager.LocalClient.PlayerObject // to get local client player object
        // NetworkManager.Singleton.LocalClientId You can pass this to an RPC that uses the ConnectedClients server side to retrieve the client

        Debug.Log("PlayerController start");

        if (IsLocalPlayer)
        {
            Debug.Log("I'm a local player object");
            Debug.Log("NetworkVariable Player id: " + playerDesignation.Value);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Debug.Log("I'm a server side PlayerController");
        }

        if (IsLocalPlayer)
        {
            Debug.Log("NetworkObjectId: " + NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId);
        }

        if (IsServer)
        {
            _ballActionHandler = new BallActionHandler(referencePrefabBall, baseBallThrust);
        }
    }

    private bool throwing = false;

    [ServerRpc]
    public void ReadyBallServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("ReadyBallServerRpc");
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log("Clientid: " + clientId);
        NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (player == null)
        {
            Debug.Log("Player was null");
        }
        _ballActionHandler.ReadyBallForThrow(Player.transform.position, Player.transform.forward, player);
        throwing = true;
    }

    [ServerRpc]
    public void ThrowBallServerRpc(Vector3 cameraForwardVector, float throwKeyPressedTime)
    {
        Debug.Log("ThrowBallServerRpc: " + cameraForwardVector.ToString() + ", throwKeyPressedTime: " + throwKeyPressedTime);
        //NetworkObject nextBallToThrow = _ballActionHandler.GetNextBallToThrow(); // TODO: probably not necessary
        throwing = false;
        _ballActionHandler.ThrowBall(Player.transform.position, Player.transform.forward, cameraForwardVector, throwKeyPressedTime);//, nextBallToThrow);
    }

    //Detect collisions between the GameObjects with Colliders attached
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.tag);

        if (IsLocalPlayer)
        {
            // show some animations or something
            //Check for a match with the specific tag on any GameObject that collides with your GameObject
            if (collision.gameObject.tag == "ball")
            {
                //If the GameObject has the same tag as specified, output this message in the console
                Debug.Log("Ball hit you client");
            }
        }
        else
        {
            // TODO: game over or something
            //Check for a match with the specific tag on any GameObject that collides with your GameObject
            if (collision.gameObject.tag == "ball")
            {
                //If the GameObject has the same tag as specified, output this message in the console
                Debug.Log("Ball hit you server");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsLocalPlayer)
        {
            // TODO: can this be done better?
            if (!_isPlayerDesignationSet && playerDesignation.Value != "")
            {
                Debug.Log("Player id set: " + playerDesignation.Value);

                _isPlayerDesignationSet = true;
            }

            // limit player speed
            if (Player.velocity.magnitude > maxSpeed)
            {
                Player.velocity = Vector3.ClampMagnitude(Player.velocity, maxSpeed);
            }

            // actual player update is performed in FixedUpdate
            inputHorX = Input.GetAxis("Horizontal");
            inputVertY = Input.GetAxis("Vertical");

            if (Input.GetMouseButtonDown(0))
            {
                _throwKeyPressedStartTime = Time.time;
                ReadyBallServerRpc();
            }

            // TODO: should probably wrap this with a condition of playerControllerReady
            if (Input.GetMouseButtonUp(0))
            {
                // Server RPC throw ball
                float throwKeyPressedTime = Time.time - _throwKeyPressedStartTime;
                ThrowBallServerRpc(PlayerCamera.forward, throwKeyPressedTime);
            }

            if (!_isPlayerControllerReady && _isPlayerDesignationSet && PlayerCamera != null)
            {
                // we have our player designation and camera, ready for play
                _isPlayerControllerReady = true;
            }
        }

        // Run on both client and server to make sure we clamp our player object
        if (_isPlayerDesignationSet)
        {
            // TODO: this probably could also go server side
            // TODO: don't need these checks, set to a variable once we know designation
            // clamp x and z position values to keep player in bounds of plane
            if (playerDesignation.Value == "p1")
            {
                // blue
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, _minX, _maxX),
                    1,
                    Mathf.Clamp(transform.position.z, _minZp1, _maxZp1));
            }
            else if (playerDesignation.Value == "p2")
            {
                // red
                transform.position = new Vector3(Mathf.Clamp(transform.position.x, _minX, _maxX),
                    1,
                    Mathf.Clamp(transform.position.z, _minZp2, _maxZp2));
            }
        }

        //if (IsServer && throwing)
        //{
        //    _ballActionHandler.UpdateReadyBallPosition(Player.transform.position, Player.transform.forward);
        //}
    }

    //private Vector3 _lastPos = Vector3.zero;
    //private Vector3 _distSinceLast;
    //private float _nextDistanceSnapshotTime = 0f;

    void FixedUpdate()
    {
        // This is linked to the project settings under Time > Fixed Timestamp
        // Currently set to .02 seconds, which is 20ms
        if (IsLocalPlayer && _isPlayerControllerReady)
        {
            PlayerMovement(inputHorX, inputVertY);
        }

        //if (IsServer)
        //{
        //    // TODO: new testing client movement extrapolation
        //    if (Time.time > _nextDistanceSnapshotTime)
        //    {
        //        if (_lastPos == Vector3.zero)
        //        {
        //            _lastPos = Player.position;
        //            _distSinceLast = Vector3.zero;
        //        }
        //        else
        //        {
        //            Vector3 delta = Player.transform.position - _lastPos;
        //            float distanceX = delta.x;
        //            float distanceY = delta.y;
        //            float distanceZ = delta.z;

        //            _lastPos = Player.position;
        //            _distSinceLast = new Vector3(distanceX, distanceY, distanceZ);
        //            _nextDistanceSnapshotTime = Time.time + .25f;
        //        }
        //    }
        //}
    }

    private void PlayerMovement(float x, float y)
    {
        Vector3 playerMovementRotation = new Vector3(x, 0f, y) * maxSpeed;

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
}
