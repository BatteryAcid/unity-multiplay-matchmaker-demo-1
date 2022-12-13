using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Calls to this class will be made ONLY from the CLIENT
public class ServerMessagingManager : IMessagingManager
{
    [ServerRpc]
    public void ConfirmGameStartServerRpc()
    {
        Debug.Log("ConfirmGameStartServerRpc called");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
