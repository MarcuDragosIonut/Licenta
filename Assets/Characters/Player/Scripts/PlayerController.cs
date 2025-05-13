using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Characters.Enemies.Scripts;
using Items.Armor.Scripts;
using Items.Weapons.Attacks.Scripts;
using Items.Weapons.Scripts;
using Map.Scripts;
using Textures.Map.Scripts;
using UI.Scripts;
using Unity.VisualScripting;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Characters.Player.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public float speed = 3.0f;
        public float health = 100.0f;
        public float maxHealth = 100.0f;
        public float mana = 20;
        public float maxMana = 20;
        public GameObject map;
        public GameObject playerHead;
        public GameObject playerBody;
        public GameObject playerHand;
        public GameObject equippedWand;
        public GameObject currentAttack;
        public GameObject[] equippedSpells = new GameObject[3];
        public GameObject inventory;
        public GameObject skillTab;
        public GameObject playerStatsElement;

        // stats variables
        private int _skillPoints = 0;
        private float _extraAttack = 0.0f;
        private float _manaRegen = 0.1f;
        private ArmorScript _equippedBodyArmor;
        private ArmorScript _equippedHeadArmor;
        
        // player state variables
        private bool _inventoryOpened = false;
        private bool _skillTabOpened = false;
        private bool _isPickingUpLoot = false;
        private bool _isAttacking = false;
        private bool _isTouchingPortal = false;
        private GameObject _collidingLoot;
        
        // attack variables
        private float _currentCooldown = 0.0f;
        private float _lastAttackTime = -Mathf.Infinity;
        private readonly List<ElementType> _preparedCombo = new();
        private readonly List<bool> _pressedKeys = new() { false, false, false };
        private SpellComboController _spellComboController;
        
        
        private InventoryController _inventoryController;
        private SkillsController _skillsController;
        private PlayerStatsController _statsController;

        private Rigidbody2D _rb;
        private Camera _mainCamera;
        private Animator _headAnimator;
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
            _headAnimator = playerHead.GetComponent<Animator>();
            _bodyAnimator = playerBody.GetComponent<Animator>();
            _handAnimator = playerHand.GetComponent<Animator>();
            _statsController = playerStatsElement.GetComponent<PlayerStatsController>();
            _inventoryController = inventory.GetComponent<InventoryController>();
            _skillsController = skillTab.GetComponent<SkillsController>();
            _spellComboController = GetComponent<SpellComboController>();
            EquipWeapon(_inventoryController.equippedWand);
            EquipArmor(_inventoryController.headEquipment);
            EquipArmor(_inventoryController.bodyEquipment);
        }

        private void Start()
        {
            _statsController.UpdateStats(health, maxHealth, mana, maxMana);
            InvokeRepeating(nameof(ManaRegain), 0, 0.5f);
        }

        public void OnInventoryButtonPress(InputAction.CallbackContext context)
        {
            if(!context.performed) return;
            ChangeInventoryVisibility();
        }

        public void OnSkillTabButtonPress(InputAction.CallbackContext context)
        {
            if(!context.performed) return;
            
            skillTab.SetActive(!_skillTabOpened);
            _skillTabOpened = !_skillTabOpened;
        }
        
        private void ChangeInventoryVisibility()
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
            var moveInput = context.ReadValue<Vector2>();
            _velocity = moveInput * speed;
            _headAnimator.SetBool(IsMoving, _velocity.magnitude > 0.01f);
            _bodyAnimator.SetBool(IsMoving, _velocity.magnitude > 0.01f);
            _handAnimator.SetBool(IsMoving, _velocity.magnitude > 0.01f);
        }

        public void DebugGame(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            var mapScript = map.GetComponent<GenerateMap>();
            StartCoroutine(mapScript.ChangeMap());
        }
        
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            
            Debug.Log("Pressed E");
            
            var mapScript = map.GetComponent<GenerateMap>();
            if (_isTouchingPortal && mapScript.CanTeleportToNextMap())
            {
                StartCoroutine(mapScript.ChangeMap());
                return;
            }

            if (_inventoryOpened)
            {
                ChangeInventoryVisibility();
            }
            
            if (_collidingLoot != null && !_isPickingUpLoot && !_collidingLoot.GetComponent<ChestScript>().isOpened)
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
            if (_inventoryOpened || _skillTabOpened) return;

            Vector2 mousePos = context.ReadValue<Vector2>();
            var direction = GetMouseDirection(mousePos);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed || _inventoryOpened || _skillTabOpened|| currentAttack == null) return;

            if (IsReadyToAttack())
            {
                _inventoryController.GetSlotsController().UnhighlightSlots();
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

        public void EquipArmor(GameObject armor)
        {
            if (armor == null) return;
            var armorScript = armor.GetComponent<ArmorScript>();

            if (armorScript.armorType == ArmorType.BodyArmor)
            {
                _equippedBodyArmor = armorScript.GetComponent<ArmorScript>();
                playerBody.GetComponent<SpriteRenderer>().sprite =
                    armorScript.GetComponent<ArmorScript>().equippedArmorSprite;
            }
            else
            {
                _equippedHeadArmor = armorScript.GetComponent<ArmorScript>();
                playerHead.GetComponent<SpriteRenderer>().sprite =
                    armorScript.GetComponent<ArmorScript>().equippedArmorSprite;
            }
        }

        public void UnequipArmor(ArmorType armorType)
        {
            if (armorType == ArmorType.BodyArmor)
            {
                _equippedBodyArmor = null;
                playerBody.GetComponent<SpriteRenderer>().sprite = _baseBodySprite;
            }
            else
            {
                _equippedHeadArmor = null;
                playerHead.GetComponent<SpriteRenderer>().sprite = _baseHeadSprite;
            }
        }

        public void EquipWeapon(GameObject weapon)
        {
            if (weapon == null) return;

            equippedWand = Instantiate(weapon, playerHand.transform);
            equippedWand.GetComponent<SpriteRenderer>().sprite = weapon.GetComponent<SpriteRenderer>().sprite;
            _wandWeaponScript = equippedWand.GetComponent<WeaponScript>();
            _wandSpriteRenderer = equippedWand.GetComponent<SpriteRenderer>();
        }

        public int GetSkillPoints()
        {
            return _skillPoints;
        }
        
        public void UpgradeStat(int skillPointsSpent, SkillsController.StatType statType, float bonusValue)
        {
            _skillPoints -= skillPointsSpent;
            
            if (statType == SkillsController.StatType.Attack) _extraAttack += bonusValue;
            if (statType == SkillsController.StatType.Health) maxHealth += bonusValue;
            if (statType == SkillsController.StatType.Mana) maxMana += bonusValue;
            if (statType == SkillsController.StatType.ManaRegen) _manaRegen += bonusValue;
            
            _statsController.UpdateStats(health, maxHealth, mana, maxMana);
        }
        
        public void PrepareAttack(InputAction.CallbackContext context)
        {
            if (_inventoryOpened || _skillTabOpened) return;

            var keyPressed = context.control.name;
            var selectedSpellSlot = int.Parse(keyPressed);

            if (equippedSpells[selectedSpellSlot - 1] != null && !_pressedKeys[selectedSpellSlot - 1] &&
                IsReadyToAttack())
            {
                _pressedKeys[selectedSpellSlot - 1] = true;
                var bookScript = equippedSpells[selectedSpellSlot - 1].GetComponent<ElementBookScript>();
                _preparedCombo.Add(bookScript.elementType);
                if (_preparedCombo.Count == 1)
                {
                    currentAttack = bookScript.baseAttack;
                    _attackBehaviour = currentAttack.GetComponent<AttackBehaviour>();
                }
            }
        }

        public bool IsReadyToAttack()
        {
            return _isAttacking == false && _lastAttackTime + _currentCooldown < Time.time;
        }

        private IEnumerator HandleAttack()
        {
            _isAttacking = true;
            _handAnimator.SetBool(IsAttacking, true);
            _wandSpriteRenderer.sprite = _wandWeaponScript.activeSprite;

            yield return new WaitForSeconds(0.4f);

            _lastAttackTime = Time.time;

            var weapon = equippedWand.GetComponent<WeaponScript>();

            var arcaneMultiplier = 1.0f +
                (_equippedHeadArmor != null ? _equippedHeadArmor.arcaneMultiplier: 0) +
                (_equippedBodyArmor != null ? _equippedBodyArmor.arcaneMultiplier: 0) +
                weapon.arcaneMultiplier +
                _extraAttack;

            var fireMultiplier = 1.0f + 
                (_equippedHeadArmor != null ? _equippedHeadArmor.fireMultiplier: 0) +
                (_equippedBodyArmor != null ? _equippedBodyArmor.fireMultiplier: 0) +
                weapon.fireMultiplier + 
                _extraAttack;

            var waterMultiplier = 1.0f + 
                (_equippedHeadArmor != null ? _equippedHeadArmor.waterMultiplier : 0) +
                (_equippedBodyArmor != null ? _equippedBodyArmor.waterMultiplier: 0) +
                weapon.waterMultiplier +
                _extraAttack;

            var physicalMultiplier = 1.0f + 
                (_equippedHeadArmor != null ? _equippedHeadArmor.physicalMultiplier: 0) +
                (_equippedBodyArmor != null ? _equippedBodyArmor.physicalMultiplier: 0) +
                weapon.physicalMultiplier +
                _extraAttack;

            float cooldown = 0.0f, manaCost = 0.0f;

            if (_preparedCombo.Count == 1)
            {
                manaCost = _attackBehaviour.manaCost;
                if (manaCost <= mana)
                {
                    _attackBehaviour.Use(true, transform.up,
                        transform.position + transform.up * 1f,
                        arcaneMultiplier, fireMultiplier, waterMultiplier, physicalMultiplier);
                    cooldown = _attackBehaviour.cooldown;
                }
            }
            else
            {
                var comboResult = _spellComboController.GetComboResult(_preparedCombo);
                if (comboResult)
                {
                    if (comboResult.CompareTag("PlayerAttack"))
                    {
                        var comboResultScript = comboResult.GetComponent<AttackBehaviour>();
                        manaCost = comboResultScript.manaCost;
                        if (manaCost <= mana)
                        {
                            cooldown = comboResultScript.cooldown;
                            comboResultScript.Use(true, transform.up,
                                transform.position + transform.up * 1f, arcaneMultiplier, fireMultiplier,
                                waterMultiplier,
                                physicalMultiplier);
                        }
                    }

                    if (comboResult.CompareTag("Effect"))
                    {
                        manaCost = comboResult.GetComponent<EffectBehaviour>().manaCost;
                        if (manaCost <= mana)
                        {
                            var usedEffect = Instantiate(comboResult);
                            var usedEffectScript = usedEffect.GetComponent<EffectBehaviour>();
                            usedEffectScript.Init(transform, comboResult.transform.position);
                            cooldown = usedEffectScript.cooldown;
                        }
                    }
                }
            }

            Debug.Log("CD " + cooldown);
            if (manaCost <= mana)
            {
                mana -= manaCost;
                _statsController.UpdateStats(health, maxHealth, mana, maxMana);
            }

            _currentCooldown = cooldown;
            currentAttack = null;
            _attackBehaviour = null;
            
            // for smoother animation
            yield return new WaitForSeconds(0.1f);

            _wandSpriteRenderer.sprite = _wandWeaponScript.idleSprite;
            _handAnimator.SetBool(IsAttacking, false);
            _pressedKeys[0] = _pressedKeys[1] = _pressedKeys[2] = false;
            _preparedCombo.Clear();
            _isAttacking = false;
        }

        public void TakeDamage(AttackBehaviour attack)
        {
            var damage = GetDamageFromAttack(attack);
            health -= damage;
            _statsController.UpdateStats(health, maxHealth, mana, maxMana);
        }

        private float GetDamageFromAttack(AttackBehaviour attack)
        {
            float arcaneRes = 1.0f, fireRes = 1.0f, waterRes = 1.0f, physRes = 1.0f;

            if (_equippedBodyArmor != null)
            {
                arcaneRes += _equippedBodyArmor.arcaneReductionMultiplier;
                fireRes += _equippedBodyArmor.fireReductionMultiplier;
                waterRes += _equippedBodyArmor.waterReductionMultiplier;
                physRes += _equippedBodyArmor.physicalReductionMultiplier;
            }

            if (_equippedHeadArmor != null)
            {
                arcaneRes += _equippedHeadArmor.arcaneReductionMultiplier;
                fireRes += _equippedHeadArmor.fireReductionMultiplier;
                waterRes += _equippedHeadArmor.waterReductionMultiplier;
                physRes += _equippedHeadArmor.physicalReductionMultiplier;
            }

            return attack.arcaneDamage * arcaneRes + attack.fireDamage * fireRes + attack.waterDamage * waterRes +
                   attack.physicalDamage * physRes;
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

        private void ManaRegain()
        {
            if (mana >= maxMana) return;
            
            mana += Math.Min(_manaRegen, maxMana - mana);
            _statsController.UpdateStats(health, maxHealth, mana, maxMana);
        }

        private void FixedUpdate()
        {
            _rb.velocity = _velocity;
        }
    }
}