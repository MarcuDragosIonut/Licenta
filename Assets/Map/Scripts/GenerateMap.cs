using System;
using System.Collections;
using System.Collections.Generic;
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

        private int[,] _roomGrid = new int[MaxRoomCountPerDimension, MaxRoomCountPerDimension];
        private int[,] _tileGrid = new int[MaxRoomCountPerDimension * MaxRoomSize, MaxRoomCountPerDimension * MaxRoomSize];
        private GameObject _portal;

        private const int MinRoomSize = 6;
        private const int MaxRoomSize = 9;
        private const int MaxRoomCountPerDimension = 6;
        private const int RoomPadding = 1;
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
            GenerateMapLayout();
            GenerateRooms();
            AstarPath.active.Scan();
        }

        private void GenerateMapLayout()
        {
            var numberOfRooms = Random.Range(8, 14);
            int startX = Random.Range(0, MaxRoomCountPerDimension), startY = Random.Range(0, MaxRoomCountPerDimension);
            Debug.Log("startcoords: " + startX + " " + startY);
            _roomGrid[startX, startY] = 1; // start room
            var roomCandidates = new List<Vector2Int>();
            roomCandidates.AddRange(GetGridNeighbors(_roomGrid, startX, startY, 0));
            var endRoomIndex = Random.Range(0, numberOfRooms) + 1;
            while (numberOfRooms-- > 0)
            {
                Debug.Log(numberOfRooms + " " + endRoomIndex);

                var chosenRoomIndex = Random.Range(0, roomCandidates.Count);
                var newRoomCoords = roomCandidates[chosenRoomIndex];

                // 2 - normal room, 3 - end room
                _roomGrid[newRoomCoords.x, newRoomCoords.y] = numberOfRooms == endRoomIndex ? 3 : 2;

                roomCandidates[chosenRoomIndex] = roomCandidates[^1];
                roomCandidates.RemoveAt(roomCandidates.Count - 1);
                roomCandidates.AddRange(GetGridNeighbors(_roomGrid, newRoomCoords.x, newRoomCoords.y, 0));
            }
        }

        private List<Vector2Int> GetGridNeighbors(int[,] grid, int x, int y, int value)
        {
            var result = new List<Vector2Int>();
            if (x > 0 && grid[x - 1, y] == value)
            {
                result.Add(new Vector2Int(x - 1, y));
                grid[x - 1, y] = -1;
            }

            if (x < grid.GetLength(0) - 1 && grid[x + 1, y] == value)
            {
                result.Add(new Vector2Int(x + 1, y));
                grid[x + 1, y] = -1;
            }

            if (y > 0 && grid[x, y - 1] == value)
            {
                result.Add(new Vector2Int(x, y - 1));
                grid[x, y - 1] = -1;
            }

            if (y < grid.GetLength(1) - 1 && grid[x, y + 1] == value)
            {
                result.Add(new Vector2Int(x, y + 1));
                grid[x, y + 1] = -1;
            }

            return result;
        }

        private void GenerateRooms()
        {
            for (var i = 0; i < MaxRoomCountPerDimension; i++)
            {
                for (var j = 0; j < MaxRoomCountPerDimension; j++)
                {
                    if (_roomGrid[i, j] > 0) GenerateRoom(i * (MaxRoomSize + RoomPadding), j * (MaxRoomSize + RoomPadding), _roomGrid[i, j] == 1, _roomGrid[i, j] == 3);
                }
            }
        }

        private void GenerateRoom(int lowX, int lowY, bool isStart = false, bool isEnd = false)
        {
            float xSeed = Random.Range(0.0f, 1000.0f), ySeed = Random.Range(0.0f, 1000.0f);
            var roomLength = Random.Range(MinRoomSize, MaxRoomSize + 1);
            var lengthOffset = (MaxRoomSize - roomLength) / 2;
            var roomWidth = Random.Range(MinRoomSize, MaxRoomSize + 1);
            var widthOffset = (MaxRoomSize - roomWidth) / 2;
            for (var y = 0; y < roomLength; y++)
            {
                for (var x = 0; x < roomWidth; x++)
                {
                    if (y == 0 || y == roomLength - 1 || x == 0 || x == roomWidth - 1)
                    {
                        var noise = Mathf.PerlinNoise((x + xSeed) * NoiseScale, (y + ySeed) * NoiseScale);
                        if (noise < LowThreshold) continue;
                    }

                    var currentPosition = new Vector2((lowX + widthOffset + x) * 2, (lowY + lengthOffset + y) * 2);
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