using System;
using System.Collections;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Textures.Map.Scripts
{
    public class GenerateMap : MonoBehaviour
    {
        public GameObject player;
        public GameObject[] groundPrefabs;
        public GameObject[] obstaclePrefabs;
        public GameObject[] borderPrefabs;
        public GameObject[] portalPrefabs;
        public int mapMinWidth;
        public int mapMaxWidth;
        public int mapLength;
        public int numberOfObstacles;
        public int borderLength;

        private const float NumberOfTilesInPrefab = 6f;
        private GameObject _portal;

        public IEnumerator changeMap(int mapSize)
        {
            if (mapSize is < 1 or > 3)
            {
                throw new Exception("Bad map size");
            }

            Vector2 portalPosition = _portal.transform.position;
            Destroy(_portal);
            _portal = Instantiate(portalPrefabs[1], portalPosition, Quaternion.identity);
            _portal.transform.parent = transform;

            yield return new WaitForSeconds(0.5f);

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            mapMinWidth = mapSize * 4;
            mapMaxWidth = mapSize * 6 + 2;
            mapLength = mapSize * 6 + 4;
            numberOfObstacles = Random.Range(mapSize * 2, mapSize * 4);

            Generate();
        }

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
            var obstacleChanceIncrease =
                (1.0f - obstacleChance) / ((mapLength - 1) * mapMinWidth) * (numberOfObstacles - 1);
            var portalPosition = -100;

            Debug.Log(obstacleChanceIncrease);

            player.transform.position = new Vector2(currentLeftLimit * 2 + 1, 1);
            for (var y = 0; y < mapLength; y++)
            {
                if (y == mapLength - 1)
                {
                    portalPosition = Random.Range(currentLeftLimit, currentRightLimit);
                }

                for (var x = currentLeftLimit; x < currentRightLimit; x++)
                {
                    Vector2 currentPosition = new Vector2(x * 2, y * 2);
                    GameObject tile = Instantiate(groundPrefabs[0], currentPosition, Quaternion.identity);
                    tile.transform.parent = transform;
                    if (x == portalPosition)
                    {
                        _portal = Instantiate(portalPrefabs[0], currentPosition, Quaternion.identity);
                        _portal.transform.parent = transform;
                    }

                    if (y > 0 && currentObstacles < numberOfObstacles && x != portalPosition)
                    {
                        if (Random.Range(0.0f, 1.0f) < obstacleChance)
                        {
                            obstacleChance = 0.02f - Math.Max(0.5f - obstacleChance, 0.0f);
                            Debug.Log(obstacleChance);
                            GameObject obstacle = Instantiate(obstaclePrefabs[0], currentPosition, Quaternion.identity);
                            obstacle.transform.parent = transform;
                            currentObstacles++;
                        }
                        else
                        {
                            obstacleChance += obstacleChanceIncrease;
                        }
                    }

                    Debug.Log(x + " " + y + " " + currentLeftLimit + " " + currentRightLimit + " " + (mapLength - 1));
                    if (x == currentLeftLimit)
                    {
                        if (0 < y && y < mapLength - 1) CreateBorder(x, y, new Vector2(-1, 0));
                        else
                        {
                            if (y == 0)
                                CreateBorder(x, y, new Vector2(-1, -1));
                            if (y == mapLength - 1)
                                CreateBorder(x, y, new Vector2(-1, 1));
                        }
                    }
                    if (x == currentRightLimit - 1)
                    {
                        if (0 < y && y < mapLength - 1) CreateBorder(x, y, new Vector2(1, 0));
                        else
                        {
                            if (y == 0)
                                CreateBorder(x, y, new Vector2(1, -1));
                            if (y == mapLength - 1)
                                CreateBorder(x, y, new Vector2(1, 1));
                        }
                    }
                    if ( x != currentLeftLimit && x != currentRightLimit - 1)
                    {
                        if (y == 0)
                            CreateBorder(x, y, new Vector2(0, -1));
                        if (y == mapLength - 1)
                            CreateBorder(x, y, new Vector2(0, 1));
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