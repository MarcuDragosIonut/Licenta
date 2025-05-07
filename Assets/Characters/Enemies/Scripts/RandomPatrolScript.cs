using System;
using System.Collections;
using Pathfinding;
using Textures.Map.Scripts;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Characters.Enemies.Scripts
{
    public class RandomPatrolScript : MonoBehaviour
    {
        private EnemyController _enemyController;
        private GenerateMap _mapController;
        private AIPath _aiPath;
        private bool _readyToMove = false;
        private const float GracePeriod = 2.5f;
        private float _lastEnemyDetectTime = -Mathf.Infinity;
        
        private void Start()
        {
            _enemyController = GetComponent<EnemyController>();
            _mapController = GameObject.Find("Map").GetComponent<GenerateMap>();
            _aiPath = GetComponent<AIPath>();
            _aiPath.enabled = true;
            _aiPath.enableRotation = true;
            StartCoroutine(GetNewPath(1f));
        }

        private void Update()
        {
            var result = Physics2D.OverlapCircle(transform.position, 0.75f, LayerMask.GetMask("Enemy"));
            if (result.gameObject == gameObject) result = null;
            if (_readyToMove && (_aiPath.reachedDestination || (result && GracePeriod + _lastEnemyDetectTime < Time.time)))
            {
                if (result != null) _lastEnemyDetectTime = Time.time;
                _aiPath.canMove = false;
                StartCoroutine(GetNewPath(result ? 1.5f : 4f));
            }
        }

        private IEnumerator GetNewPath(float cooldown)
        {
            _readyToMove = false;
            yield return new WaitForSeconds(cooldown);
            var freeTiles = _mapController.GetFreeTilesInRoom(Mathf.FloorToInt(transform.position.x),
                Mathf.FloorToInt(transform.position.y));
            var chosenTile = freeTiles[Random.Range(0, freeTiles.Count)];
            _aiPath.destination = new Vector2(chosenTile.x * 2 + 0.5f, chosenTile.y * 2);
            _aiPath.canMove = true;
            _readyToMove = true;
        }
    }
}