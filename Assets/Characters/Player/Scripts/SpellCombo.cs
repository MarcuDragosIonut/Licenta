using System;
using System.Collections.Generic;
using Items.Weapons.Scripts;
using UnityEngine;

namespace Characters.Player.Scripts
{
    [Serializable]
    public class SpellCombo : IEquatable<SpellCombo>
    {
        public readonly List<ElementType> comboElements;

        public SpellCombo(List<ElementType> comboElements)
        {
            this.comboElements = comboElements;
        }

        public bool Equals(SpellCombo other)
        {
            if (other == null || other.comboElements.Count != comboElements.Count) return false;

            for (var i = 0; i < comboElements.Count; i++)
            {
                if (comboElements[i] != other.comboElements[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var element in comboElements)
                    hash = hash * 23 + element.GetHashCode();
                return hash;
            }
        }
        
        public override bool Equals(object obj) => Equals(obj as SpellCombo);

    }
}
