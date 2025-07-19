using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Steamworks;

namespace SteamLobbyTutorial
{
    public class LobbyUIManager : NetworkBehaviour
    {
        public static LobbyUIManager Instance;
        public Transform playerListParent;
        public List<TextMeshProUGUI> playerNameTexts = new List<TextMeshProUGUI>();
        public List<PlayerLobbyHandler> playerLobbyHandlers = new List<PlayerLobbyHandler>();
        public Button playGameButton;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            playGameButton.interactable = false;
        }

        public void UpdatePlayerLobbyUI()
        {
            playerNameTexts.Clear();
            playerLobbyHandlers.Clear();

            var lobby = new CSteamID(SteamLobby.Instance.lobbyID);
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobby);

            // Clear all slots first
            foreach (Transform child in playerListParent)
            {
                TextMeshProUGUI txtMesh = child.GetChild(0).GetComponent<TextMeshProUGUI>();
                txtMesh.text = "";
                child.gameObject.SetActive(false);
            }

            CSteamID hostID = new CSteamID(ulong.Parse(SteamMatchmaking.GetLobbyData(lobby, "HostAddress")));
            List<CSteamID> orderedMembers = new List<CSteamID>();

            if (memberCount == 0)
            {
                Debug.LogWarning("Lobby has no members.. retrying...");
                StartCoroutine(RetryUpdate());
                return;
            }

            orderedMembers.Add(hostID);

            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (memberID != hostID)
                {
                    orderedMembers.Add(memberID);
                }
            }

            int maxSlots = playerListParent.childCount;
            int maxPlayers = Mathf.Min(maxSlots, orderedMembers.Count);

            for (int j = 0; j < maxPlayers; j++)
            {
                var member = orderedMembers[j];
                Transform slot = playerListParent.GetChild(j);
                slot.gameObject.SetActive(true);

                TextMeshProUGUI txtMesh = slot.GetChild(0).GetComponent<TextMeshProUGUI>();
                PlayerLobbyHandler playerLobbyHandler = slot.GetComponent<PlayerLobbyHandler>();

                playerNameTexts.Add(txtMesh);
                playerLobbyHandlers.Add(playerLobbyHandler);

                string playerName = SteamFriends.GetFriendPersonaName(member);
                txtMesh.text = playerName;
            }

            // If we have more players than slots, log a warning
            if (orderedMembers.Count > maxSlots)
            {
                Debug.LogWarning($"Not enough player UI slots! Players: {orderedMembers.Count}, Slots: {maxSlots}");
            }
        }

        public void OnPlayButtonClicked()
        {
            if (NetworkServer.active)
            {
                CustomNetworkManager.singleton.ServerChangeScene("GameplayScene");
            }
        }

        public void RegisterPlayer(PlayerLobbyHandler player)
        {
            player.transform.SetParent(playerListParent, false);
            UpdatePlayerLobbyUI();
        }

        [Server]
        public void CheckAllPlayersReady()
        {
            foreach (var player in playerLobbyHandlers)
            {
                if (!player.isReady)
                {
                    RpcSetPlayButtonInteractable(false);
                    return;
                }
            }
            RpcSetPlayButtonInteractable(true);
        }

        [ClientRpc]
        void RpcSetPlayButtonInteractable(bool truthStatus)
        {
            playGameButton.interactable = truthStatus;
        }

        private IEnumerator RetryUpdate()
        {
            yield return new WaitForSeconds(1f);
            UpdatePlayerLobbyUI();
        }

    }
}