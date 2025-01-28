using System;
using System.Collections;
using System.Collections.Generic;
using Textures.Map.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.Player.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 3.0f;
        public GameObject map;
        
        private Rigidbody2D _rb;
        private Vector2 _velocity = Vector2.zero;
        private bool _isTouchingPortal = false;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            _velocity = moveInput * speed;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            Debug.Log("Pressed E");
            if (_isTouchingPortal)
            {
                StartCoroutine(map.GetComponent<GenerateMap>().changeMap(1));
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log("Enter " + collision.transform.tag);
            if (collision.CompareTag("Portal"))
            {
                _isTouchingPortal = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Debug.Log("Exit " + collision.transform.tag);
            if (collision.CompareTag("Portal"))
            {
                _isTouchingPortal = false;
            }
        }

        private void FixedUpdate()
        {
            _rb.velocity = _velocity;
        }
    }
}
