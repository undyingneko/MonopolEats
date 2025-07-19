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

        private const int MaxPlayers = 4;

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

            if (playerListParent.childCount < MaxPlayers)
            {
                Debug.LogError($"Not enough UI slots! Expected {MaxPlayers}, but found {playerListParent.childCount}");
                return;
            }

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

            Debug.Log($"playerListParent child count: {playerListParent.childCount}");
            Debug.Log($"Ordered members count: {orderedMembers.Count}");

            int j = 0;
            foreach (var member in orderedMembers)
            {
                if (j >= MaxPlayers)
                {
                    Debug.LogWarning($"Reached max player UI slots at index {j}, skipping remaining players.");
                    break;
                }

                Transform playerEntry = playerListParent.GetChild(j);
                if (playerEntry.childCount == 0)
                {
                    Debug.LogError($"playerListParent child at index {j} has no children!");
                    break;
                }

                TextMeshProUGUI txtMesh = playerEntry.GetChild(0).GetComponent<TextMeshProUGUI>();
                PlayerLobbyHandler playerLobbyHandler = playerEntry.GetComponent<PlayerLobbyHandler>();

                playerLobbyHandlers.Add(playerLobbyHandler);
                playerNameTexts.Add(txtMesh);

                string playerName = SteamFriends.GetFriendPersonaName(member);
                txtMesh.text = playerName;

                Debug.Log($"Set player name: {playerName} at index {j}");

                j++;
            }

            // Clear remaining slots if fewer players
            for (int k = j; k < MaxPlayers; k++)
            {
                Transform playerEntry = playerListParent.GetChild(k);
                if (playerEntry.childCount > 0)
                {
                    TextMeshProUGUI txtMesh = playerEntry.GetChild(0).GetComponent<TextMeshProUGUI>();
                    txtMesh.text = "Empty Slot";
                }

                PlayerLobbyHandler handler = playerEntry.GetComponent<PlayerLobbyHandler>();
                if (handler != null)
                {
                    handler.ResetState(); // You can implement ResetState() in PlayerLobbyHandler to clear ready status etc.
                }
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
