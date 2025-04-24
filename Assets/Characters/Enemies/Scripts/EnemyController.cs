using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Player.Scripts;
using Items.Weapons.Attacks.Scripts;
using Pathfinding;
using Textures.Map.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public GameObject[] attackPool;

        private bool _effectsApplied = false;
        private bool _canAttack = true;
        private AIPath _aiPath;
        private AIDestinationSetter _aiDestinationSetter;
        private bool _playerInRange = false;
        private float _attackCooldown = -Mathf.Infinity;
        private Animator _animator;
        private Rigidbody2D _rb;
        private PlayerController _playerController;
        private Transform _player;
        private GenerateMap _mapScript;
        private static readonly int IsMoving = Animator.StringToHash("isMoving");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _player = GameObject.Find("Player").transform;
            _playerController = _player.GetComponent<PlayerController>();
            _aiPath = GetComponent<AIPath>();
            _aiDestinationSetter = GetComponent<AIDestinationSetter>();
            _aiDestinationSetter.target = _player;
            _aiPath.maxSpeed = speed;
            _aiPath.enabled = false;
            _mapScript = GameObject.Find("Map").GetComponent<GenerateMap>();
        }

        private void Update()
        {
            if (_playerInRange && !_aiPath.enabled)
            {
                _aiDestinationSetter.target = _player;
                _aiPath.enabled = true;
            }

            if (!_playerInRange && _aiPath.enabled)
            {
                _aiPath.enabled = false;
                _aiDestinationSetter.target = null;
            }

            if(_animator) _animator.SetBool(IsMoving, _aiPath.velocity.magnitude > 0.01f);

            if (_effectsApplied) return;
            
            var distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            var hit = Physics2D.Raycast(transform.position, _player.position - transform.position, distanceToPlayer, LayerMask.GetMask("Obstacle"));
            if (distanceToPlayer <= attackRange && !hit.collider)
            {
                if (attackRange >= 1.1f)
                {
                    _aiPath.canMove = false;
                    Vector2 direction = _player.position - transform.position;
                    var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle - 90);
                }

                if (_canAttack && _attackCooldown < Time.time)
                {
                    Attack();
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
            if (health <= 0) // on death behaviour
            {
                _player.GetComponent<PlayerController>().OnEnemyKill(this);
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Knckbck " + attack.knockBack);
                if (attack.knockBack > 0) StartCoroutine(HandleKnockBack(attack));
                if (attack.slowEffect > 0) StartCoroutine(HandleSlow(attack));
            }
        }

        private void OnDestroy()
        {
            _mapScript.DecrementEnemyCount();
        }

        private IEnumerator HandleKnockBack(AttackBehaviour attack)
        {
            _effectsApplied = true;
            _aiPath.canMove = false;
            var attackDirection = (_rb.position - (Vector2)attack.transform.position).normalized;
            _rb.velocity = attackDirection * attack.knockBack;
            Debug.Log("KnockBack " + _rb.velocity);
            yield return new WaitForSeconds(0.15f);
            _rb.velocity = Vector2.zero;
            _aiPath.canMove = true;
            _effectsApplied = false;
        }

        private IEnumerator HandleSlow(AttackBehaviour attack)
        {
            var actualSlow = Math.Min(attack.slowEffect, _aiPath.maxSpeed - 0.05f);
            _aiPath.maxSpeed -= actualSlow;
            yield return new WaitForSeconds(attack.slowDuration);
            _aiPath.maxSpeed += actualSlow;
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
            //StartCoroutine(Stun(attackStagger));

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