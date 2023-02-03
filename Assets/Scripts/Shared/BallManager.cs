using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class BallManager : NetworkBehaviour
{
    // Indicates this ball's ownership
    public NetworkVariable<FixedString32Bytes> PlayerDesignation = new NetworkVariable<FixedString32Bytes>("");
    public delegate void BallHitPlayerDelegate(string playerHit);
    public BallHitPlayerDelegate BallHitPlayerHandler;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Untagged")
        {
            Debug.Log($"Ball that belongs to {PlayerDesignation.Value}, hit object: " + collision.gameObject.tag);

            if (IsClient)
            {
                Debug.Log("local client id: " + NetworkManager.Singleton.LocalClientId.ToString());

                // Check for a match with the specific tag on any GameObject that collides with your GameObject
                if (collision.gameObject.tag.Contains("ball"))
                {
                    string ballClientOwner = collision.collider.gameObject.GetComponent<BallManager>().PlayerDesignation.Value.ToString();
                    Debug.Log("Ball is owned by clientId: " + ballClientOwner);

                    // If the GameObject has the same tag as specified, output this message in the console
                    if (ballClientOwner != NetworkManager.Singleton.LocalClientId.ToString())
                    {
                        Debug.Log("Ball was NOT yours, you are hit!");
                    }
                    else
                    {
                        Debug.Log("Ball was yours!");
                    }
                }
            }
            else if (IsServer)
            {
                // Server side only

                // Check if the ball hit the other player
                if (collision.gameObject.tag.StartsWith("player") && collision.gameObject.tag != PlayerDesignation.Value.ToString())
                {
                    string ballClientOwner = PlayerDesignation.Value.ToString();
                    Debug.Log("Server: Ball hit you that belonged to client: " + ballClientOwner);
                    BallHitPlayerHandler(collision.gameObject.tag);
                }
            }
        }
    }
}
