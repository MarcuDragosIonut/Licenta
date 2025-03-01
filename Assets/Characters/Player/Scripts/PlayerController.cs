using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Player.Weapons.Attacks.Scripts;
using Characters.Player.Weapons.Scripts;
using Textures.Map.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.Player.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 3.0f;
        public GameObject map;
        public GameObject playerBody;
        public GameObject playerHand;
        public GameObject equippedWand;
        public GameObject currentAttack;

        private bool _isAttacking = false;
        private bool _isTouchingPortal = false;
        private float _lastAttackTime = -Mathf.Infinity;
        private Rigidbody2D _rb;
        private Camera _mainCamera;
        private Animator _bodyAnimator;
        private Animator _handAnimator;
        private SpriteRenderer _wandSpriteRenderer;
        private WeaponScript _wandWeaponScript;
        private AttackBehaviour _attackBehaviour;
        private Vector2 _velocity = Vector2.zero;
        private static readonly int IsMoving = Animator.StringToHash("isMoving");
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");

        private void Awake()
        {
            _wandWeaponScript = equippedWand.GetComponent<WeaponScript>();
            _rb = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;
            _bodyAnimator = playerBody.GetComponent<Animator>();
            _handAnimator = playerHand.GetComponent<Animator>();
            _wandSpriteRenderer = equippedWand.GetComponent<SpriteRenderer>();
            _attackBehaviour = currentAttack.GetComponent<AttackBehaviour>();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            _velocity = moveInput * speed;
            _bodyAnimator.SetBool(IsMoving, _velocity.magnitude > 0.01f);
            _handAnimator.SetBool(IsMoving, _velocity.magnitude > 0.01f);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            Debug.Log("Pressed E");
            if (_isTouchingPortal)
            {
                StartCoroutine(map.GetComponent<GenerateMap>().changeMap(1));
            }
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            Vector2 mousePos = context.ReadValue<Vector2>();
            var direction = GetMouseDirection(mousePos);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Debug.Log("Clicked");
            if (_isAttacking == false && _lastAttackTime + _attackBehaviour.cooldown < Time.time)
            {
                StartCoroutine(HandleAttack());
            }
        }

        private IEnumerator HandleAttack()
        {
            _isAttacking = true;
            _handAnimator.SetBool(IsAttacking, true);
            _wandSpriteRenderer.sprite = _wandWeaponScript.activeSprite;
            _lastAttackTime = Time.time;

            yield return new WaitForSeconds(0.5f);
            
            _attackBehaviour.Use(GetMouseDirection(Mouse.current.position.ReadValue()),
                transform.position + transform.up * 1f);

            yield return new WaitForSeconds(1.5f);

            _wandSpriteRenderer.sprite = _wandWeaponScript.idleSprite;
            _handAnimator.SetBool(IsAttacking, false);
            _isAttacking = false;
        }

        private Vector2 GetMouseDirection(Vector2 mousePos)
        {
            Vector3 worldMousePos =
                _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _mainCamera.nearClipPlane));
            Vector2 direction = (worldMousePos - transform.position).normalized;
            return direction;
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