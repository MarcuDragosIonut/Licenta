using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Textures.Map.Scripts
{
    public class GenerateMap : MonoBehaviour
    {
        public GameObject player;
        public GameObject[] groundPrefabs;
        public GameObject[] obstaclePrefabs;
        public GameObject[] borderPrefabs;
        public int mapMinWidth;
        public int mapMaxWidth;
        public int mapLength;
        public int numberOfObstacles;
        public int borderLength;

        private const float NumberOfTilesInPrefab = 6f;

        private void Start()
        {
            Generate();
        }

        private void Generate()
        {
            var currentWidth = Random.Range(mapMinWidth, mapMaxWidth);
            var currentLeftLimit = mapMaxWidth / 2 - currentWidth / 2;
            var currentRightLimit = currentLeftLimit + currentWidth;
            var currentObstacles = 0;
            var obstacleChance = 0.02f;
            var obstacleChanceIncrease = (1.0f - obstacleChance) / (mapLength * mapMinWidth) * (numberOfObstacles - 1);

            player.transform.position = new Vector2(currentLeftLimit * 2 + 1, 1);
            for (var y = 0; y < mapLength; y++)
            {
                for (var x = currentLeftLimit; x < currentRightLimit; x++)
                {
                    Vector2 currentPosition = new Vector2(x * 2, y * 2);
                    GameObject tile = Instantiate(groundPrefabs[0], currentPosition, Quaternion.identity);
                    tile.transform.parent = transform;
                    if (y > 0 && currentObstacles < numberOfObstacles)
                    {
                        if (Random.Range(0.0f, 1.0f) < obstacleChance)
                        {
                            obstacleChance = 0.02f - Math.Max(0.5f - obstacleChance, 0.0f);
                            GameObject obstacle = Instantiate(obstaclePrefabs[0], currentPosition, Quaternion.identity);
                            obstacle.transform.parent = transform;
                            currentObstacles++;
                        }
                        else
                        {
                            obstacleChance += obstacleChanceIncrease;
                        }
                    }

                    if (y == 0 || y == mapLength - 1 || x == currentLeftLimit || x == currentRightLimit - 1)
                    {
                        Vector2 borderDir = new Vector2(
                            x == currentLeftLimit || x == currentRightLimit - 1
                                ? Math.Sign(x - currentLeftLimit - 1)
                                : 0,
                            y == 0 || y == mapLength - 1 ? 1 * Math.Sign(y - 1) : 0);
                        CreateBorder(x, y, borderDir);
                    }
                }

                currentLeftLimit = NewMapLimit(currentLeftLimit, currentRightLimit);
                currentRightLimit = NewMapLimit(currentRightLimit, currentLeftLimit);
            }
        }

        private int NewMapLimit(int currentLimit, int otherLimit)
        {
            var extendChancePull = Random.Range(0.0f, 1.0f);
            if (currentLimit == 0)
            {
                if (extendChancePull < 0.2f && otherLimit + 2 < mapMaxWidth)
                    currentLimit++;
            }
            else
            {
                if (extendChancePull < 0.2f)
                {
                    if (extendChancePull < 0.1f && otherLimit - currentLimit + 2 < mapMaxWidth)
                        currentLimit++;
                    else if (otherLimit - currentLimit > mapMinWidth) currentLimit--;
                }
            }

            return currentLimit;
        }

        private void CreateBorder(int x, int y, Vector2 dir)
        {
            if (dir.y != 0)
            {
                for (var borderObjectIndex = 1; borderObjectIndex <= borderLength; borderObjectIndex++)
                {
                    InstantiateBorder(x * 2, 2 * (y + borderObjectIndex * dir.y), borderObjectIndex);
                    if (dir.x != 0)
                    {
                        for (var cornerObjectIndex = 1; cornerObjectIndex <= borderLength; cornerObjectIndex++)
                        {
                            InstantiateBorder(2 * (x + cornerObjectIndex * dir.x), 2 * (y + borderObjectIndex * dir.y),
                                cornerObjectIndex);
                        }
                    }
                }
            }

            if (dir.x != 0)
            {
                for (var borderObjectIndex = 1; borderObjectIndex <= borderLength; borderObjectIndex++)
                {
                    InstantiateBorder(2 * (x + borderObjectIndex * dir.x), y * 2, borderObjectIndex);
                }
            }
        }

        private void InstantiateBorder(float x, float y, int objectIndex)
        {
            var borderPrefabIndex = objectIndex == 1 ? 0 : Random.Range(0, borderPrefabs.Length);
            GameObject borderObject = Instantiate(borderPrefabs[borderPrefabIndex],
                new Vector2(x, y), Quaternion.identity);
            borderObject.transform.parent = transform;
        }
    }
}