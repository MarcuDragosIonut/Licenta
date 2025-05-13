using UnityEngine;

namespace Items.Armor.Scripts
{
    public class ArmorScript : MonoBehaviour
    {
        public Sprite armorSprite;
        public Sprite equippedArmorSprite;
        public ArmorType armorType;
        public float arcaneMultiplier = 1.0f;
        public float fireMultiplier = 1.0f;
        public float waterMultiplier = 1.0f;
        public float physicalMultiplier = 1.0f;
        public float arcaneReductionMultiplier = 0f;
        public float fireReductionMultiplier = 0f;
        public float waterReductionMultiplier = 0f;
        public float physicalReductionMultiplier = 0f;

    }
}
