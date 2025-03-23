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
        public Transform player;

        private AIPath _aiPath;
        private AIDestinationSetter _aiDestinationSetter;
        private bool _playerInRange = false;
        private Animator _animator;
        private static readonly int IsMoving = Animator.StringToHash("isMoving");

        private void Awake()
        {
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
