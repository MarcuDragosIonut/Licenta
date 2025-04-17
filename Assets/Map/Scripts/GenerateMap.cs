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
        public int numberOfObstacles;
        public int borderLength;
        
        private int _minRoomSize = 6;
        private int _maxRoomSize = 9;
        private GameObject _portal;

        private const float NoiseScale = 0.8f;
        private const float LowThreshold = 0.45f;
        private const float HighThreshold = 0.6f;

        public IEnumerator ChangeMap()
        {

            Vector2 portalPosition = _portal.transform.position;
            Destroy(_portal);
            _portal = Instantiate(portalPrefabs[1], portalPosition, Quaternion.identity);
            _portal.transform.parent = transform;

            yield return new WaitForSeconds(0.5f);

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            GenerateRoom(0, 0, true);
            AstarPath.active.Scan();
        }

        private void Start()
        {
            GenerateRoom(0 ,0, true);
            AstarPath.active.Scan();
        }

        private void GenerateRoom(int lowX, int lowY, bool isStart = false, bool isEnd = false)
        {
            float xSeed = Random.Range(0.0f, 1000.0f), ySeed = Random.Range(0.0f, 1000.0f);
            var roomLength = Random.Range(_minRoomSize, _maxRoomSize);
            var roomWidth = Random.Range(_minRoomSize, _maxRoomSize);
            for (var y = 0; y < roomLength; y++)
            {
                for (var x = 0; x < roomWidth; x++)
                {
                    if (y == 0 || y == roomLength - 1 || x == 0 || x == roomWidth - 1)
                    {
                        var noise = Mathf.PerlinNoise((x + xSeed) * NoiseScale, (y + ySeed) * NoiseScale);
                        if (noise < LowThreshold) continue;
                    }
                    var currentPosition = new Vector2((lowX + x) * 2, (lowY + y) * 2);
                    var tile = Instantiate(groundPrefabs[0], currentPosition, Quaternion.identity);
                    tile.transform.parent = transform;
                }
                
            }
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