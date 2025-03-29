using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Characters.Enemies.Scripts;
using Characters.Player.Inventory.Scripts;
using Characters.Player.Items.Weapons.Scripts;
using Items.Armors.Scripts;
using Items.Weapons.Attacks.Scripts;
using Items.Weapons.Scripts;
using Map.Scripts;
using Textures.Map.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.Player.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 3.0f;
        public float health = 100.0f;
        public GameObject map;
        public GameObject interactionArea;
        public GameObject playerHead;
        public GameObject playerBody;
        public GameObject playerHand;
        public GameObject equippedWand;
        public GameObject currentAttack;
        public GameObject inventory;
        public GameObject[] equippedSpells = new GameObject[3];

        private GameObject _equippedBodyArmor;
        private GameObject _equippedHeadArmor;
        private int _skillPoints = 0;
        private bool _inventoryOpened = false;
        private bool _isPickingUpLoot = false;
        private bool _isAttacking = false;
        private bool _isTouchingPortal = false;
        private GameObject _collidingLoot;
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
            if (_isPickingUpLoot)
            {
                inventory.GetComponent<InventoryController>().CancelLootAction();
                _isPickingUpLoot = false;
            }

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
                return;
            }

            if (_collidingLoot!=null && !_isPickingUpLoot && !_collidingLoot.GetComponent<ChestScript>().isOpened)
            {
                _isPickingUpLoot = true;
                _inventoryOpened = true;
                inventory.SetActive(true);
                inventory.GetComponent<InventoryController>()
                    .HandleLoot(_collidingLoot.GetComponent<ChestScript>().GetLoot());
            }
        }

        public void CancelLootPickUp()
        {
            _isPickingUpLoot = false;
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

        public void OnEnemyKill(EnemyController enemy)
        {
            _skillPoints += enemy.skillPoints;
        }
        
        public void EquipSpell(GameObject spellItem, int spellPosition)
        {
            equippedSpells[spellPosition] = spellItem;
        }

        public void EquipBodyArmor(GameObject bodyArmor)
        {
            if (bodyArmor == null) return;

            _equippedBodyArmor = bodyArmor;
            playerBody.GetComponent<SpriteRenderer>().sprite =
                bodyArmor.GetComponent<ArmorScript>().equippedArmorSprite;
        }

        public void UnequipBodyArmor()
        {
            _equippedBodyArmor = null;
            playerBody.GetComponent<SpriteRenderer>().sprite = _baseBodySprite;
        }

        public void EquipHeadArmor(GameObject headArmor)
        {
            if (headArmor == null) return;

            _equippedHeadArmor = headArmor;
            playerHead.GetComponent<SpriteRenderer>().sprite =
                headArmor.GetComponent<ArmorScript>().equippedArmorSprite;
        }

        public void UnequipHeadArmor()
        {
            _equippedHeadArmor = null;
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
            if (_inventoryOpened) return;

            var keyPressed = context.control.name;
            var selectedSpellSlot = int.Parse(keyPressed);

            if (equippedSpells[selectedSpellSlot - 1] != null)
            {
                currentAttack = equippedSpells[selectedSpellSlot - 1].GetComponent<ElementBookScript>().baseAttack;
                _attackBehaviour = currentAttack.GetComponent<AttackBehaviour>();
            }
        }

        public void TakeDamage(float damage)
        {
            var bodyReduction = _equippedBodyArmor != null ? _equippedBodyArmor.GetComponent<ArmorScript>().damageReductionMultiplier : 1.0f;
            var headReduction = _equippedHeadArmor != null ?_equippedHeadArmor.GetComponent<ArmorScript>().damageReductionMultiplier : 1.0f;
            health -= damage * (
                bodyReduction +
                headReduction - 1.0f
                );
        }
        
        private IEnumerator HandleAttack()
        {
            _isAttacking = true;
            _handAnimator.SetBool(IsAttacking, true);
            _wandSpriteRenderer.sprite = _wandWeaponScript.activeSprite;

            yield return new WaitForSeconds(0.5f);

            //Debug.Log("PlayerHead: " + _equippedHeadArmor.GetComponent<ArmorScript>().arcaneMultiplier);
            _lastAttackTime = Time.time;

            var headArmor = _equippedHeadArmor != null ?_equippedHeadArmor.GetComponent<ArmorScript>() : null;
            var bodyArmor = _equippedBodyArmor != null ?  _equippedBodyArmor.GetComponent<ArmorScript>() : null;
            var weapon = equippedWand.GetComponent<WeaponScript>();

            var arcaneMultiplier = 1.0f + (
                (headArmor != null ? headArmor.arcaneMultiplier - 1.0f : 0) +
                (bodyArmor != null ? bodyArmor.arcaneMultiplier - 1.0f : 0) +
                weapon.arcaneMultiplier - 1.0f);
            
            var fireMultiplier = 1.0f + (
                (headArmor != null ? headArmor.fireMultiplier - 1.0f : 0) +
                (bodyArmor != null ? bodyArmor.fireMultiplier - 1.0f : 0) +
                weapon.fireMultiplier - 1.0f);
            
            var waterMultiplier = 1.0f + (
                (headArmor != null ? headArmor.waterMultiplier - 1.0f : 0) +
                (bodyArmor != null ? bodyArmor.waterMultiplier - 1.0f : 0) +
                weapon.waterMultiplier - 1.0f);

            var physicalMultiplier = 1.0f + (
                (headArmor != null ? headArmor.physicalMultiplier - 1.0f : 0) +
                (bodyArmor != null ? bodyArmor.physicalMultiplier - 1.0f : 0) +
                weapon.physicalMultiplier - 1.0f);

            Debug.Log("arc mult: " + arcaneMultiplier);
            
            _attackBehaviour.Use(transform.up,
                transform.position + transform.up * 1f,
                arcaneMultiplier, fireMultiplier, waterMultiplier, physicalMultiplier);
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

            if (collision.CompareTag("Loot"))
            {
                Debug.Log("loot");
                if (!collision.GetComponent<ChestScript>().isOpened) _collidingLoot = collision.gameObject;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Debug.Log("Exit " + collision.transform.tag);
            if (collision.CompareTag("Portal"))
            {
                _isTouchingPortal = false;
            }

            if (collision.CompareTag("Loot"))
            {
                _collidingLoot = null;
            }
        }

        private void FixedUpdate()
        {
            _rb.velocity = _velocity;
        }
    }
}