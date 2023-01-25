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
    public const int MAX_HITS_GAME_OVER = 5;

    private static int MinPlayersPerSession = 2;
    private Dictionary<ulong, PlayerSessionStatus> _playerSessionStatus;

    public void CheckForGameOver()
    {
        Dictionary<ulong, PlayerSessionStatus> playerStatuses = GetPlayerSessionStatuses();

        ulong winner = 0;
        ulong loser = 0;

        foreach (KeyValuePair<ulong, PlayerSessionStatus> playerStatus in playerStatuses)
        {
            if (loser != 0)
            {
                winner = playerStatus.Value.NetworkId;
                playerStatus.Value.GameOverOutcome = "You Won!";
                break;
            }

            // TODO: this feels icky
            if (playerStatus.Value.PlayerController.NumberOfHits.Value >= MAX_HITS_GAME_OVER)
            {
                // this player lost
                Debug.Log("Player " + playerStatus.Value.Designation + " was hit max times and lost, ending game...");

                // In the case where both have max hits, the first checked loses.  Could update to do it differently.
                loser = playerStatus.Value.NetworkId;
                playerStatus.Value.GameOverOutcome = "You Lost!";
            }
            else
            {
                // temp store incase we have a winner
                winner = playerStatus.Value.NetworkId;
            }
        }

        if (winner != 0 && loser != 0)
        {
            // game is over
            Debug.Log($"Winner: {winner} Loser: {loser}.");
            EndGameAfterWinner(winner, loser);
        } else
        {
            Debug.Log("No winner found.");
        }
    }

    private void EndGameAfterWinner(ulong winnerNetworkId, ulong loserNetworkId)
    {
        Debug.Log("EndGameAfterWinner");
        // TODO: mention that these big "finds" can be avoided for the most part if using a DI framework
        GameStateManager gameStateManager = GameObject.Find("GameStateManager").GetComponent<GameStateManager>();

        // notifiy winner client
        ClientRpcParams winnerClientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { winnerNetworkId }
            }
        };
        Debug.Log("Sending client rpc to winner: " + winnerNetworkId);
        gameStateManager.SetIsWinnerClientRpc(true, winnerClientRpcParams);

        // notifiy loser client
        ClientRpcParams loserClientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { loserNetworkId }
            }
        };
        Debug.Log("Sending client rpc to loser: " + loserNetworkId);
        gameStateManager.SetIsWinnerClientRpc(false, loserClientRpcParams);

        ServerNetworkManager serverNetworkManager = GameObject.Find("GameManager").GetComponent<ServerNetworkManager>();
        Debug.Log("Calling serverNetworkManager.EndGame().");
        serverNetworkManager.EndGame();
    }

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

                    _playerSessionStatus[clientId].PlayerController = ((PlayerController)networkBehaviour);

                    // Update some value on the player's prefab
                    //((PlayerController)networkBehaviour).playerDesignation.Value = playerDesignation;
                    _playerSessionStatus[clientId].PlayerController.PlayerDesignation.Value = playerDesignation;
                    playerObject.GetComponentInChildren<Rigidbody>().tag = playerDesignation;
                    // TODO: also tag players - maybe we don't need to use playerDesignation if tag is propigated to client, not sure.
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

    // TODO: not used yet... remove?
    public PlayerSessionStatus GetPlayerSessionStatus(ulong clientId)
    {
        return _playerSessionStatus[clientId];
    }

    public PlayerSessionStatus GetPlayerSessionStatusByDesignation(string designation)
    {
        foreach (KeyValuePair<ulong, PlayerSessionStatus> playerSessionStatus in _playerSessionStatus)
        {
            if (playerSessionStatus.Value.Designation == designation)
            {
                return playerSessionStatus.Value;
            }
        }
        return null;
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
