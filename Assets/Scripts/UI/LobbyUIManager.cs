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
        public GameObject playerListItemPrefab; // New prefab reference
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

            if (memberCount == 0)
            {
                Debug.LogWarning("Lobby has no members.. retrying...");
                StartCoroutine(RetryUpdate());
                return;
            }

            CSteamID hostID = new CSteamID(ulong.Parse(SteamMatchmaking.GetLobbyData(lobby, "HostAddress")));
            List<CSteamID> orderedMembers = new List<CSteamID> { hostID };

            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (memberID != hostID)
                {
                    orderedMembers.Add(memberID);
                }
            }

            // Ensure enough UI entries
            while (playerListParent.childCount < orderedMembers.Count)
            {
                Instantiate(playerListItemPrefab, playerListParent);
            }

            int j = 0;
            foreach (var member in orderedMembers)
            {
                Transform playerItem = playerListParent.GetChild(j);
                TextMeshProUGUI txtMesh = playerItem.GetChild(0).GetComponent<TextMeshProUGUI>();
                PlayerLobbyHandler playerLobbyHandler = playerItem.GetComponent<PlayerLobbyHandler>();

                playerLobbyHandlers.Add(playerLobbyHandler);
                playerNameTexts.Add(txtMesh);

                string playerName = SteamFriends.GetFriendPersonaName(member);
                playerNameTexts[j].text = playerName;
                j++;
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
