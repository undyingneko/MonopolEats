using UnityEngine;
using Mirror;
using TMPro;

namespace SteamLobbyTutorial
{
    public class PlayerLobbyHandler : NetworkBehaviour
    {
        public TextMeshProUGUI nameText;

        public override void OnStartClient()
        {
            base.OnStartClient();
            LobbyUIManager.Instance.RegisterPlayer(this);
        }
    }
}
