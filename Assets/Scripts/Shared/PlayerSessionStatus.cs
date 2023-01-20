using Unity.Netcode;

public class PlayerSessionStatus
{
    public bool IsReady = false;
    public string Designation = "";
    public ulong NetworkId = 0;
    public PlayerController PlayerController;

    public PlayerSessionStatus(ulong networkId, string designation)
    {
        NetworkId = networkId;
        Designation = designation;
    }
}
