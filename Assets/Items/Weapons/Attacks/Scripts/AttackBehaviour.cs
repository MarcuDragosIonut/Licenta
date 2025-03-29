using Characters.Enemies.Scripts;
using Items.Armors.Scripts;
using Items.Weapons.Scripts;
using UnityEngine;

namespace Items.Weapons.Attacks.Scripts
{
    public class AttackBehaviour : MonoBehaviour
    {
        public float arcaneDamage = 0.0f;
        public float fireDamage = 0.0f;
        public float waterDamage = 0.0f;
        public float physicalDamage = 0.0f;
        public float knockBack = 0.0f;
        public float travelSpeed;
        public float cooldown;

        public void Use(Vector2 direction, Vector2 spawnPosition, float arcaneMultiplier, float fireMultiplier, float waterMultiplier, float physicalMultiplier)
        {
            var modifiedArcaneDamage = arcaneDamage * arcaneMultiplier;
            var modifiedFireDamage = fireDamage * fireMultiplier;
            var modifiedWaterDamage = waterDamage * waterMultiplier;
            var modifiedPhysicalDamage = physicalDamage * physicalMultiplier;
            
            var attack = Instantiate(gameObject, spawnPosition, Quaternion.identity);
            var attackScript = attack.GetComponent<AttackBehaviour>();

            attackScript.SetDamage(modifiedArcaneDamage, modifiedFireDamage, modifiedWaterDamage, modifiedPhysicalDamage);

            attack.GetComponent<Rigidbody2D>().velocity = direction * travelSpeed;
        }

        private void SetDamage(float arcane, float fire, float water, float physical)
        {
            arcaneDamage = arcane;
            fireDamage = fire;
            waterDamage = water;
            physicalDamage = physical;
        }


        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                other.gameObject.GetComponent<EnemyController>().TakeDamage(this);
            }

            Destroy(gameObject);
        }
    }
}