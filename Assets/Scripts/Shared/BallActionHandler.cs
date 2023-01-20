using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using static BallManager;

public class BallActionHandler
{
    private GameObject _referencePrefab;
    //private Transform _playerCamera;
    //private Rigidbody _ball;
    private float _baseBallThrust;
    private float _nextThrowTime = 0f;
    private float _minThrowRate = 0.5f; // half second delay between throws
    private float _throwShim = 0.6f; // makes the throw more playable
    //private GameObject _lastBall;

    private NetworkObject _nextBallToThrow;
    private BallHitPlayerDelegate _ballHitPlayerDelegate;

    public BallActionHandler(GameObject referencePrefab, float baseBallThrust, BallHitPlayerDelegate ballHitPlayerDelegate) //TODO Rigidbody ball,
    {
        //_playerCamera = playerCamera;
        //_ball = ball;
        _referencePrefab = referencePrefab;
        _baseBallThrust = baseBallThrust;
        _ballHitPlayerDelegate = ballHitPlayerDelegate;
    }

    // TODO: finish setting up despanwer, server-side only
    private IEnumerator DespawnTimer(NetworkObject objectToDespawn)
    {
        yield return new WaitForSeconds(10);
        Debug.Log("ReturnNetworkObject...");
        NetworkObjectPool.Singleton.ReturnNetworkObject(objectToDespawn, _referencePrefab);
        Debug.Log("Object returned to pool");
        // TODO: may have to call .despawn() here...
        yield break;
    }

    //public NetworkObject GetNextBallToThrow()
    //{
    //    return NetworkObjectPool.Singleton.GetNetworkObject(_referencePrefab);
    //}

    //public void ReadyBallForThrow(Vector3 playerPosition, Vector3 playerForward, Rigidbody player, NetworkObject playerNO)
    //{
    //    _nextBallToThrow = NetworkObjectPool.Singleton.GetNetworkObject(_referencePrefab);
    //    _nextBallToThrow.transform.position = GetSpawnPointInFrontOfPlayer(playerPosition, playerForward);

    //    //BallManager ballManager = _nextBallToThrow.GetComponent<BallManager>();
    //    //ballManager.Player = player;

    //    //// TODO: See if parenting to the NetworkObject of the player prefab root helps synchronize the position, probably won't but still want to try.
    //    //// Parenting to the RigidBody player object won't work i think, because it's not a network object.
    //    //// https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/networkobject-parenting#only-a-server-or-a-host-can-parent-networkobjects

    //    //NetworkObject playerNO = player.GetComponentInChildren<NetworkObject>();
    //    //Debug.Log(playerNO.name + ", " + playerNO.transform.name + ", " + playerNO.gameObject.name);

    //    //// TODO: this isn't working
    //    //bool worked1 = _nextBallToThrow.TrySetParent(playerNO);// player.transform);
    //    //if (worked1)
    //    //{
    //    //    Debug.Log("worked: playerNO");
    //    //} else
    //    //{
    //    //    Debug.Log("failed: playerNO");
    //    //}

    //    //_nextBallToThrow.Spawn(true);

    //    //_nextBallToThrow.enabled = false;
    //}

    // Don't think this is necessary as the we spawned above with ready, which means auto position network transform takes over
    //public void UpdateReadyBallPosition(Vector3 playerPosition, Vector3 playerForward)
    //{
    //    if (_nextBallToThrow)
    //    {
    //        _nextBallToThrow.transform.position = GetSpawnPointInFrontOfPlayer(playerPosition, playerForward);
    //    }
    //}

    public void SoftReadyNextBallToThrow()
    {
        _nextBallToThrow = NetworkObjectPool.Singleton.GetNetworkObject(_referencePrefab);//BallPool.SharedInstance.GetPooledObject();
    }

