using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;

// TODO: rename to server gameplay..
public class GamePlayManager : MonoBehaviour
{
    public const string MAIN_SCENE = "MainScene";
    public const string GAME_PLAY_SCENE = "GamePlay";
    public const string GAME_OVER_SCENE = "GameOver";
    private static int MinPlayersPerSession = 2;
    private Dictionary<ulong, PlayerSessionStatus> _playerSessionStatus;
    
    //public void LoadGamePlayScene(NetworkSceneManager sceneManager)
    //{
    //    var status = sceneManager.LoadScene(GAME_PLAY_SCENE, LoadSceneMode.Single);
    //    if (status != SceneEventProgressStatus.Started)
    //    {
    //        Debug.LogWarning($"Failed to load GamePlay with a {nameof(SceneEventProgressStatus)}: {status}");
    //    }
    //    else
    //    {
    //        Debug.Log("GamePlay scene started.");
    //    }
    //}

    //public void LoadMainScene(NetworkSceneManager sceneManager)
    //{
    //    var status = sceneManager.LoadScene(MAIN_SCENE, LoadSceneMode.Single);
    //    if (status != SceneEventProgressStatus.Started)
    //    {
    //        Debug.LogWarning($"Failed to load Main Scene with a {nameof(SceneEventProgressStatus)}: {status}");
    //    }
    //    else
    //    {
    //        Debug.Log("Main Scene scene started.");
    //    }
    //}

    public void LoadSingleModeScene(NetworkSceneManager sceneManager, string sceneName)
    {
        var status = sceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to load {sceneName} Scene with a {nameof(SceneEventProgressStatus)}: {status}");
        }
        else
        {
            Debug.Log($"{sceneName} scene started.");
        }
    }

    public void ReadyPlayerForGamePlay(ulong clientId)
    {
        Debug.Log($"ReadyPlayerForGamePlay: {clientId}");

        string playerDesignation = _playerSessionStatus[clientId].Designation;

        // Find the client we want to 'ready'
        NetworkObject playerObject = null;
        foreach (KeyValuePair<ulong, NetworkClient> connectedClient in NetworkManager.Singleton.ConnectedClients)
        {

            Debug.Log("ConnectedClient key: " + connectedClient.Key + ", Assumed clientId which should be key: " + clientId);
            Debug.Log("ConnectedClient clientid: " + connectedClient.Value.ClientId);

            if (connectedClient.Value.ClientId == clientId)
            {
                Debug.Log("ConnectedClient found");
                playerObject = NetworkManager.Singleton.ConnectedClients[connectedClient.Key].PlayerObject;
                break;
            }
        }

        // Update the client's player prefab object with whatever is required for your game.
        // Here we set the player 1/2 designation.
        if (playerObject != null)
        {
            Debug.Log(playerObject.IsLocalPlayer);

            foreach (NetworkBehaviour networkBehaviour in playerObject.GetComponentsInChildren<NetworkBehaviour>())
            {
                // The PlayerController is a script attached to the Player Prefab.
                // Only change the server side representation of PlayerController, as the network variable will sync
                if (networkBehaviour is PlayerController && !networkBehaviour.IsLocalPlayer)
                {
                    Debug.Log("Found the playerController, is spawned: " + networkBehaviour.IsSpawned);

                    // Update some value on the player's prefab
                    ((PlayerController)networkBehaviour).playerDesignation.Value = playerDesignation;
                }
            }

            _playerSessionStatus[clientId].IsReady = true;
        }
        else
        {
            Debug.Log("player object was null");
        }
    }

    public bool IsMatchReadyToStart()
    {
        int readyCount = 0;
        foreach (KeyValuePair<ulong, PlayerSessionStatus> playerSessionStatus in _playerSessionStatus)
        {
            if (playerSessionStatus.Value.IsReady)
            {
                readyCount++;
            }
        }

        Debug.Log("Current player count: " + readyCount + ", Min Players: " + MinPlayersPerSession);

        if (readyCount == MinPlayersPerSession)
        {
            Debug.Log("Game is full and is ready to start.");
            Debug.Log("Loading GamePlay scene...");

            return true;
        }
        return false;
    }

    public Dictionary<ulong, PlayerSessionStatus> GetPlayerSessionStatuses()
    {
        return _playerSessionStatus;
    }

    void Start()
    {
        if (ApplicationController.IsLocalTesting)
        {
            MinPlayersPerSession = 1;
        }
    }

    private void Awake()
    {
        _playerSessionStatus = new Dictionary<ulong, PlayerSessionStatus>();
    }
}
