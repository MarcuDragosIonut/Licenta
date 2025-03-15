using System;
using System.Collections;
using System.Collections.Generic;
using Characters.Player.Inventory.Scripts;
using Characters.Player.Items.Armors.Scripts;
using Characters.Player.Items.Weapons.Attacks.Scripts;
using Characters.Player.Items.Weapons.Scripts;
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
        public GameObject playerHead;
        public GameObject playerBody;
        public GameObject playerHand;
        public GameObject equippedWand;
        public GameObject currentAttack;
        public GameObject inventory;
        public GameObject[] equippedSpells = new GameObject[3];

        private bool _inventoryOpened = false;
        private bool _isAttacking = false;
        private bool _isTouchingPortal = false;
        private float _lastAttackTime = -Mathf.Infinity;
        private InventoryController _inventoryController;
        private Rigidbody2D _rb;
        private Camera _mainCamera;
        private Animator _bodyAnimator;
        private Animator _handAnimator;
        private Sprite _baseHeadSprite;
        private Sprite _baseBodySprite;
        private SpriteRenderer _wandSpriteRenderer;
        private WeaponScript _wandWeaponScript;
        private AttackBehaviour _attackBehaviour;
        private Vector2 _velocity = Vector2.zero;
        private static readonly int IsMoving = Animator.StringToHash("isMoving");
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");

        private void Awake()
        {
            _baseBodySprite = playerBody.GetComponent<SpriteRenderer>().sprite;
            _baseHeadSprite = playerHead.GetComponent<SpriteRenderer>().sprite;
            _rb = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;
            _bodyAnimator = playerBody.GetComponent<Animator>();
            _handAnimator = playerHand.GetComponent<Animator>();
            //_attackBehaviour = currentAttack.GetComponent<AttackBehaviour>();
            _inventoryController = inventory.GetComponent<InventoryController>();
            EquipWeapon(_inventoryController.equippedWand);
            EquipHeadArmor(_inventoryController.headEquipment);
            EquipBodyArmor(_inventoryController.bodyEquipment);
        }

        public void ChangeInventoryVisibility()
        {
            inventory.SetActive(!_inventoryOpened);
            _inventoryOpened = !_inventoryOpened;
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
            if (_inventoryOpened) return;
            
            Vector2 mousePos = context.ReadValue<Vector2>();
            var direction = GetMouseDirection(mousePos);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed || _inventoryOpened || currentAttack == null) return;

            //Debug.Log("Clicked");
            if (_isAttacking == false && _lastAttackTime + _attackBehaviour.cooldown < Time.time)
            {
                StartCoroutine(HandleAttack());
            }
        }

        public void EquipSpell(GameObject spellItem, int spellPosition)
        {
            if (equippedSpells[spellPosition] != null)
            {
                equippedSpells[spellPosition] = null;
            }
            else
            {
                if (spellItem == null) return;
                equippedSpells[spellPosition] = spellItem;
            }
        }
        
        public void EquipBodyArmor(GameObject bodyArmor)
        {
            if (bodyArmor == null) return;
            
            playerBody.GetComponent<SpriteRenderer>().sprite = bodyArmor.GetComponent<ArmorScript>().equippedArmorSprite;
        }

        public void UnequipBodyArmor()
        {
            playerBody.GetComponent<SpriteRenderer>().sprite = _baseBodySprite;
        }
        
        public void EquipHeadArmor(GameObject headArmor)
        {
            if (headArmor == null) return;
            
            playerHead.GetComponent<SpriteRenderer>().sprite = headArmor.GetComponent<ArmorScript>().equippedArmorSprite;
        }

        public void UnequipHeadArmor()
        {
            playerHead.GetComponent<SpriteRenderer>().sprite = _baseHeadSprite;
        }

        public void EquipWeapon(GameObject weapon)
        {
            if (weapon == null) return;
            
            equippedWand = Instantiate(weapon, playerHand.transform);
            equippedWand.GetComponent<SpriteRenderer>().sprite = weapon.GetComponent<SpriteRenderer>().sprite;
            _wandWeaponScript = equippedWand.GetComponent<WeaponScript>();
            _wandSpriteRenderer = equippedWand.GetComponent<SpriteRenderer>();
        }

        public void PrepareAttack(InputAction.CallbackContext context)
        {
            var keyPressed = context.control.name;
            var selectedSpellSlot = int.Parse(keyPressed);

            if (equippedSpells[selectedSpellSlot - 1] != null)
            {
                currentAttack = equippedSpells[selectedSpellSlot - 1].GetComponent<ElementBookScript>().baseAttack;
                _attackBehaviour = currentAttack.GetComponent<AttackBehaviour>();
            }
        }
        
        private IEnumerator HandleAttack()
        {
            _isAttacking = true;
            _handAnimator.SetBool(IsAttacking, true);
            _wandSpriteRenderer.sprite = _wandWeaponScript.activeSprite;

            yield return new WaitForSeconds(0.5f);
            
            _lastAttackTime = Time.time;
            _attackBehaviour.Use(transform.up,
                transform.position + transform.up * 1f);
            currentAttack = null;
            _attackBehaviour = null;
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