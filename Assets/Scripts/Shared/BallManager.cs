using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class BallManager : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerDesignation = new NetworkVariable<FixedString32Bytes>("");
    public delegate void BallHitPlayerDelegate(string playerHit);
    public BallHitPlayerDelegate BallHitPlayerHandler;

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log($"Ball that belongs to {PlayerDesignation.Value}, hit object: " + collision.gameObject.tag);

        if (collision.gameObject.tag != "Untagged")
        {
            Debug.Log($"Ball that belongs to {PlayerDesignation.Value}, hit object: " + collision.gameObject.tag);

            if (IsLocalPlayer)
            {
                Debug.Log("local client id: " + NetworkManager.Singleton.LocalClientId.ToString());

                // show some animations or something
                // Check for a match with the specific tag on any GameObject that collides with your GameObject
                if (collision.gameObject.tag.Contains("ball"))
                {
                    string ballClientOwner = collision.collider.gameObject.GetComponent<BallManager>().PlayerDesignation.Value.ToString();
                    Debug.Log("Ball is owned by clientId: " + ballClientOwner);

                    //If the GameObject has the same tag as specified, output this message in the console
                    if (ballClientOwner != NetworkManager.Singleton.LocalClientId.ToString())
                    {
                        Debug.Log("Ball was NOT yours, you are hit!");
                    }
                    else
                    {
                        Debug.Log("Ball was yours!");
                    }
                    //TODO show red flash when hit

                }
            }
            else
            {
                // server side only

                //Check for a match with the specific tag on any GameObject that collides with your GameObject
                // TODO refactor to something better
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
