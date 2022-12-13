using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    init,
    started,
    ended
}

// TODO: consider passing these to the respective GameManagers
// TODO: can this be moved to only be on on for server side?
public class GameStateManager : NetworkBehaviour
{
    public NetworkVariable<GameState> CurrentGameState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("GameStateManager.OnNetworkSpawn...");

        // skip functionality if on client side
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        Debug.Log("GameStateManager.OnNetworkSpawn setting gamestate to init...");
        CurrentGameState.Value = GameState.init;

        // TODO: testing remove
        //var status = NetworkManager.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
        //if (status != SceneEventProgressStatus.Started)
        //{
        //    Debug.LogWarning($"Failed to load GamePlay " +
        //          $"with a {nameof(SceneEventProgressStatus)}: {status}");
        //}
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("GameStateManager start...");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
