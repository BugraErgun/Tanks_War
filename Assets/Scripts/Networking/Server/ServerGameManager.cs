using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TerrainTools;


public class ServerGameManager : IDisposable
{
    private string serverIp;
    private int serverPort;
    private int queryPort;
    private MatchplayBackfiller backfiller;
    public NetworkServer NetworkServer { get; private set; }

    private MultiplayAllocationService multiplayAllocationService;

    private Dictionary<string,int> teamIdToTeamIndex = new Dictionary<string,int>();

    public ServerGameManager(string serverIp, int serverPort, int queryPort, NetworkManager manager,NetworkObject playerPrefab) 
    { 
        this.serverIp = serverIp;
        this.serverPort = serverPort;
        this.queryPort = queryPort;
        NetworkServer = new NetworkServer(manager,playerPrefab);

        multiplayAllocationService = new MultiplayAllocationService();
    }

    public async Task StartGameServerAsync()
    {
        await multiplayAllocationService.BeginServerCheck();

        try
        {
            MatchmakingResults matchmakerPayload = await GetMatchMakerPayload();

            if (matchmakerPayload != null)
            {
                await StartBackFill(matchmakerPayload);
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }
            else
            {
                Debug.LogWarning("matchmaker payload timed out");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            throw;
        }

        if(!NetworkServer.OpenConnection(serverIp, serverPort))
        {
            Debug.LogWarning("Network Server did not start as expected.");
            return;
        }
    }

    private async Task StartBackFill(MatchmakingResults payload)
    {
        backfiller = new MatchplayBackfiller($"{serverIp}:{serverPort}",
                                                payload.QueueName,
                                                payload.MatchProperties,
                                                20);
        if (backfiller.NeedsPlayers())
        {
            await backfiller.BeginBackfilling();
        }
    }

    private void UserJoined(GameData user)
    {
        Team team = backfiller.GetTeamByUserId(user.userAuthId);

        if (!teamIdToTeamIndex.TryGetValue(team.TeamId, out int teamIndex))
        {
            teamIndex = teamIdToTeamIndex.Count;
            teamIdToTeamIndex.Add(team.TeamId, teamIndex);
        }

        user.teamIndex = teamIndex;

        multiplayAllocationService.AddPlayer();

        if (!backfiller.NeedsPlayers() && backfiller.IsBackfilling)
        {
            _ = backfiller.StopBackfill();
        }
    }
    private void UserLeft(GameData user)
    {
        int playerCount = backfiller.RemovePlayerFromMatch(user.userAuthId);
        multiplayAllocationService.RemovePlayer();

        if (playerCount <= 0)
        {
            CloseServer();
            return;
        }
        if (backfiller.NeedsPlayers() && !backfiller.IsBackfilling)
        {
            _ = backfiller.BeginBackfilling();
        }
    }

    private async void CloseServer()
    {
        await backfiller.StopBackfill();
        Dispose();
        Application.Quit();
    }

    private async Task <MatchmakingResults> GetMatchMakerPayload()
    {
        Task<MatchmakingResults> matchmakerPayloadTask = multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();

        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(20000)) == matchmakerPayloadTask)
        {
            return matchmakerPayloadTask.Result;
        }
        return null;
    }

    public void Dispose()
    {
        NetworkServer.OnUserJoined -= UserJoined;
        NetworkServer.OnUserLeft -= UserLeft;

        backfiller?.Dispose();
        multiplayAllocationService?.Dispose();
        NetworkServer?.Dispose();
    }
}