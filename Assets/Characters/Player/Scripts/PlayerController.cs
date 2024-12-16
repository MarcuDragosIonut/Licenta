using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.Player.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 3.0f;

        private Rigidbody2D _rb;
        private Vector2 _velocity = Vector2.zero;
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            _velocity = moveInput * speed;
        }
        
        void FixedUpdate()
        {
            _rb.velocity = _velocity;
        }
    }
}
