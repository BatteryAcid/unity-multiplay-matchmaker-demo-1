using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : NetworkBehaviour
{
    public Rigidbody Player;
    public Transform PlayerCamera;

    private float maxSpeed = 10;
    private float inputHorX, inputVertY;

    public NetworkVariable<FixedString32Bytes> playerId = new NetworkVariable<FixedString32Bytes>();
    private bool _isPlayerIdSet = false;

    public override void OnNetworkSpawn()
    {
        Debug.Log("PlayerController OnNetworkSpawn");
        base.OnNetworkSpawn();

        if (IsLocalPlayer)
        {
            Debug.Log("LocalPlayer/OnNetworkSpawn: NetworkVariable Player id: " + playerId.Value);

            // This works.
            Transform cameraMountPoint = gameObject.transform.Find("CameraMount"); // get mount point for camera
            Transform cameraTransform = Camera.main.gameObject.transform;  //Find main camera which is part of the scene instead of the prefab
            cameraTransform.parent = cameraMountPoint.transform;  //Make the camera a child of the mount point
            cameraTransform.position = cameraMountPoint.transform.position;  //Set position/rotation same as the mount point
            cameraTransform.rotation = cameraMountPoint.transform.rotation;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // NetworkManager.LocalClient.PlayerObject // to get local client player object
        // NetworkManager.Singleton.LocalClientId You can pass this to an RPC that uses the ConnectedClients server side to retrieve the client

        Debug.Log("PlayerController start");

        if (IsLocalPlayer)
        {
            Debug.Log("I'm a local player object");
            Debug.Log("NetworkVariable Player id: " + playerId.Value);
        }
        else
        {
            Debug.Log("I'm a server side PlayerController");
        }

        if (IsLocalPlayer)
        {
            Debug.Log("NetworkObjectId: " + NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId);
        }
        
        //if (playerId.Value == "p1")
        //{
        //    // TODO: set these on server
        //    Player.position = new Vector3(Random.Range(-9.5f, 9.5f), 1, Random.Range(-9.5f, -5f));

        //    //Vector3 relativePos = new Vector3(-90, 1, 0);// - transform.position;

        //    //// the second argument, upwards, defaults to Vector3.up
        //    //Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
        //    //Player.transform.rotation = rotation;
        //}
        

    }

    // Update is called once per frame
    void Update()
    {
        if (IsLocalPlayer)
        {
            if (!_isPlayerIdSet && playerId.Value != "")
            {
                Debug.Log("Player id set: " + playerId.Value);
                _isPlayerIdSet = true;
            }

            // limit player speed
            if (Player.velocity.magnitude > maxSpeed)
            {
                Player.velocity = Vector3.ClampMagnitude(Player.velocity, maxSpeed);
            }

            // actual player update is performed in FixedUpdate
            inputHorX = Input.GetAxis("Horizontal");
            inputVertY = Input.GetAxis("Vertical");


            float minX = -9.5f, maxX = 9.5f, minZ = -10f, maxZ = -5f;

            // TODO: this will have to be based on player1/2
            // clamp x and z position values to keep player in bounds of plane
            //transform.position = new Vector3(Mathf.Clamp(transform.position.x, minX, maxX),
            //    1,
            //    Mathf.Clamp(transform.position.z, minZ, maxZ));
        }
    }

    void FixedUpdate()
    {
        // This is linked to the project settings under Time > Fixed Timestamp
        // Currently set to .02 seconds, which is 20ms
        if (IsLocalPlayer)
        {
            PlayerMovement(inputHorX, inputVertY);
            
        }
    }

    private void PlayerMovement(float x, float y)
    {
        //PlayerIdleCheck(x, y);

        //if (!playerIdle) // skip sending extra zero vertors when player isn't moving
        //{
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



        //Player.MovePosition(new Vector3(Player.position.x + x, Player.position.y, Player.position.z + y));

        //if (WebSocketService.Instance.matchInitialized)
        //{
        //    WebSocketService.Instance.SendVelocity(roundedVelocity);
        //}
    }
}
