using System.Collections;
using System.Collections.Generic;
using Characters.Player.Scripts;
using Items.Weapons.Attacks.Scripts;
using Pathfinding;
using UnityEngine;

namespace Characters.Enemies.Scripts
{
    public class EnemyController : MonoBehaviour
    {
        public float health;
        public float arcaneRes;
        public float fireRes;
        public float waterRes;
        public float physicalRes;
        public float speed;
        public float attackDamage;
        public float attackRange;
        public float cooldown;
        public int skillPoints;
        public Transform player;

        private bool _canAttack = true;
        private AIPath _aiPath;
        private AIDestinationSetter _aiDestinationSetter;
        private bool _playerInRange = false;
        private Animator _animator;
        private Rigidbody2D _rb;
        private static readonly int IsMoving = Animator.StringToHash("isMoving");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _aiPath = GetComponent<AIPath>();
            _aiDestinationSetter = GetComponent<AIDestinationSetter>();
            _aiDestinationSetter.target = player;
            _aiPath.maxSpeed = speed;
            _aiPath.enabled = false;
        }

        private void Update()
        {
            if (_playerInRange && !_aiPath.enabled)
            {
                _aiDestinationSetter.target = player;
                _aiPath.enabled = true;
            }
        
            if(!_playerInRange && _aiPath.enabled)
            {
                _aiPath.enabled = false;
                _aiDestinationSetter.target = null;
            }

            _animator.SetBool(IsMoving,_aiPath.velocity.magnitude > 0.01f);
            
            var distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= attackRange && _canAttack)
            {
                StartCoroutine(Attack());
            }
        }

        public void TakeDamage(AttackBehaviour attack)
        {
            var damageGiven = attack.arcaneDamage * arcaneRes +
                              attack.fireDamage * fireRes +
                              attack.waterDamage * waterRes +
                              attack.physicalDamage * physicalRes;
            Debug.Log("Damage: " + damageGiven);
            health -= damageGiven;
            if (health <= 0)
            {
                player.GetComponent<PlayerController>().OnEnemyKill(this);
                Destroy(gameObject);
            }
            else
            {
                if (attack.knockBack <= 0) return;
                var attackDirection = (transform.position - attack.transform.position).normalized;
                _rb.AddForce(attackDirection * attack.knockBack, ForceMode2D.Impulse);
            }
        }
        
        private IEnumerator Attack()
        {
            _canAttack = false;

            Debug.Log("attacked");
            player.GetComponent<PlayerController>().TakeDamage(attackDamage);

            yield return new WaitForSeconds(cooldown);

            _canAttack = true;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                _playerInRange = true;
            }
        }
    
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                _playerInRange = false;
            }
        }
    
    }
}
