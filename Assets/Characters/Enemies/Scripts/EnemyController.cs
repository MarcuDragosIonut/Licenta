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
        public float attackRange;
        public int skillPoints;
        public Transform player;
        public GameObject[] attackPool;

        private bool _canAttack = true;
        private AIPath _aiPath;
        private AIDestinationSetter _aiDestinationSetter;
        private bool _playerInRange = false;
        private float _attackCooldown = -Mathf.Infinity;
        private Animator _animator;
        private Rigidbody2D _rb;
        private PlayerController _playerController;
        private static readonly int IsMoving = Animator.StringToHash("isMoving");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _playerController = player.GetComponent<PlayerController>();
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

            if (!_playerInRange && _aiPath.enabled)
            {
                _aiPath.enabled = false;
                _aiDestinationSetter.target = null;
            }

            if(_animator) _animator.SetBool(IsMoving, _aiPath.velocity.magnitude > 0.01f);

            var distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= attackRange)
            {
                if (_canAttack && _attackCooldown < Time.time)
                {
                    Attack();
                }

                if (attackRange >= 1.1f)
                {
                    _aiPath.canMove = false;
                    Vector2 direction = player.position - transform.position;
                    var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle - 90);
                }
            }
            else
            {
                if (!_aiPath.canMove) _aiPath.canMove = true;
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
                var attackDirection = (_rb.position - (Vector2)attack.transform.position).normalized;
                _rb.AddForce(attackDirection * attack.knockBack, ForceMode2D.Impulse);
            }
        }

        private void Attack()
        {
            _canAttack = false;
            var attackStagger = 0.0f;
            var chosenAttack = attackPool[Random.Range(0, attackPool.Length)].GetComponent<AttackBehaviour>();
            
            if (attackRange < 1.1f)
            {
                chosenAttack.Use(_playerController);
                attackStagger = chosenAttack.attackStagger;
            }
            else
            {
                chosenAttack.Use(isPlayerAttack: false, transform.up, transform.position + transform.up * 1f);
                attackStagger = chosenAttack.attackStagger;
            }

            _attackCooldown = Time.time + chosenAttack.cooldown;
            StartCoroutine(Stun(attackStagger));

            _canAttack = true;
        }

        private IEnumerator Stun(float duration)
        {
            _aiPath.canMove = false;

            yield return new WaitForSeconds(duration);

            _aiPath.canMove = true;
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