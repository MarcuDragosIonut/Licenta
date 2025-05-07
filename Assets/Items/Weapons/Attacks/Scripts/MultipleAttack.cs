using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items.Weapons.Attacks.Scripts
{
    public class MultipleAttack : MonoBehaviour
    {
        [Tooltip("X = angle difference, Y = time before it spawns")]
        public List<Vector2> attackParameters;

        public List<GameObject> attackPrefabs;

        private void Start()
        {
            if (attackParameters.Count != attackPrefabs.Count)
                throw new Exception("Attack prefab count does not match parameter count");
        }

        public void Use(bool isPlayerAttack, Vector2 direction, Vector2 spawnPosition, float arcaneMultiplier = 1.0f,
            float fireMultiplier = 1.0f, float waterMultiplier = 1.0f, float physicalMultiplier = 1.0f)
        {
            for (var i = 0; i < attackParameters.Count; i++)
            {
                float angle = attackParameters[i].x, time = attackParameters[i].y;
                float cos = Mathf.Cos(angle * Mathf.Deg2Rad), sin = Mathf.Sin(angle * Mathf.Deg2Rad);
                var newDirection = new Vector2(
                    direction.x * cos - direction.y * sin,
                    direction.x * sin + direction.y * cos);
                if (time > 0)
                {
                    StartCoroutine(DelayAttack(attackPrefabs[i], time, isPlayerAttack, newDirection, spawnPosition,
                        arcaneMultiplier, fireMultiplier, waterMultiplier, physicalMultiplier));
                }
                else
                {
                    attackPrefabs[i].GetComponent<AttackBehaviour>().Use(isPlayerAttack, newDirection, spawnPosition,
                        arcaneMultiplier, fireMultiplier, waterMultiplier, physicalMultiplier);
                }
            }
        }

        private IEnumerator DelayAttack(GameObject attack, float delay, bool isPlayerAttack, Vector2 direction,
            Vector2 spawnPosition,
            float arcaneMultiplier = 1.0f,
            float fireMultiplier = 1.0f, float waterMultiplier = 1.0f, float physicalMultiplier = 1.0f)
        {
            yield return new WaitForSeconds(delay);
            attack.GetComponent<AttackBehaviour>().Use(isPlayerAttack, direction, spawnPosition, arcaneMultiplier,
                fireMultiplier, waterMultiplier, physicalMultiplier);
        }
    }
}