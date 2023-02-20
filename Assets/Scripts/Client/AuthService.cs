using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class AuthService : MonoBehaviour
{
    public UserData MockGetUserData()
    {
        // Note: this random number is just for illustrative purposes, in production
        // you would use the actual username and whatever authorization id your project requires.
        System.Random rnd = new System.Random();
        int num = rnd.Next();
        return new UserData("username" + num, "authid" + num, (ulong)num);
    }

    public async Task MockSignIn()
    {
        // NOTE: not addressing authentication in this demo, users will be anonymous
        if (AuthenticationService.Instance.IsSignedIn) { return; }
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
        // Debug.Log($"Player token: {AuthenticationService.Instance.AccessToken}");
    }
}
