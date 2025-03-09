using System.Net.Mime;
using Characters.Player.Items.Armors.Scripts;
using Characters.Player.Items.Weapons.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Characters.Player.Scripts
{
    public class InventoryController : MonoBehaviour
    {
        public GameObject player;
        public GameObject[] inventorySlots;
        public GameObject headEquipment;
        public GameObject bodyEquipment;
        public GameObject equippedWand;

        private Sprite _emptySlotSprite;
        private PlayerController _playerScript;
        private Transform _inventoryGrid;
        private Transform _equipmentGrid;
        private int _selectedInventorySlotIndex = -1;
        private int _selectedEquipmentSlotIndex = -1;

        private void Start()
        {
            _inventoryGrid = transform.Find("InventoryGrid");
            _equipmentGrid = transform.Find("EquipmentGrid");
            _emptySlotSprite = _inventoryGrid.GetChild(0).GetComponent<Image>().sprite;
            //load ui
            _equipmentGrid.GetChild(0).transform.GetSiblingIndex();
            _inventoryGrid.GetChild(0).transform.GetSiblingIndex();
            //end load ui
            _playerScript = player.GetComponent<PlayerController>();
            inventorySlots = new GameObject[_inventoryGrid.childCount];

            RefreshEquipment();
        }

        public void OnInventorySlotClick()
        {
            var selectedSlot = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            var currentSlotIndex = selectedSlot.transform.GetSiblingIndex();

            Debug.Log("inv " + currentSlotIndex + " inv: " + _selectedInventorySlotIndex + " eq: " + _selectedEquipmentSlotIndex);

            if (_selectedEquipmentSlotIndex != -1)
            {
                SwapOutEquipment(currentSlotIndex);
                _selectedEquipmentSlotIndex = -1;
                _selectedInventorySlotIndex = -1;
                return;
            }

            if (_selectedInventorySlotIndex != currentSlotIndex && inventorySlots[currentSlotIndex] != null)
            {
                _selectedInventorySlotIndex = currentSlotIndex;
            }
            else
            {
                _selectedInventorySlotIndex = -1;
            }
            
            Debug.Log("inv end " + currentSlotIndex + " inv: " + _selectedInventorySlotIndex + " eq: " + _selectedEquipmentSlotIndex);

        }

        public void OnEquipmentSlotClick()
        {
            var selectedSlot = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            var currentSlotIndex = selectedSlot.transform.GetSiblingIndex();

            Debug.Log("eq " + currentSlotIndex + " inv: " + _selectedInventorySlotIndex + " eq: " + _selectedEquipmentSlotIndex);

            if (_selectedInventorySlotIndex == -1)
            {
                switch (currentSlotIndex)
                {
                    case 0 when headEquipment == null:
                    case 1 when bodyEquipment == null:
                    case 2 when equippedWand == null:
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
            
            Debug.Log("eq end " + currentSlotIndex + " inv: " + _selectedInventorySlotIndex + " eq: " + _selectedEquipmentSlotIndex);
        }

        private void EquipItem(int currentSlotIndex, GameObject selectedSlot)
        {
            var selectedItem = inventorySlots[_selectedInventorySlotIndex];
            if (selectedItem.CompareTag("Armor") && currentSlotIndex < 2) // 0, 1 = head, body
            {
                var selectedItemScript = selectedItem.GetComponent<ArmorScript>();
                if (selectedItemScript.armorType == ArmorType.HeadArmor && currentSlotIndex == 0)
                {
                    headEquipment = selectedItem;
                    _playerScript.EquipHeadArmor(selectedItem);
                    selectedSlot.GetComponent<Image>().sprite = selectedItemScript.armorSprite;
                    _inventoryGrid.GetChild(_selectedInventorySlotIndex).GetComponent<Image>().sprite =
                        _emptySlotSprite;
                    inventorySlots[_selectedInventorySlotIndex] = null;
                }

                if (selectedItemScript.armorType == ArmorType.BodyArmor && currentSlotIndex == 1)
                {
                    bodyEquipment = selectedItem;
                    _playerScript.EquipBodyArmor(selectedItem);
                    selectedSlot.GetComponent<Image>().sprite = selectedItemScript.armorSprite;
                    _inventoryGrid.GetChild(_selectedInventorySlotIndex).GetComponent<Image>().sprite =
                        _emptySlotSprite;
                    inventorySlots[_selectedInventorySlotIndex] = null;
                }
            }

            if (selectedItem.CompareTag("Weapon"))
            {
                var selectedItemScript = selectedItem.GetComponent<WeaponScript>();
                if (currentSlotIndex == 2) // 2 = wand
                {
                    _playerScript.EquipWeapon(selectedItem);
                    selectedSlot.GetComponent<Image>().sprite = selectedItemScript.idleSprite;
                    _inventoryGrid.GetChild(_selectedInventorySlotIndex).GetComponent<Image>().sprite =
                        _emptySlotSprite;
                    inventorySlots[_selectedInventorySlotIndex] = null;
                }
            }
        }

        private void SwapOutEquipment(int currentSlotIndex)
        {
            if (_selectedEquipmentSlotIndex == 0) // head
            {
                GameObject inventoryObject = null;
                if (inventorySlots[currentSlotIndex] != null)
                {
                    if (inventorySlots[currentSlotIndex].GetComponent<ArmorScript>()?.armorType !=
                        ArmorType.HeadArmor) return;
                    inventoryObject = inventorySlots[currentSlotIndex];
                }

                inventorySlots[currentSlotIndex] = headEquipment;
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    inventorySlots[currentSlotIndex].GetComponent<SpriteRenderer>().sprite;
                headEquipment = inventoryObject;
                if (headEquipment != null)
                {
                    _playerScript.EquipHeadArmor(headEquipment);
                    _equipmentGrid.GetChild(0).GetComponent<Image>().sprite =
                        headEquipment.GetComponent<ArmorScript>().armorSprite;
                }
                else _playerScript.UnequipHeadArmor();
            }

            if (_selectedEquipmentSlotIndex == 1) // body
            {
                GameObject inventoryObject = null;
                if (inventorySlots[currentSlotIndex] != null)
                {
                    if (inventorySlots[currentSlotIndex].GetComponent<ArmorScript>()?.armorType !=
                        ArmorType.BodyArmor) return;
                    inventoryObject = inventorySlots[currentSlotIndex];
                }

                inventorySlots[currentSlotIndex] = bodyEquipment;
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    inventorySlots[currentSlotIndex].GetComponent<SpriteRenderer>().sprite;
                bodyEquipment = inventoryObject;
                if (bodyEquipment != null)
                {
                    _playerScript.EquipBodyArmor(bodyEquipment);
                    _equipmentGrid.GetChild(1).GetComponent<Image>().sprite =
                        bodyEquipment.GetComponent<ArmorScript>().armorSprite;
                }
                else _playerScript.UnequipBodyArmor();
            }

            if (_selectedEquipmentSlotIndex == 2) // wand
            {
                if (inventorySlots[currentSlotIndex]?.GetComponent<WeaponScript>() == null) return;
                
                (inventorySlots[currentSlotIndex], equippedWand) = (equippedWand, inventorySlots[currentSlotIndex]);
                _inventoryGrid.GetChild(currentSlotIndex).GetComponent<Image>().sprite =
                    inventorySlots[currentSlotIndex].GetComponent<SpriteRenderer>().sprite;
                _playerScript.EquipWeapon(equippedWand);
                _equipmentGrid.GetChild(2).GetComponent<Image>().sprite =
                    equippedWand.GetComponent<WeaponScript>().idleSprite;
            }
                
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
        }
    }
}