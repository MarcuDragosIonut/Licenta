using UnityEngine;

namespace Characters.Player.Items.Weapons.Attacks.Scripts
{
    public class AttackBehaviour : MonoBehaviour
    {
        public float damage;
        public float travelSpeed;
        public float cooldown;

        public void Use(Vector2 direction, Vector2 spawnPosition)
        {
            var attack = Instantiate(gameObject, spawnPosition, Quaternion.identity);
            attack.GetComponent<Rigidbody2D>().velocity = direction * travelSpeed;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            Destroy(gameObject);
        }
    }
}
