using UnityEngine;
using Unity.Netcode;
using static BallManager;
using System.Threading.Tasks;

public class BallActionHandler : MonoBehaviour
{
    private GameObject _referencePrefab;
    private float _baseBallThrust;
    private float _nextThrowTime = 0f;
    private float _minThrowRate = 0.5f; // half second delay between throws
    private float _throwShim = 0.6f; // makes the throw more playable

    private NetworkObject _nextBallToThrow;
    private BallHitPlayerDelegate _ballHitPlayerDelegate;

    public void Init(GameObject referencePrefab, float baseBallThrust, BallHitPlayerDelegate ballHitPlayerDelegate)
    {
        _referencePrefab = referencePrefab;
        _baseBallThrust = baseBallThrust;
        _ballHitPlayerDelegate = ballHitPlayerDelegate;
    }

    public void SoftReadyNextBallToThrow()
    {
        _nextBallToThrow = NetworkObjectPool.Singleton.GetNetworkObject(_referencePrefab);
    }

    private async Task WaitThenDespawn(NetworkObject objectToDespawn)
    {
        await Task.Delay(2250);

        NetworkObjectPool.Singleton.ReturnNetworkObject(objectToDespawn, _referencePrefab);
        if (objectToDespawn.IsSpawned)
        {
            objectToDespawn.Despawn(false);
        }
    }

    public async void ThrowBall(Vector3 cameraForwardVector, float throwKeyPressedTime, Rigidbody Player, string playerDesignation)
    {
        // Don't dequeue the ball unless the cooldown nextThrowTime has passed
        if (Time.time > _nextThrowTime)
        {
            Vector3 spawnPos = GetBallSpawnPointInFrontOfPlayer(Player.transform.position, Player.transform.forward);
            NetworkObject ballToThrow = NetworkObjectPool.Singleton.GetNetworkObject(_referencePrefab, spawnPos, Quaternion.identity);

            ballToThrow.transform.position = spawnPos;

            Vector3 throwVector = DetermineVectorOfThrow(cameraForwardVector);
            // Debug.Log("Throw vector: " + throwVector);

            Vector3 throwWithThrust = throwVector * DetermineThrustOfBall(throwKeyPressedTime);
            // Debug.Log("throwWithThrust: " + throwWithThrust);

            BallManager ballManager = ballToThrow.gameObject.GetComponent<BallManager>();
            ballManager.BallHitPlayerHandler = _ballHitPlayerDelegate;

            if (!ballToThrow.IsSpawned)
            {
                ballToThrow.Spawn(true);
            }

            ballToThrow.gameObject.GetComponent<BallManager>().PlayerDesignation.Value = playerDesignation; // must happen after spawn
            ballToThrow.gameObject.GetComponent<Rigidbody>().AddForce(throwWithThrust, ForceMode.Impulse);

            _nextThrowTime = Time.time + _minThrowRate;

            await WaitThenDespawn(ballToThrow);
        }
    }

    // Returns a point just in front of the facing direction of the player
    private Vector3 GetBallSpawnPointInFrontOfPlayer(Vector3 playerPosition, Vector3 playerForward)
    {
        return playerPosition + playerForward;
    }

    private Vector3 DetermineVectorOfThrow(Vector3 cameraForwardVector)
    {
        Vector3 cameraVector = cameraForwardVector;
        cameraVector.y += _throwShim; // shim to get the throw vector more playable
        return cameraVector;
    }

    private float DetermineThrustOfBall(float throwKeyPressedTime)
    {
        // Debug.Log("throwKeyPressedTime: " + throwKeyPressedTime);

        float thrustOfBall = _baseBallThrust;
        if (throwKeyPressedTime <= .5)
        {
            thrustOfBall = _baseBallThrust * 1.5f;
        }
        else if (throwKeyPressedTime <= .85)
        {
            thrustOfBall = _baseBallThrust * 2f;
        }
        else if (throwKeyPressedTime >= .85)
        {
            thrustOfBall = _baseBallThrust * 3f;
        }

        // Debug.Log("Throw speed: " + thrustOfBall);

        return thrustOfBall;
    }
}
