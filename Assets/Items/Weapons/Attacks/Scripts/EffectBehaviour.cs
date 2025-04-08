using System;
using System.Collections;
using UnityEngine;

namespace Items.Weapons.Attacks.Scripts
{
    public class EffectBehaviour : MonoBehaviour
    {
        private bool _isActive = true;
        private Transform _player;
        private Vector3 _initialOffset;

        public void Init(Transform player, Vector3 offset)
        {
            _initialOffset = offset;
            Debug.Log("init " + _initialOffset);
            _player = player;
            StartCoroutine(ShieldDuration());
        }

        private IEnumerator ShieldDuration()
        {
            yield return new WaitForSeconds(10f);
            if (_isActive) Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.gameObject.CompareTag("EnemyAttack") || !_isActive) return;
            _isActive = false;
            Destroy(gameObject);
        }

        private void Update()
        {
            var rotatedOffset = _player.rotation * _initialOffset;
            transform.position = _player.position + rotatedOffset;
            transform.rotation = _player.rotation;
        }
    }
}
