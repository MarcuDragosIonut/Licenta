using System;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Map.Scripts
{
    public class ChestScript : MonoBehaviour
    {
        public Sprite openedSprite;
        public GameObject[] lootTable;
        public float[] chances;
        public bool isOpened = false; 
        
        private void Awake()
        {
            if (lootTable.Length != chances.Length)
                throw new Exception("lootTable and chances must have the same amount of elements");
        
            for (var i = 0; i < chances.Length; i++)
            {
                if (chances[i] < 0.0f) throw new Exception("Chances can't be negative");
                if (i > 0) chances[i] += chances[i - 1];
            }

            if (!Mathf.Approximately(chances[^1], 1.0f)) throw new Exception("The sum of loot table chances must equal 1");
        }

        public GameObject GetLoot()
        {
            var lootDistributionVal = Random.Range(0.0f, 1.0f);
            if (lootDistributionVal == 0.0f) return lootTable[0];
            var lootIndex = 0;
            for (; lootIndex < chances.Length; lootIndex++)
            {
                var lowerBound = lootIndex == 0 ? 0.0f : chances[lootIndex - 1];
                if (lowerBound < lootDistributionVal && lootDistributionVal <= chances[lootIndex]) break;
            }
            
            gameObject.GetComponent<SpriteRenderer>().sprite = openedSprite;
            
            isOpened = true;

            gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
            return lootTable[lootIndex];
        }
    }
}