    //public void ThrowBall(Vector3 playerPosition, Vector3 playerForward, Vector3 cameraForwardVector, float throwKeyPressedTime)
    public void ThrowBall(Vector3 cameraForwardVector, float throwKeyPressedTime, Rigidbody Player, string playerDesignation) //, NetworkObject ballToThrow)
    {
        // Debug.Log("Throw ball was: " + _nextBallToThrow == null ? "null" : "good");

        //if (_nextBallToThrow != null && Time.time > _nextThrowTime)
        //{
        //    Debug.Log("Throwing ball with cameraForwardVector: " + cameraForwardVector.ToString());

        //    //Vector3 spawnPos = GetSpawnPointInFrontOfPlayer(playerPosition, playerForward);

        //    NetworkObject throwingBall = _nextBallToThrow;

        //    //BallManager ballManager = throwingBall.GetComponent<BallManager>();
        //    //ballManager.IsThrown = true;
        //    //Debug.Log("ballManager.IsThrown: " + ballManager.IsThrown);

        //    //throwingBall.transform.position = spawnPos;
        //    //throwingBall.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);

        //    Vector3 throwVector = DetermineVectorOfThrow(cameraForwardVector);
        //    Debug.Log("Throw vector: " + throwVector);

        //    Vector3 throwWithThrust = throwVector * DetermineThrustOfBall(throwKeyPressedTime);
        //    Debug.Log("throwWithThrust: " + throwWithThrust);

        //    //Debug.Log("name: " + ballToThrow.name);
        //    //ballToThrow.gameObject.SetActive(true); // TODO: probably not needed as the GetNetworkObject call does this.

        //    Debug.Log("Spawn before");

        //    //throwingBall.Spawn(true); // Spawn here works, but it makes for weird gameplay, like it sticks above then goes, i want a more fluid motion.
        //    throwingBall.gameObject.GetComponent<Rigidbody>().AddForce(throwWithThrust, ForceMode.Impulse);
        //    throwingBall.Spawn(true);


        //    // TODO: does this work?
        //    // TODO: no because this isn't a monoBehaviour... need to refactor this class maybe...
        //    //DespawnTimer(ballToThrow);

        //    //_lastBall = ballToThrow;
        //    _nextThrowTime = Time.time + _minThrowRate;

        //    _nextBallToThrow = null;
        //}

        //NetworkObject ballToThrow = NetworkObjectPool.Singleton.GetNetworkObject(_referencePrefab);//BallPool.SharedInstance.GetPooledObject();

        NetworkObject ballToThrow = _nextBallToThrow;

        if (ballToThrow != null && Time.time > _nextThrowTime)
        {
            // Debug.Log("Throwing ball with cameraForwardVector: " + cameraForwardVector.ToString());

            //Debug.Log("pos1: " + Player.transform.position);
            Vector3 spawnPos = GetSpawnPointInFrontOfPlayer(Player.transform.position, Player.transform.forward);//GetSpawnPointInFrontOfPlayer(playerPosition, playerForward);

            ballToThrow.transform.position = spawnPos;
            //ballToThrow.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);

            Vector3 throwVector = DetermineVectorOfThrow(cameraForwardVector);
            // Debug.Log("Throw vector: " + throwVector);

            Vector3 throwWithThrust = throwVector * DetermineThrustOfBall(throwKeyPressedTime);
            // Debug.Log("throwWithThrust: " + throwWithThrust);

            //Debug.Log("Spawn before");
            //Debug.Log("pos2: " + Player.transform.position);
            BallManager ballManager = ballToThrow.gameObject.GetComponent<BallManager>();
            ballManager.BallHitPlayerHandler = _ballHitPlayerDelegate;

            ballToThrow.Spawn(true); // Spawn here works, but it makes for weird gameplay, like it sticks above then goes, i want a more fluid motion.
            ballToThrow.gameObject.GetComponent<BallManager>().PlayerDesignation.Value = playerDesignation;//clientId.ToShortString(); // must happen after spawn
            ballToThrow.gameObject.GetComponent<Rigidbody>().AddForce(throwWithThrust, ForceMode.Impulse);


            // TODO: does this work?
            // TODO: no because this isn't a monoBehaviour... need to refactor this class maybe...
            //DespawnTimer(ballToThrow);

            //_lastBall = ballToThrow;
            _nextThrowTime = Time.time + _minThrowRate;
        }

        //if (_ball != null && Time.time > _nextThrowTime)
        //{
        //    Debug.Log("Throwing ball with cameraForwardVector: " + cameraForwardVector.ToString());
        //    //Debug.Log("Throwing ball with playerForward: " + playerForward.ToString());

        //    Vector3 spawnPos = GetSpawnPointInFrontOfPlayer(playerPosition, playerForward);

        //    //_ball.gameObject.SetActive(false);
        //    _ball.position = spawnPos;
        //    _ball.velocity = new Vector3(0, 0, 0);

        //    Vector3 throwVector = DetermineVectorOfThrow(cameraForwardVector);
        //    //Vector3 throwVector = DetermineVectorOfThrow(playerForward);
        //    Debug.Log("Throw vector: " + throwVector);

        //    Vector3 throwWithThrust = throwVector * DetermineThrustOfBall(throwKeyPressedTime);
        //    Debug.Log("throwWithThrust: " + throwWithThrust);

        //    //_ball.gameObject.SetActive(true);
        //    _ball.AddForce(throwWithThrust, ForceMode.Impulse);
        //    _nextThrowTime = Time.time + _minThrowRate;
        //    //_ball.AddForce(DetermineVectorOfThrow() * DetermineThrustOfBall(throwKeyPressedTime), ForceMode.Impulse);
        //}
    }

    // TODO: rename this ball spawn point at player
    // Returns a point just in front of the facing direction/vector of the player
    private Vector3 GetSpawnPointInFrontOfPlayer(Vector3 playerPosition, Vector3 playerForward)
    {
        // spawns ball right in front of player
        return playerPosition + playerForward * 1f;
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

    // TODO: remove
    public void TestThrowBall(Vector3 playerPosition, Vector3 playerForward, Vector3 cameraForwardVector)
    {
        Vector3 spawnPos = GetSpawnPointInFrontOfPlayer(playerPosition, playerForward);
        _referencePrefab.SetActive(true);
        _referencePrefab.transform.position = spawnPos;
        _referencePrefab.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);

        Vector3 throwVector = DetermineVectorOfThrow(cameraForwardVector);
        Debug.Log("Throw vector: " + throwVector);

        Vector3 throwWithThrust = throwVector * DetermineThrustOfBall(.5f);
        Debug.Log("throwWithThrust: " + throwWithThrust);

        //Debug.Log("name: " + _referencePrefab.name);
        //_referencePrefab.gameObject.SetActive(true); // TODO: probably not needed
        //_referencePrefab.Spawn(true); // Spawn here works, but it makes for weird gameplay, like it sticks above then goes, i want a more fluid motion.
        _referencePrefab.gameObject.GetComponent<Rigidbody>().AddForce(throwWithThrust, ForceMode.Impulse);
    }
}
