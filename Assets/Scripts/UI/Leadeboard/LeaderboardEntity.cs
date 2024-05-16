using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardEntity : MonoBehaviour
{
    [SerializeField] TMP_Text displayText;

    private FixedString32Bytes displayName;

    public int TeamIndex {  get; private set; }
    public ulong ClientId { get; private set; }
    public int Coins { get; private set; }
    public void SetColour(Color color)
    {
        displayText.color = color;
    }
    public void Initialise(ulong clientId,FixedString32Bytes displayName,int coins)
    {
        this.ClientId = clientId;
        this.displayName = displayName;

        UpdateCoins(coins);
    }
    public void Initialise(int teamIndex, FixedString32Bytes displayName, int coins)
    {
        TeamIndex = teamIndex;
        this.displayName = displayName;

        UpdateCoins(coins);
    }
    public void UpdateCoins(int coins)
    {
        Coins = coins;
        UpdateText();
    }
    public void UpdateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}.{displayName} ({Coins})";
    }
}
