using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Class is mostly based on: https://github.com/Unity-Technologies/com.unity.services.samples.matchplay/blob/master/Assets/Scripts/Matchplay/Shared/Game/GameData.cs#L88

/// <summary>
/// All the data we would want to pass across the network.
///
/// NOTE: for this demo I'm not using userGamePreferences (from Unity's Matchplay example project)
/// but it can be used to determine what type of match (2v2, 3v3), game type
/// (capture the flag, free for all, etc), map, etc they wish to play.
/// </summary>
[Serializable]
public class UserData
{
    public string userName; //name of the player
    public string userAuthId; //Auth Player ID
    public ulong networkId;
    //public GameInfo userGamePreferences;

    public UserData(string userName, string userAuthId, ulong networkId) //, GameInfo userGamePreferences)
    {
        this.userName = userName;
        this.userAuthId = userAuthId;
        this.networkId = networkId;
        //this.userGamePreferences = userGamePreferences;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("UserData: ");
        sb.AppendLine($"- User Name:             {userName}");
        sb.AppendLine($"- User Auth Id:          {userAuthId}");
        //sb.AppendLine($"- User Game Preferences: {userGamePreferences}");
        return sb.ToString();
    }
}