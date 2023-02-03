using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameOver : NetworkBehaviour
{
    public TMPro.TextMeshProUGUI _outcomeText;

    private GameStateManager _gameStateManager;
    private Button _mainMenuButton;
    private string winningMessage = "You won!";
    private string losingMessage = "You lost!";

    public void OnMainMenuPressed()
    {
        Debug.Log("OnMainMenuPressed");
        // NOTE: Not using NetworkManager's scene management here because at this point the
        // connection is closed and your client is operating locally without server synchronization.
        SceneManager.LoadScene(ServerGamePlayManager.MAIN_SCENE, LoadSceneMode.Single);
    }

    void Start()
    {
        if (!IsServer)
        {
            _mainMenuButton = GameObject.Find("ReturnToMainMenu").GetComponent<Button>();
            _mainMenuButton.onClick.AddListener(OnMainMenuPressed);

            _gameStateManager = GameObject.Find("GameStateManager").GetComponent<GameStateManager>();
            
            if (_gameStateManager.IsWinner)
            {
                Debug.Log(winningMessage);
                _outcomeText.text = winningMessage;
            }
            else
            {
                Debug.Log(losingMessage);
                _outcomeText.text = losingMessage;
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
