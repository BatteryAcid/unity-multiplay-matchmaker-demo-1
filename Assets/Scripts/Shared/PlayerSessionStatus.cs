using Unity.Netcode;

public class PlayerSessionStatus
{
    public bool IsReady = false;
    public ulong NetworkId = 0;
    public string Designation = "";
    public string GameOverOutcome = "";
    public PlayerController PlayerController;

    public PlayerSessionStatus(ulong networkId, string designation)
    {
        NetworkId = networkId;
        Designation = designation;
    }
}
