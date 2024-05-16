using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform leaderboardEntithHolder;
    [SerializeField] private Transform teamLeaderBoardEntityHolder;
    [SerializeField] private GameObject teamLeaderboardBG;
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private int entitiesToDisplay = 8;
    [SerializeField] private Color ownerColor;
    [SerializeField] private string[] teamNames;
    [SerializeField] private TeamColourLookup teamColourLookup;

    private NetworkList<LeaderboardEntityState> leaderboardEntities;

    private List<LeaderboardEntity> entityDisplays = new List<LeaderboardEntity>();

    private List<LeaderboardEntity> teamEntityDisplays = new List<LeaderboardEntity>();

    private void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            if (ClientSingleton.Instance.GameManager.UserData.userGamePreferences.gameQueue == GameQueue.Team)
            {
                teamLeaderboardBG.SetActive(true);

                for (int i = 0; i < teamNames.Length; i++)
                {
                    LeaderboardEntity teamLeaderboardEntity = Instantiate(leaderboardEntityPrefab, teamLeaderBoardEntityHolder);

                    teamLeaderboardEntity.Initialise(i, teamNames[i], 0);

                    Color teamColour =  teamColourLookup.GetTeamColour(i);
                    teamLeaderboardEntity.SetColour(teamColour);

                    teamEntityDisplays.Add(teamLeaderboardEntity);
                }
            }

            leaderboardEntities.OnListChanged += HandleLeaderboardEntitiesChanged;
            foreach (LeaderboardEntityState entity in leaderboardEntities)
            {
                HandleLeaderboardEntitiesChanged(new NetworkListEvent<LeaderboardEntityState>
                {
                    Type = NetworkListEvent<LeaderboardEntityState>.EventType.Add,
                    Value = entity
                });
            }
        }

        if (IsServer)
        {
            TankPlayer[] players = FindObjectsByType<TankPlayer>(FindObjectsSortMode.None);

            foreach (TankPlayer player in players)
            {
                HandlePlayerSpawned(player);
            }

            TankPlayer.OnPlayerSpawned += HandlePlayerSpawned;
            TankPlayer.OnPlayerDespawned += HandlePlayerDesawned;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged -= HandleLeaderboardEntitiesChanged;
        }

        if (IsServer)
        {
            TankPlayer.OnPlayerSpawned -= HandlePlayerSpawned;
            TankPlayer.OnPlayerDespawned -= HandlePlayerDesawned;
        }
    }
    private void HandlePlayerSpawned(TankPlayer player)
    {
        leaderboardEntities.Add(new LeaderboardEntityState
        {
            ClientId = player.OwnerClientId,
            PlayerName = player.PlayerName.Value,
            TeamIndex = player.TeamIndex.Value,
            Coins = 0
        });

        player.Wallet.TotalCoins.OnValueChanged += (oldCoins, newCoins) => HandleCoinsChanged(player.OwnerClientId, newCoins);
    }
    private void HandlePlayerDesawned(TankPlayer player)
    {
        if (leaderboardEntities == null)
        {
            return;
        }

        foreach (LeaderboardEntityState entity in leaderboardEntities)
        {
            if (entity.ClientId != player.OwnerClientId)
            {
                continue;
            }
            leaderboardEntities.Remove(entity);
            break;
        }
        player.Wallet.TotalCoins.OnValueChanged -= (oldCoins, newCoins) => HandleCoinsChanged(player.OwnerClientId, newCoins);
    }
    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent)
    {
        if (!gameObject.scene.isLoaded)
        {
            return;
        }

        switch (changeEvent.Type)
        {
            case NetworkListEvent<LeaderboardEntityState>.EventType.Add:
                if (!entityDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                {
                    LeaderboardEntity leaderboardEntity = Instantiate(leaderboardEntityPrefab, leaderboardEntithHolder);

                    leaderboardEntity.Initialise(
                        changeEvent.Value.ClientId, 
                        changeEvent.Value.PlayerName, 
                        changeEvent.Value.Coins);

                    if (NetworkManager.Singleton.LocalClientId == changeEvent.Value.ClientId)
                    {
                        leaderboardEntity.SetColour(ownerColor);
                    }

                    entityDisplays.Add(leaderboardEntity);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Remove:
                LeaderboardEntity displayToRemove = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToRemove != null)
                {
                    displayToRemove.transform.SetParent(null);
                    Destroy(displayToRemove.gameObject);
                    entityDisplays.Remove(displayToRemove);   
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                LeaderboardEntity displayToUpdate = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToUpdate != null)
                {
                    displayToUpdate.UpdateCoins(changeEvent.Value.Coins);
                }
                break;
                /*
            case NetworkListEvent<LeaderboardEntityState>.EventType.Insert:
                break;
            case NetworkListEvent<LeaderboardEntityState>.EventType.RemoveAt:
                break;
            case NetworkListEvent<LeaderboardEntityState>.EventType.Clear:
                break;
            case NetworkListEvent<LeaderboardEntityState>.EventType.Full:
                break;
            default:
                break;*/
        }
        entityDisplays.Sort((x, y) => y.Coins.CompareTo(x.Coins));

        for (int i = 0; i < entityDisplays.Count; i++)
        {
            entityDisplays[i].transform.SetSiblingIndex(i);
            entityDisplays[i].UpdateText();
           
            entityDisplays[i].gameObject.SetActive(i <= entitiesToDisplay - 1);
        }

        LeaderboardEntity myDisplay = entityDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);

        if (myDisplay != null)
        {
            if (myDisplay.transform.GetSiblingIndex() >= entitiesToDisplay)
            {
                leaderboardEntithHolder.GetChild(entitiesToDisplay - 1).gameObject.SetActive(false);
                myDisplay.gameObject.SetActive(true);
            }
        }
        if (!teamLeaderboardBG.activeSelf)
        {
            return;
        }

        LeaderboardEntity teamDisplay = teamEntityDisplays.FirstOrDefault(x => x.TeamIndex == changeEvent.Value.TeamIndex);

        if (teamDisplay != null)
        {
            if (changeEvent.Type == NetworkListEvent<LeaderboardEntityState>.EventType.Remove)
            {
                teamDisplay.UpdateCoins(teamDisplay.Coins - changeEvent.Value.Coins);
            }
            else
            {
                teamDisplay.UpdateCoins(teamDisplay.Coins + (changeEvent.Value.Coins - changeEvent.PreviousValue.Coins));   
            }

            teamEntityDisplays.Sort((x, y) => y.Coins.CompareTo(x.Coins));

            for (int i = 0; i < teamEntityDisplays.Count; i++)
            {
                teamEntityDisplays[i].transform.SetSiblingIndex(i);
                teamEntityDisplays[i].UpdateText();
            }
        }

    }
    private void HandleCoinsChanged(ulong clientId, int newCoins)
    {
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId != clientId)
            {
                continue;
            }

            leaderboardEntities[i] = new LeaderboardEntityState
            {
                ClientId = leaderboardEntities[i].ClientId,
                PlayerName = leaderboardEntities[i].PlayerName,
                TeamIndex = leaderboardEntities[i].TeamIndex,
                Coins = newCoins
            };

            return;
        }
    }
}
