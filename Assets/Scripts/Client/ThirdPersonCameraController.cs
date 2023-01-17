using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    //public Transform Target;
    //public Transform Player;
    //public GameObject Target;
    //public GameObject Player;
    //private Transform _targetTransform;

    public Transform PlayerTransform;
    public string PlayerDesignation = "";
    public static bool TESTING = false;

    public float RotationSpeed = 1;
    // Not using zoom for this demo
    //public float zoomSpeed = 2;
    //public float zoomMin = -2f;
    //public float zoomMax = -10f;

    private Transform _camTransform;
    private float _mouseX, _mouseY, _zoom;
    private bool _cursorLocked = true;
    private bool _isCamControlReady = false;

    //TODO: can we just disable this if not server
    void Start()
    {
        Debug.Log("ThirdPersonCameraController start");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _camTransform = transform;
        _zoom = -10;

        // Set both camera follow and look-at targets to the Player transform
        //_targetTransform = Player.GetComponentInChildren<Rigidbody>().transform;
        //PlayerTransform = Player.GetComponentInChildren<Rigidbody>().transform;

        // TODO: not sure this is the best way here to check for local player status
        //NetworkBehaviour[] networkBehaviours = Player.GetComponentsInChildren<NetworkBehaviour>(); //gameObject.GetComponentsInParent<NetworkBehaviour>();
        //foreach (NetworkBehaviour networkBehaviour in networkBehaviours)
        //{
        //    if (networkBehaviour.IsLocalPlayer && networkBehaviour is PlayerController)
        //    {
        //        Debug.Log("camera controller: Local player controller found");
        //        _playerController = networkBehaviour;
        //        break;
        //    }
        //}
        ////_playerController = gameObject.GetComponent<NetworkBehaviour>();

        //if (_playerController != null)
        //{
        //    Debug.Log("CameraController is Local: " + _playerController.IsLocalPlayer);
        //    // TODO: designation isn't set here or isn't working, needs to be in update?
        //    Debug.Log("Camera controller is playerDesignation: " + ((PlayerController)_playerController).playerDesignation.Value);
        //}
        //else
        //{
        //    Debug.Log("PlayerController network behaviour was null");
        //}

    }

    void LateUpdate()
    {
        // The PlayerTransform can be null when despawning player object, which would cause exceptions
        if (_isCamControlReady && PlayerTransform != null)
        {
            CamControl();
        }
    }

    void Update()
    {
        if (_isCamControlReady && PlayerTransform != null)
        {
            HandleInput();
        }

        if (!_isCamControlReady)
        {
            if (PlayerDesignation != "")
            {
                if (PlayerDesignation == "p1")
                {
                    _mouseX = 180;
                }
                else if (PlayerDesignation == "p2")
                {
                    _mouseX = 0;
                }

                _isCamControlReady = true;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown("escape") && !_cursorLocked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _cursorLocked = !_cursorLocked;
        }
        else if (Input.GetKeyDown("escape") && _cursorLocked)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _cursorLocked = !_cursorLocked;
        }
    }

    //private Vector3 previousRot = new Vector3(0, 0, 0);
    //private float previousMouseX = 0;

    private void CamControl()
    {
        // disable zoom for this demo
        //_zoom += Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

        //if (_zoom > zoomMin)
        //    _zoom = zoomMin;

        //if (_zoom < zoomMax)
        //    _zoom = zoomMax;

        _mouseX += Input.GetAxis("Mouse X") * RotationSpeed;

        // TODO: get these in start and set class variables - maybe even server side??
        // locks character/camera rotation to the forward 180 degree view
        if (PlayerDesignation == "p1")
        {
            _mouseX = Mathf.Clamp(_mouseX, 90, 270);
        }
        else if (PlayerDesignation == "p2")
        {
            _mouseX = Mathf.Clamp(_mouseX, -90, 90);
        }

        // y is clamped the same as it's vertical rotation
        _mouseY -= Input.GetAxis("Mouse Y") * RotationSpeed;
        _mouseY = Mathf.Clamp(_mouseY, 30, 30);

        Vector3 dir = new Vector3(0, 0, _zoom);

        Quaternion rotation = Quaternion.Euler(_mouseY, _mouseX, 0);

        _camTransform.position = PlayerTransform.position + rotation * dir; // NOTE: this is causing the camera to be positioned off by 90 
        _camTransform.LookAt(PlayerTransform.position);


        //if (previousMouseX != _mouseX)
        //{
        //    Debug.Log("mousex updated from: " + previousMouseX + ", to: " + _mouseX);
        //    previousMouseX = _mouseX;
        //}

        PlayerTransform.rotation = Quaternion.Euler(0, _mouseX, 0);

        //if (previousRot != Player.rotation.eulerAngles)
        //{
        //    Debug.Log("rot updated: " + Player.rotation.eulerAngles + ", mouse input rotation: " + rotation.eulerAngles);
        //    previousRot = Player.rotation.eulerAngles;
        //}
    }
}
