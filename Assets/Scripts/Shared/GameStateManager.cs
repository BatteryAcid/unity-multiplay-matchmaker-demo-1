using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// An example script where you can hold state, independent of client or server,
// throughout the lifecycle of the game.
public class GameStateManager : NetworkBehaviour
{
    public bool IsWinner = false;

    [ClientRpc]
    public void SetIsWinnerClientRpc(bool isWinner, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("isWinner: " + isWinner);
        IsWinner = isWinner;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
