using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    public Transform Target;
    public Transform Player;
    public float RotationSpeed = 1;
    public float zoomSpeed = 2;
    public float zoomMin = -2f;
    public float zoomMax = -10f;

    private Transform _camTransform;
    private float _mouseX, _mouseY, _zoom;
    private bool _cursorLocked = true;

    private NetworkBehaviour _playerController;

    //TODO: can we just disable this if not server
    void Start()
    {
        Debug.Log("ThirdPersonCameraController start");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _camTransform = transform;
        _zoom = -10;

        // TODO: not sure this is the best way here to check for local player status
        NetworkBehaviour[] networkBehaviours = gameObject.GetComponentsInParent<NetworkBehaviour>();
        foreach(NetworkBehaviour networkBehaviour in networkBehaviours)
        {
            if (networkBehaviour is PlayerController)
            {
                Debug.Log("player controller found in camera controller");
                _playerController = networkBehaviour;
                break;
            }
        }
        //_playerController = gameObject.GetComponent<NetworkBehaviour>();

        if (_playerController != null)
        {
            Debug.Log("CameraController is Local: " + _playerController.IsLocalPlayer);
        } else
        {
            Debug.Log("PlayerController network behaviour was null");
        }
    }

    void LateUpdate()
    {
        if (_playerController.IsLocalPlayer)
        {
            CamControl();
        }
    }

    void Update()
    {
        if (_playerController.IsLocalPlayer)
        {
            HandleInput();
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

    private void CamControl()
    {
        // disable zoom for this demo
        //_zoom += Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

        //if (_zoom > zoomMin)
        //    _zoom = zoomMin;

        //if (_zoom < zoomMax)
        //    _zoom = zoomMax;

        _mouseX += Input.GetAxis("Mouse X") * RotationSpeed;
        _mouseX = Mathf.Clamp(_mouseX, -90, 90); // locks character/camera rotation to the forward 180 degree view

        _mouseY -= Input.GetAxis("Mouse Y") * RotationSpeed;
        _mouseY = Mathf.Clamp(_mouseY, 30, 30);

        Vector3 dir = new Vector3(0, 0, _zoom);

        Quaternion rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
        //if (Input.GetMouseButton(1))
        //{
        //    _camTransform.position = Target.position + rotation * dir;
        //    _camTransform.LookAt(Target.position);
        //}
        //else
        //{

        // will likely change based on player
        int mouseOffset = -90;

        _camTransform.position = Target.position + rotation * dir;
        _camTransform.LookAt(Target.position);

        Player.rotation = Quaternion.Euler(0, _mouseX + mouseOffset, 0);
        //}
    }
}
