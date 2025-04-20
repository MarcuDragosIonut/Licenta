using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
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

        private readonly int[,] _roomGrid = new int[MaxRoomCountPerDimension, MaxRoomCountPerDimension];

        private readonly int[,] _tileGrid =
            new int[MaxRoomCountPerDimension * (MaxRoomSize + RoomPadding),
                MaxRoomCountPerDimension * (MaxRoomSize + RoomPadding)];

        private Vector2Int _startCoords;
        private Vector2Int _endCoords;
        private int _roomCount;
        private GameObject _portal;

        private const int MinRoomSize = 6;
        private const int MaxRoomSize = 9;
        private const int MaxRoomCountPerDimension = 6;
        private const int RoomPadding = 4;
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
            GenerateCorridors();
            GenerateUnwalkableTiles();
            AstarPath.active.Scan();
        }

        private void GenerateMapLayout()
        {
            _roomCount = Random.Range(8, 14);
            int startX = Random.Range(0, MaxRoomCountPerDimension), startY = Random.Range(0, MaxRoomCountPerDimension);
            Debug.Log("start coords: " + startX + " " + startY + " nr rooms: " + _roomCount);
            _roomGrid[startX, startY] = 1;
            _startCoords = new Vector2Int(startX, startY);
            var roomCandidates = new List<Vector2Int>();
            roomCandidates.AddRange(GetMatrix4Neighbors(_roomGrid, startX, startY, 0));
            var exploredCandidates = new HashSet<Vector2Int>(roomCandidates);
            var endRoomIndex = Random.Range(0, _roomCount);
            for (var roomIndex = 0; roomIndex < _roomCount; roomIndex++)
            {
                var chosenRoomIndex = Random.Range(0, roomCandidates.Count);
                var newRoomCoords = roomCandidates[chosenRoomIndex];

                _roomGrid[newRoomCoords.x, newRoomCoords.y] = 1;
                if (roomIndex == endRoomIndex) _endCoords = new Vector2Int(newRoomCoords.x, newRoomCoords.y);

                roomCandidates[chosenRoomIndex] = roomCandidates[^1];
                roomCandidates.RemoveAt(roomCandidates.Count - 1);
                var roomNeighbors = GetMatrix4Neighbors(_roomGrid, newRoomCoords.x, newRoomCoords.y, 0);
                roomCandidates.AddRange(roomNeighbors.Where(coords => !exploredCandidates.Contains(coords)));
                exploredCandidates.UnionWith(roomNeighbors);
            }

            for (int i = _roomGrid.GetLength(1) - 1; i >= 0; i--)
            {
                var rez = "";
                for (int j = 0; j < _roomGrid.GetLength(0); j++)
                {
                    rez += _roomGrid[j, i] + " ";
                }

                Debug.Log(rez);
            }
        }

        private void GenerateRooms()
        {
            for (var i = 0; i < MaxRoomCountPerDimension; i++)
            {
                for (var j = 0; j < MaxRoomCountPerDimension; j++)
                {
                    if (_roomGrid[i, j] > 0)
                        GenerateRoom(i * (MaxRoomSize + RoomPadding), j * (MaxRoomSize + RoomPadding),
                            _roomGrid[i, j] == 1, _roomGrid[i, j] == 3);
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

                    var currentPosition = new Vector2(lowX + widthOffset + x, lowY + lengthOffset + y);
                    _tileGrid[(int)currentPosition.x, (int)currentPosition.y] = 1;
                    var tile = Instantiate(groundPrefabs[Random.Range(0, groundPrefabs.Length)], currentPosition * 2,
                        Quaternion.identity);
                    tile.transform.parent = transform;
                }
            }
        }

        private void GenerateCorridors()
        {
            var unusedPaths = new HashSet<Tuple<Vector2Int, Vector2Int>>();
            var traversedRooms = new HashSet<Vector2Int>();
            var currentCoords = _startCoords;
            var corridorCandidates = new List<Tuple<Vector2Int, Vector2Int>>();
            while (true)
            {
                traversedRooms.Add(currentCoords);
                foreach (var neighbor in GetMatrix4Neighbors(_roomGrid, currentCoords.x, currentCoords.y, 1))
                {
                    if (!traversedRooms.Contains(neighbor))
                    {
                        corridorCandidates.Add(new Tuple<Vector2Int, Vector2Int>(currentCoords, neighbor));
                    }
                }

                Tuple<Vector2Int, Vector2Int> chosenPath;
                while (true)
                {
                    var chosenIndex = Random.Range(0, corridorCandidates.Count);
                    chosenPath = corridorCandidates[chosenIndex];
                    corridorCandidates[chosenIndex] = corridorCandidates[^1];
                    corridorCandidates.RemoveAt(corridorCandidates.Count - 1);
                    if (!traversedRooms.Contains(chosenPath.Item2)) break;
                    unusedPaths.Add(chosenPath);
                }

                GenerateCorridor(chosenPath);
                // Debug.Log("used room: " + currentCoords + " used path: " + chosenPath);
                currentCoords = chosenPath.Item2; // destination becomes new current node
                if (traversedRooms.Count == _roomCount) break;
            }
        }

        private void GenerateCorridor(Tuple<Vector2Int, Vector2Int> path)
        {
            const int minOffset = (MaxRoomSize - MinRoomSize) / 2;
            var firstRoomCorridorPos = Random.Range(minOffset, minOffset + MinRoomSize);
            var secondRoomCorridorPos = Random.Range(minOffset, minOffset + MinRoomSize);
            var firstRoomPos = path.Item1;
            var secondRoomPos = path.Item2;
            var direction = new Vector2Int(
                secondRoomPos.x -
                firstRoomPos.x, // rooms are always adjacent, result will be the direction the corridor moves in
                secondRoomPos.y - firstRoomPos.y);
            var currentPos = new Vector2(
                firstRoomPos.x * (MaxRoomSize + RoomPadding) +
                (direction.x == 1 ? MinRoomSize : 0) +
                (direction.x == -1 ? MaxRoomSize - MinRoomSize : 0) +
                (direction.y != 0 ? firstRoomCorridorPos : 0),
                firstRoomPos.y * (MaxRoomSize + RoomPadding) +
                (direction.y == 1 ? MinRoomSize : 0) +
                (direction.y == -1 ? MaxRoomSize - MinRoomSize : 0) +
                (direction.x != 0 ? firstRoomCorridorPos : 0)
            );
            int counter = 0, directionTileLimit = MaxRoomSize + RoomPadding / 2 - MinRoomSize;
            while (true)
            {
                if (_tileGrid[(int)currentPos.x, (int)currentPos.y] == 0)
                    Instantiate(groundPrefabs[Random.Range(0, groundPrefabs.Length)], currentPos * 2,
                        Quaternion.identity).transform.parent = transform;
                _tileGrid[(int)currentPos.x, (int)currentPos.y] = 1;
                counter++;
                if (counter == directionTileLimit * 2 + 1) break;
                if (counter == directionTileLimit)
                {
                    var corridorDiff = secondRoomCorridorPos - firstRoomCorridorPos;
                    var bridgeDirection = new Vector2Int(
                        Math.Sign(corridorDiff) * Math.Abs(direction.y),
                        Math.Sign(corridorDiff) * Math.Abs(direction.x));
                    for (var i = 0; i < Math.Abs(corridorDiff); i++)
                    {
                        currentPos += bridgeDirection;
                        var tile = Instantiate(groundPrefabs[Random.Range(0, groundPrefabs.Length)], currentPos * 2,
                            Quaternion.identity);
                        tile.transform.parent = transform;
                        //tile.GetComponent<Tilemap>().color = Color.blue;
                        _tileGrid[(int)currentPos.x, (int)currentPos.y] = 1;
                    }
                }

                currentPos += direction;
            }
        }

        private void GenerateUnwalkableTiles()
        {
            var tileGridLength = _tileGrid.GetLength(0);
            for (var x = 0; x < tileGridLength; x++)
            {
                for (var y = 0; y < tileGridLength; y++)
                {
                    int roomGridX = x / (MaxRoomSize + RoomPadding), roomGridY = y / (MaxRoomSize + RoomPadding);
                    // if a room exists on this tile or in its vicinity create border
                    if ((_roomGrid[roomGridX, roomGridY] != 0 || (_roomGrid[roomGridX, roomGridY] == 0 &&
                                                                  GetMatrix8Neighbors(_roomGrid, roomGridX, roomGridY,
                                                                      1).Count > 0)) &&
                        (x == 0 || y == 0 || x == tileGridLength - 1 || y == tileGridLength - 1))
                    {
                        CreateBorder(x, y,
                            new Vector2(x == 0 ? -1 : x == tileGridLength - 1 ? 1 : 0,
                                y == 0 ? -1 : y == tileGridLength - 1 ? 1 : 0));
                    }

                    // if a room exists on this tile or in its vicinity fill up unused tiles with unwalkable prefabs
                    if (_roomGrid[roomGridX, roomGridY] == 0 &&
                        GetMatrix8Neighbors(_roomGrid, roomGridX, roomGridY, 1).Count == 0) continue;

                    if (_tileGrid[x, y] == 0)
                    {
                        InstantiateBorder(x * 2, y * 2, GetMatrix4Neighbors(_tileGrid, x, y, 1).Count > 0 ? 1 : 0);
                    }
                }
            }
        }

        private List<Vector2Int> GetMatrix4Neighbors(int[,] grid, int x, int y, int value)
        {
            var result = new List<Vector2Int>();

            if (x > 0 && grid[x - 1, y] == value) result.Add(new Vector2Int(x - 1, y));
            if (x < grid.GetLength(0) - 1 && grid[x + 1, y] == value) result.Add(new Vector2Int(x + 1, y));
            if (y > 0 && grid[x, y - 1] == value) result.Add(new Vector2Int(x, y - 1));
            if (y < grid.GetLength(1) - 1 && grid[x, y + 1] == value) result.Add(new Vector2Int(x, y + 1));

            return result;
        }

        private List<Vector2Int> GetMatrix8Neighbors(int[,] grid, int x, int y, int value)
        {
            var result = GetMatrix4Neighbors(grid, x, y, value);

            if (x > 0 && y > 0 && grid[x - 1, y - 1] == value) result.Add(new Vector2Int(x - 1, y - 1));
            if (x < grid.GetLength(0) - 1 && y > 0 && grid[x + 1, y - 1] == value)
                result.Add(new Vector2Int(x + 1, y - 1));
            if (x > 0 && y < grid.GetLength(1) - 1 && grid[x - 1, y + 1] == value)
                result.Add(new Vector2Int(x - 1, y + 1));
            if (x < grid.GetLength(0) - 1 && y < grid.GetLength(1) - 1 && grid[x + 1, y + 1] == value)
                result.Add(new Vector2Int(x + 1, y + 1));

            return result;
        }

        private void CreateBorder(int x, int y, Vector2 dir)
        {
            Debug.Log("CREATE BORDER: " + x + " " + y + " " + dir);
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