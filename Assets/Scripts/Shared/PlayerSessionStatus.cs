public class PlayerSessionStatus
{
    public bool IsReady = false;
    public string Designation = "";

    public PlayerSessionStatus(string designation)
    {
        Designation = designation;
    }
}
