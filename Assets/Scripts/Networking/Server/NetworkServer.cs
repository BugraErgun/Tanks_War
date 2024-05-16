using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkServer : IDisposable
{
    public Action<string> OnClientLeft;

    public Action<GameData> OnUserJoined;
    public Action<GameData> OnUserLeft;

    private NetworkManager networkManager;

    private NetworkObject playerPrefab;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string,GameData> authIdToUserdata = new Dictionary<string,GameData>();

    public NetworkServer(NetworkManager networkManager,NetworkObject playerPrefab)
    {
        this.networkManager = networkManager;

        this.playerPrefab = playerPrefab;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
    }

    public bool OpenConnection(string ip,int port)
    {
        UnityTransport transport = networkManager.gameObject.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);
        return networkManager.StartServer();
    }
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        GameData userData = JsonUtility.FromJson<GameData>(payload);

        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        authIdToUserdata[userData.userAuthId] = userData;

        OnUserJoined?.Invoke(userData);

        _ = SpawnPlayerDelayed(request.ClientNetworkId);

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private async Task SpawnPlayerDelayed(ulong clientId)
    {
        await Task.Delay(1000);

        NetworkObject playerInstance = GameObject.Instantiate(playerPrefab, SpawnPoint.GetRandomSpawnPos(), Quaternion.identity);

        playerInstance.SpawnAsPlayerObject(clientId);
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId,out string authId))
        {
            clientIdToAuth.Remove(clientId);
            OnUserLeft?.Invoke(authIdToUserdata[authId]);
            authIdToUserdata.Remove(authId);
            OnClientLeft?.Invoke(authId);
        }
    }

    public GameData GetUserDataByClientId(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserdata.TryGetValue(authId,out GameData data))
            {
                return data;
            }
            return null;
        }
        return null;
    }

    public void Dispose()
    {
        if (networkManager == null)
        {
            return;
        }

        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        networkManager.OnServerStarted -= OnNetworkReady;

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();  
        }
    }
}
