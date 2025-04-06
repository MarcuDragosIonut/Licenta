using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Scripts
{
    public class EquipmentSlotsController : MonoBehaviour
    {
        public GameObject[] slots;
        public Sprite emptySlotImage;
        public Sprite normalBorder;
        public Sprite highlightedBorder;
        
        private readonly bool[] _highlightedSlot = { false, false, false };
        private readonly bool[] _occupiedSlot = { false, false, false };
        public void ChangeSlotIcon(int slotIndex, Sprite newIcon)
        {
            Debug.Log(slots[slotIndex].GetComponentInChildren<Image>());
            slots[slotIndex].transform.GetChild(0).GetComponent<Image>().sprite = newIcon == null ? emptySlotImage : newIcon;
            _occupiedSlot[slotIndex] = newIcon != null;
        }

        public void HighlightSlot(InputAction.CallbackContext context)
        {
            var slotIndex = int.Parse(context.control.name) - 1;
            if (!_occupiedSlot[slotIndex] || _highlightedSlot[slotIndex]) return;
            
            slots[slotIndex].GetComponent<Image>().sprite = highlightedBorder;
            _highlightedSlot[slotIndex] = true;
        }

        public void UnhighlightSlots()
        {
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i].GetComponent<Image>().sprite = normalBorder;
                _highlightedSlot[i] = false;
            }
        }
    }
}
