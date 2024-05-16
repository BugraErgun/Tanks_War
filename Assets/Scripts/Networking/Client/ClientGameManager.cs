using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable
{
    private const string MenuSceneGame = "Menu";

    private NetworkClient networkClient;
    private JoinAllocation allocation;

    private MatchplayMatchmaker matchmaker;
    public GameData UserData { get; private set; }
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);
        matchmaker = new MatchplayMatchmaker();

        AuthState authState = await AuthenticationWrapper.DoAuth();

        if(authState == AuthState.Authenticated)
        {
            UserData = new GameData
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
                userAuthId = AuthenticationService.Instance.PlayerId
            };
            return true;
        }

        return false;
    }
    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneGame);
    }
    public async void MatchmakeAsync(bool isTeamQueue, Action<MatchmakerPollingResult> onMatchMakeResponse)
    {
        if (matchmaker.IsMatchmaking)
        {
            return;
        }

        UserData.userGamePreferences.gameQueue = isTeamQueue ? GameQueue.Team : GameQueue.Solo;

        MatchmakerPollingResult matchResult = await GetMatchAsync();
        onMatchMakeResponse?.Invoke(matchResult);
    }
    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return;
        }
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        ConnectClient();
    }
    public void StartClient(string ip, int port)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);

        ConnectClient();
    }
    private void ConnectClient()
    {
        string payload = JsonUtility.ToJson(UserData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartClient();
    }
    private async Task<MatchmakerPollingResult> GetMatchAsync()
    {
        MatchmakingResult matchmakingResult = await matchmaker.Matchmake(UserData);
        if (matchmakingResult.result == MatchmakerPollingResult.Success)
        {
            StartClient(matchmakingResult.ip, matchmakingResult.port);
        }
        return matchmakingResult.result;
    }
    public void Disconnect()
    {
        networkClient.Disconnect();
    }
    public void Dispose()
    {
        networkClient?.Dispose();
    }

    public async Task CancelMatchmaking()
    {
        await matchmaker.CancelMatchmaking();
    }
}
