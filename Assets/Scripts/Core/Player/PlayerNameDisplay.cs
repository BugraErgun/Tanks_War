using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Collections;
using System;

public class PlayerNameDisplay : MonoBehaviour
{
    [SerializeField] private TankPlayer player;
    [SerializeField] private TMP_Text displayNameText;
    private void Start()
    {
        player.PlayerName.OnValueChanged += HandlePlayerNameChanged;

        HandlePlayerNameChanged(string.Empty, player.PlayerName.Value);
    }
    private void OnDestroy()
    {
        player.PlayerName.OnValueChanged -= HandlePlayerNameChanged;
    }
    private void HandlePlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        displayNameText.text = newName.ToString();
    }
}
