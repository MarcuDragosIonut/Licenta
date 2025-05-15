using System;
using System.Collections.Generic;
using Items.Weapons.Scripts;
using UnityEngine;

namespace Characters.Player.Scripts
{
    [Serializable]
    public class SpellCombo : IEquatable<SpellCombo>
    {
        public readonly List<ElementType> ComboElements;

        public SpellCombo(List<ElementType> comboElements)
        {
            this.ComboElements = comboElements;
        }

        public bool Equals(SpellCombo other)
        {
            if (other == null || other.ComboElements.Count != ComboElements.Count) return false;

            for (var i = 0; i < ComboElements.Count; i++)
            {
                if (ComboElements[i] != other.ComboElements[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var element in ComboElements)
                    hash = hash * 23 + element.GetHashCode();
                return hash;
            }
        }
        
        public override bool Equals(object obj) => Equals(obj as SpellCombo);

    }
}
