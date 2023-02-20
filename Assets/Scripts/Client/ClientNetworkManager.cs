using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

// All client networking 
public class ClientNetworkManager : MonoBehaviour
{
    // Time in seconds before the client considers a lack of server response a timeout
    private const int k_TimeoutDuration = 10;

    public void StartConnectionToServer(MatchmakingResult matchmakingResult)
    {
        Debug.Log($"Connecting to server at {matchmakingResult.ip}:{matchmakingResult.port}");

        // Set the server ip and port to connect to, received from our match making result.
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData(matchmakingResult.ip, (ushort)matchmakingResult.port);

        ConnectClient();
    }

    private void ConnectClient()
    {
        // NOTE: For this demo no data is being sent upon connection, but you could do it with the example code below.
        // var userData = <some_user_data_source>;
        // var payload = JsonUtility.ToJson(userData);
        // var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        // _networkManager.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.NetworkConfig.ClientConnectionBufferTimeout = k_TimeoutDuration;

        //  If the socket connection fails, we'll hear back by getting an ReceiveLocalClientDisconnectStatus callback
        //  for ourselves and get a message telling us the reason. If the socket connection succeeds, we'll get our
        //  ReceiveLocalClientConnectStatus callback This is where game-layer failures will be reported.
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Starting Client!");
        }
        else
        {
            Debug.LogWarning($"Could not Start Client!");
        }
    }
}
