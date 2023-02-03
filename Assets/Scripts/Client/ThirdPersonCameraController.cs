using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    public Transform PlayerTransform;
    public string PlayerDesignation = "";
    public static bool TESTING = false;
    public float RotationSpeed = 1;

    private Transform _camTransform;
    private float _mouseX, _mouseY, _zoom;
    private bool _cursorLocked = true;
    private bool _isCamControlReady = false;

    void Start()
    {
        Debug.Log("ThirdPersonCameraController start");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _camTransform = transform;
        _zoom = -10;
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
                if (PlayerDesignation == "player1")
                {
                    _mouseX = 180;
                }
                else if (PlayerDesignation == "player2")
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

    private void CamControl()
    {
        _mouseX += Input.GetAxis("Mouse X") * RotationSpeed;

        // locks character/camera rotation to the forward 180 degree view
        if (PlayerDesignation == "player1")
        {
            _mouseX = Mathf.Clamp(_mouseX, 90, 270);
        }
        else if (PlayerDesignation == "player2")
        {
            _mouseX = Mathf.Clamp(_mouseX, -90, 90);
        }

        // y is clamped the same as it's vertical rotation
        _mouseY -= Input.GetAxis("Mouse Y") * RotationSpeed;
        _mouseY = Mathf.Clamp(_mouseY, 30, 30);

        Vector3 dir = new Vector3(0, 0, _zoom);

        Quaternion rotation = Quaternion.Euler(_mouseY, _mouseX, 0);

        _camTransform.position = PlayerTransform.position + rotation * dir;
        _camTransform.LookAt(PlayerTransform.position);

        PlayerTransform.rotation = Quaternion.Euler(0, _mouseX, 0);
    }
}
