using Characters.Player.Items.Armors.Scripts;
using Characters.Player.Weapons.Scripts;
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
        
        private PlayerController _playerScript;
        private Transform _inventoryGrid;
        private Transform _equipmentGrid;
        private int _selectedInventorySlotIndex = -1;
        private int _selectedEquipmentSlotIndex = -1;
    
        void Start()
        {
            _inventoryGrid = transform.Find("InventoryGrid");
            _equipmentGrid = transform.Find("EquipmentGrid");
            _playerScript = player.GetComponent<PlayerController>();
            inventorySlots = new GameObject[_inventoryGrid.childCount];
            if (headEquipment != null)
            {
                _equipmentGrid.transform.GetChild(0).GetComponent<Image>().sprite = headEquipment.GetComponent<SpriteRenderer>().sprite;
            }
            if (bodyEquipment != null)
            {
                _equipmentGrid.transform.GetChild(1).GetComponent<Image>().sprite = bodyEquipment.GetComponent<SpriteRenderer>().sprite;
            }
            if (equippedWand != null)
            {
                _equipmentGrid.transform.GetChild(2).GetComponent<Image>().sprite = equippedWand.GetComponent<WeaponScript>().idleSprite;
            }
        }

        public void OnInventorySlotClick()
        {
            var selectedSlot = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            var currentSlotIndex = selectedSlot.transform.GetSiblingIndex();
        
            Debug.Log("inv " + currentSlotIndex);
        
            if (_selectedInventorySlotIndex != currentSlotIndex)
            {
                _selectedInventorySlotIndex = currentSlotIndex;
                if (inventorySlots[_selectedInventorySlotIndex] != null)
                {
                    _inventoryGrid.transform.GetChild(_selectedInventorySlotIndex).GetComponent<Outline>().effectColor =
                        Color.yellow;
                }
                else
                {
                    _selectedInventorySlotIndex = -1;
                }
            }
            else
            {
                _selectedInventorySlotIndex = -1;
            }

        }

        public void OnEquipmentSlotClick()
        {
        
            var selectedSlot = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            var currentSlotIndex = selectedSlot.transform.GetSiblingIndex();
        
            Debug.Log("eq " + currentSlotIndex);
            
            if (_selectedInventorySlotIndex == -1)
            {
                _selectedEquipmentSlotIndex = currentSlotIndex;
            }
            else
            {
                var selectedItem = inventorySlots[_selectedInventorySlotIndex];
                if (selectedItem.CompareTag("Armor") && currentSlotIndex < 2) // 0, 1 = head, body
                {
                    var selectedItemScript = selectedItem.GetComponent<ArmorScript>();
                    if (selectedItemScript.armorType == ArmorType.HeadArmor && currentSlotIndex == 0)
                    {
                        _playerScript.EquipHeadArmor(selectedItem);
                        selectedSlot.GetComponent<Image>().sprite = selectedItemScript.armorSprite;
                    }

                    if (selectedItemScript.armorType == ArmorType.BodyArmor && currentSlotIndex == 1)
                    {
                        _playerScript.EquipBodyArmor(selectedItem);
                        selectedSlot.GetComponent<Image>().sprite = selectedItemScript.armorSprite;
                    }
                }

                if (selectedItem.CompareTag("Weapon"))
                {
                    var selectedItemScript = selectedItem.GetComponent<WeaponScript>();
                    if (currentSlotIndex == 2) // 2 = wand
                    {
                        _playerScript.EquipWeapon(selectedItem);
                        selectedSlot.GetComponent<Image>().sprite = selectedItemScript.idleSprite;
                    }
                }
            }
        }
    
    }
}
