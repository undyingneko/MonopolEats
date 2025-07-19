using UnityEngine;
using Mirror;

namespace SteamLobbyTutorial
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovementHandler : NetworkBehaviour
    {
        public float moveSpeed = 5f;
        private Rigidbody2D rb;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer) return;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector2 movement = new Vector2(h, v) * moveSpeed;
            rb.velocity = movement;
        }
    }
}
