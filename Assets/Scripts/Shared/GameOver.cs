using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameOver : MonoBehaviour
{
    private Button _mainMenuButton;

    public void OnMainMenuPressed()
    {
        Debug.Log("OnMainMenuPressed");
        // Not using NetworkManager's scene management here because at this point the
        // connection is closed and your client is operating locally without server synchronization.
        SceneManager.LoadScene(GamePlayManager.MAIN_SCENE, LoadSceneMode.Single);
    }

    void Start()
    {
        _mainMenuButton = GameObject.Find("ReturnToMainMenu").GetComponent<Button>();
        _mainMenuButton.onClick.AddListener(OnMainMenuPressed);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
