using System;
using System.Collections.Generic;
using Characters.Player.Items.Weapons.Scripts;
using Characters.Player.Scripts;
using Items.Armors.Scripts;
using Items.Weapons.Scripts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Characters.Player.Inventory.Scripts
{
    public class InventoryController : MonoBehaviour
    {
        public GameObject player;
        public GameObject[] inventorySlots;
        public GameObject[] spellSlots;
        public GameObject headEquipment;
        public GameObject bodyEquipment;
        public GameObject equippedWand;

        private GameObject _lootObject;
        private Sprite _emptySlotSprite;
        private PlayerController _playerScript;
        private int _selectedInventorySlotIndex = -1;
        private int _selectedEquipmentSlotIndex = -1;

        private Transform _inventoryGrid;
        private Transform _equipmentGrid;
        private GameObject _lootImage;
        private GameObject _lootSlot;


        private void Awake()
        {
            _inventoryGrid = transform.Find("InventoryGrid");
            _equipmentGrid = transform.Find("EquipmentGrid");
            _lootImage = transform.Find("PickUpBackground").gameObject;
            _lootSlot = transform.Find("LootSlot").gameObject;
            _lootImage.SetActive(false);
            _lootSlot.SetActive(false);
        }
        
        private void Start()
        {
            _emptySlotSprite = _inventoryGrid.GetChild(0).GetComponent<Image>().sprite;
            _playerScript = player.GetComponent<PlayerController>();
            
            RefreshEntireInventory();
            RefreshEquipment();
        }

        public void HandleLoot(GameObject lootObject)
        {
            _lootObject = lootObject;
            _lootSlot.GetComponent<Image>().sprite = lootObject.GetComponent<SpriteRenderer>().sprite;
            Debug.Log(_lootObject);
            _lootImage.SetActive(true);
            _lootSlot.SetActive(true);
        }

        public void CancelLootAction()
        {
            _lootObject = null;
            _lootImage.SetActive(false);
            _lootSlot.SetActive(false);
        }
        
        public void OnInventorySlotClick()
        {
            
            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            var selectedSlot = results[0].gameObject;

            var currentSlotIndex = selectedSlot.transform.GetSiblingIndex();

            if (_lootObject != null)
            {
                TakeLoot(currentSlotIndex);
                return;
            }
            
            if (_selectedEquipmentSlotIndex != -1)
            {
                SwapOutEquipment(currentSlotIndex);
                _selectedEquipmentSlotIndex = -1;
                _selectedInventorySlotIndex = -1;
                return;
            }

            if (_selectedInventorySlotIndex == -1)
            {
                if (inventorySlots[currentSlotIndex] == null) return;
                _selectedInventorySlotIndex = currentSlotIndex;
            }
            else
            {
                (inventorySlots[currentSlotIndex], inventorySlots[_selectedInventorySlotIndex]) = (
                    inventorySlots[_selectedInventorySlotIndex], inventorySlots[currentSlotIndex]);
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    inventorySlots[currentSlotIndex].GetComponent<SpriteRenderer>().sprite;
                if (inventorySlots[_selectedInventorySlotIndex] != null)
                {
                    _inventoryGrid.GetChild(_selectedInventorySlotIndex).GetComponent<Image>().sprite =
                        inventorySlots[_selectedInventorySlotIndex].GetComponent<SpriteRenderer>().sprite;
                }
                else
                {
                    _inventoryGrid.GetChild(_selectedInventorySlotIndex).GetComponent<Image>().sprite =
                        _emptySlotSprite;
                }

                _selectedInventorySlotIndex = -1;
            }
        }

        public void OnEquipmentSlotClick()
        {
            //var selectedSlot = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            var selectedSlot = results[0].gameObject;

            var currentSlotIndex = selectedSlot.transform.GetSiblingIndex();

            if (_selectedEquipmentSlotIndex != -1)
            {
                SwapSpells(currentSlotIndex);
                _selectedEquipmentSlotIndex = -1;
                return;
            }

            if (_selectedInventorySlotIndex == -1)
            {
                switch (currentSlotIndex)
                {
                    case 0 when headEquipment == null:
                    case 1 when bodyEquipment == null:
                    case 2 when equippedWand == null:
                    case 3 when spellSlots[0] == null:
                    case 4 when spellSlots[1] == null:
                    case 5 when spellSlots[2] == null:
                        Debug.Log("empty eq");
                        _selectedEquipmentSlotIndex = -1;
                        return;
                }

                _selectedEquipmentSlotIndex = currentSlotIndex;
            }
            else
            {
                EquipItem(currentSlotIndex, selectedSlot);
                _selectedInventorySlotIndex = -1;
            }
        }

        private void TakeLoot(int currentSlotIndex)
        {
            inventorySlots[currentSlotIndex] = _lootObject;
            _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                _lootObject.GetComponent<SpriteRenderer>().sprite;
            _lootImage.gameObject.SetActive(false);
            _lootSlot.gameObject.SetActive(false);
            _playerScript.CancelLootPickUp();
            _lootObject = null;
        }
        
        private void EquipItem(int currentSlotIndex, GameObject selectedSlot)
        {
            var selectedItem = inventorySlots[_selectedInventorySlotIndex];
            var successfulEquip = false;
            if (selectedItem.CompareTag("Armor") && currentSlotIndex < 2) // 0, 1 = head, body
            {
                var selectedItemScript = selectedItem.GetComponent<ArmorScript>();
                var armorType = selectedItemScript.armorType;
                if ((int)armorType != currentSlotIndex) return;

                if (armorType == ArmorType.HeadArmor)
                {
                    inventorySlots[_selectedInventorySlotIndex] = headEquipment;
                    headEquipment = selectedItem;
                    _playerScript.EquipHeadArmor(selectedItem);
                }
                else
                {
                    inventorySlots[_selectedInventorySlotIndex] = bodyEquipment;

                    bodyEquipment = selectedItem;
                    _playerScript.EquipBodyArmor(selectedItem);
                }

                selectedSlot.GetComponent<Image>().sprite = selectedItemScript.armorSprite;
                successfulEquip = true;
            }

            if (selectedItem.CompareTag("Weapon"))
            {
                var selectedItemScript = selectedItem.GetComponent<WeaponScript>();
                if (currentSlotIndex == 2) // 2 = wand
                {
                    _playerScript.EquipWeapon(selectedItem);
                    selectedSlot.GetComponent<Image>().sprite = selectedItemScript.idleSprite;
                    (equippedWand, inventorySlots[_selectedInventorySlotIndex]) =
                        (inventorySlots[_selectedInventorySlotIndex], equippedWand);

                    successfulEquip = true;
                }
            }

            if (selectedItem.CompareTag("Book"))
            {
                if (currentSlotIndex > 2) // 3, 4, 5 - spell slots
                {
                    _playerScript.EquipSpell(selectedItem, currentSlotIndex - 3);
                    selectedSlot.GetComponent<Image>().sprite = selectedItem.GetComponent<SpriteRenderer>().sprite;
                    inventorySlots[_selectedInventorySlotIndex] = spellSlots[currentSlotIndex - 3];
                    spellSlots[currentSlotIndex - 3] = selectedItem;

                    successfulEquip = true;
                }
            }


            if (!successfulEquip) return;
            _inventoryGrid.GetChild(_selectedInventorySlotIndex).GetComponent<Image>().sprite =
                inventorySlots[_selectedInventorySlotIndex] != null
                    ? inventorySlots[_selectedInventorySlotIndex].GetComponent<SpriteRenderer>().sprite
                    : _emptySlotSprite;
        }

        private void SwapOutEquipment(int currentSlotIndex)
        {
            if (_selectedEquipmentSlotIndex < 2) // head or body
            {
                GameObject inventoryObject = null;

                if (inventorySlots[currentSlotIndex] != null)
                {
                    if (inventorySlots[currentSlotIndex].GetComponent<ArmorScript>()?.armorType == null) return;
                    inventoryObject = inventorySlots[currentSlotIndex];
                }

                var itemArmorType = inventorySlots[currentSlotIndex] == null
                    ? (ArmorType)_selectedEquipmentSlotIndex
                    : inventorySlots[currentSlotIndex].GetComponent<ArmorScript>().armorType;

                inventorySlots[currentSlotIndex] = itemArmorType == ArmorType.HeadArmor ? headEquipment : bodyEquipment;
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    inventorySlots[currentSlotIndex].GetComponent<SpriteRenderer>().sprite;
                if (itemArmorType == ArmorType.HeadArmor)
                {
                    headEquipment = inventoryObject;
                    if (headEquipment != null)
                    {
                        _playerScript.EquipHeadArmor(headEquipment);
                    }
                    else _playerScript.UnequipHeadArmor();
                }
                else
                {
                    bodyEquipment = inventoryObject;
                    if (bodyEquipment != null)
                    {
                        _playerScript.EquipBodyArmor(bodyEquipment);
                    }
                    else _playerScript.UnequipBodyArmor();
                }
            }

            // wand
            if (_selectedEquipmentSlotIndex == 2 && inventorySlots[currentSlotIndex] != null)
            {
                if (inventorySlots[currentSlotIndex]?.GetComponent<WeaponScript>() == null) return;

                (inventorySlots[currentSlotIndex], equippedWand) = (equippedWand, inventorySlots[currentSlotIndex]);
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    inventorySlots[currentSlotIndex].GetComponent<SpriteRenderer>().sprite;
                _playerScript.EquipWeapon(equippedWand);
            }

            if (_selectedEquipmentSlotIndex > 2)
            {
                GameObject inventoryObject = null;


                if (inventorySlots[currentSlotIndex] != null) // swap from inventory
                {
                    if (inventorySlots[currentSlotIndex].GetComponent<ElementBookScript>()?.elementType ==
                        null) return;
                    inventoryObject = inventorySlots[currentSlotIndex];
                }

                _playerScript.EquipSpell(inventoryObject, _selectedEquipmentSlotIndex - 3);
                inventorySlots[currentSlotIndex] = spellSlots[_selectedEquipmentSlotIndex - 3];
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    spellSlots[_selectedEquipmentSlotIndex - 3].GetComponent<SpriteRenderer>().sprite;
                spellSlots[_selectedEquipmentSlotIndex - 3] = inventoryObject;
            }

            RefreshEquipment();
        }

        private void SwapSpells(int currentSlotIndex)
        {
            // swap spells between themselves

            if (currentSlotIndex < 3) return;
            (spellSlots[_selectedEquipmentSlotIndex - 3], spellSlots[currentSlotIndex - 3]) =
                (spellSlots[currentSlotIndex - 3], spellSlots[_selectedEquipmentSlotIndex - 3]);
            _playerScript.EquipSpell(spellSlots[_selectedEquipmentSlotIndex - 3], _selectedEquipmentSlotIndex - 3);
            _playerScript.EquipSpell(spellSlots[currentSlotIndex - 3], currentSlotIndex - 3);
            
            RefreshEquipment();
        }

        private void RefreshEquipment()
        {
            _equipmentGrid.transform.GetChild(0).GetComponent<Image>().sprite = headEquipment != null
                ? headEquipment.GetComponent<SpriteRenderer>().sprite
                : _emptySlotSprite;

            _equipmentGrid.transform.GetChild(1).GetComponent<Image>().sprite = bodyEquipment != null
                ? bodyEquipment.GetComponent<SpriteRenderer>().sprite
                : _emptySlotSprite;

            _equipmentGrid.transform.GetChild(2).GetComponent<Image>().sprite = equippedWand != null
                ? equippedWand.GetComponent<WeaponScript>().idleSprite
                : _emptySlotSprite;

            for (var i = 0; i < 3; i++)
            {
                _equipmentGrid.transform.GetChild(i + 3).GetComponent<Image>().sprite = spellSlots[i] != null
                    ? spellSlots[i].GetComponent<SpriteRenderer>().sprite
                    : _emptySlotSprite;
            }
        }

        private void RefreshEntireInventory()
        {
            for (var slotIndex = 0; slotIndex < inventorySlots.Length; slotIndex++)
            {
                if (inventorySlots[slotIndex] != null)
                {
                    _inventoryGrid.GetChild(slotIndex).GetComponent<Image>().sprite =
                        inventorySlots[slotIndex].GetComponent<SpriteRenderer>().sprite;
                }
            }
        }
    }
}