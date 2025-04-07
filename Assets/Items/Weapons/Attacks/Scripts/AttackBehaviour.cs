using System;
using Characters.Enemies.Scripts;
using Characters.Player.Scripts;
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
        public bool isRanged = true;
        public float attackStagger = 0.5f;

        private bool _playerAttack = true;

        public void Use(bool isPlayerAttack, Vector2 direction, Vector2 spawnPosition, float arcaneMultiplier = 1.0f,
            float fireMultiplier = 1.0f, float waterMultiplier = 1.0f, float physicalMultiplier = 1.0f)
        {
            if (!isRanged) return;

            var modifiedArcaneDamage = arcaneDamage * arcaneMultiplier;
            var modifiedFireDamage = fireDamage * fireMultiplier;
            var modifiedWaterDamage = waterDamage * waterMultiplier;
            var modifiedPhysicalDamage = physicalDamage * physicalMultiplier;

            var attack = Instantiate(gameObject, spawnPosition, Quaternion.identity);
            var attackScript = attack.GetComponent<AttackBehaviour>();

            attackScript.SetDamage(modifiedArcaneDamage, modifiedFireDamage, modifiedWaterDamage,
                modifiedPhysicalDamage);
            attackScript.SetPlayerAttack(isPlayerAttack);

            attack.GetComponent<Rigidbody2D>().velocity = direction * travelSpeed;
        }

        public void Use(PlayerController playerController)
        {
            if (isRanged) return;

            playerController.TakeDamage(this);
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
            switch (_playerAttack)
            {
                case true when other.gameObject.CompareTag("Enemy"):
                    other.gameObject.GetComponent<EnemyController>().TakeDamage(this);
                    break;
                case false when other.gameObject.CompareTag("Player"):
                    other.gameObject.GetComponent<PlayerController>().TakeDamage(this);
                    break;
            }
            
            Destroy(gameObject);
        }
        
        private void SetPlayerAttack(bool val)
        {
            _playerAttack = val;
        }
    }
}