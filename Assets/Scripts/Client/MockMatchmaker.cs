using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class MockMatchmaker : IMatchmaker
{
    private string _localServerIp = "127.0.0.1";
    private int _localServerPort = 9011;

    public async Task<MatchmakingResult> FindMatch(UserData userData)
    {
        return await Task.FromResult(new MatchmakingResult
        {
            result = MatchmakerPollingResult.Success,
            ip = _localServerIp,
            port = _localServerPort,
            resultMessage = "Mock success result"
        });
    }

    public void Dispose()
    {
    }
}
