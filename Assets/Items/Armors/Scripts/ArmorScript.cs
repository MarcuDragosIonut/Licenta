using UnityEngine;

namespace Items.Armors.Scripts
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
        public float arcaneReductionMultiplier = 1.0f;
        public float fireReductionMultiplier = 1.0f;
        public float waterReductionMultiplier = 1.0f;
        public float physicalReductionMultiplier = 1.0f;

    }
}
