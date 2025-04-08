using System.Collections.Generic;
using Items.Weapons.Scripts;
using UnityEngine;

namespace Characters.Player.Scripts
{
    public class SpellComboController : MonoBehaviour
    {
        [System.Serializable]
        public class ComboEntry
        {
            public List<ElementType> comboElements;
            public GameObject result;
        }

        public List<ComboEntry> comboList;
        
        private Dictionary<SpellCombo, GameObject> _comboDict;

        private void Awake()
        {
            _comboDict = new Dictionary<SpellCombo, GameObject>();
            foreach (var entry in comboList)
            {
                var comboKey = new SpellCombo(entry.comboElements);
                if (!_comboDict.ContainsKey(comboKey))
                    _comboDict.Add(comboKey, entry.result);
            }
        }

        public GameObject GetComboResult(List<ElementType> elements)
        {
            /*
            foreach (var el in elements)
            {
                Debug.Log("combo " + el);
            }
            */
            var key = new SpellCombo(elements);
            return _comboDict.GetValueOrDefault(key);
        }
    }
}
