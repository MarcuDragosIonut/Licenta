using System;
using Characters.Player.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Scripts
{
    public class UIEffectController : MonoBehaviour
    {
        private Image _cooldownIcon;
        private PlayerController _playerController;

        private void Awake()
        {
            _cooldownIcon = transform.GetChild(0).GetComponent<Image>();
            _cooldownIcon.enabled = false;
            _playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!_cooldownIcon.enabled && !_playerController.IsReadyToAttack())
                _cooldownIcon.enabled = true;
            if (_cooldownIcon.enabled && _playerController.IsReadyToAttack())
                _cooldownIcon.enabled = false;
        }
    }
}
