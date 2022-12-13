using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

// Calls to this class will be made ONLY from the SERVER
public class ClientMessagingManager : IMessagingManager
{
    private ClientGameManager _clientGameManager;

    [ClientRpc]
    public void StartGameClientRpc()
    {
        Debug.Log("StartGameClientRpc called");

        if (_clientGameManager != null)
        {
            _clientGameManager.OnStartGameEvent();
        } else
        {
            Debug.Log("_clientGameManager was null");
        }
        
    }

    // Start is called before the first frame update
    void Start()
    {
        _clientGameManager = FindObjectOfType<ClientGameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
