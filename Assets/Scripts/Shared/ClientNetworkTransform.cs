using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
/// for transforms that'll always be owned by the server.
/// source: https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop
///
/// On the gameobject you're tracking position, don't add the built-in NetworkTransform, add this component instead.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Used to determine who can write to this transform. Owner client only.
    /// This imposes state to the server. This is putting trust on your clients. Make sure no security-sensitive features use this transform.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}