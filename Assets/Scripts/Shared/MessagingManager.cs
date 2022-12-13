using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// TODO remove this class
public class MessagingManager : NetworkBehaviour
{
    private ClientGameManager _clientGameManager;

    [ClientRpc]
    public void StartGameClientRpc()
    {
        Debug.Log("StartGameClientRpc called");

        // TODO: all for debugging until we know for sure what's going on here
        // can't call this in start
        if (_clientGameManager == null)
        {
            _clientGameManager = FindObjectOfType<ClientGameManager>();
        }
        
        if (_clientGameManager != null)
        {
            _clientGameManager.OnStartGameEvent();
        }
        else
        {
            Debug.Log("_clientGameManager was null");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConfirmGameStartServerRpc(ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("ConfirmGameStartServerRpc called");
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log("client: " + clientId);
    }

    // Start is called before the first frame update
    void Start()
    {
        // TODO: maybe do a isClient check then try to perform the FindObjectOfType call?
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
