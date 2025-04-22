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

        private readonly List<List<Vector2Int>> _freeTilesInRoom = new List<List<Vector2Int>>();
        private Vector2Int _startCoords;
        private Vector2Int _endCoords;
        private int _roomCount;
        private GameObject _portal;

        private const int MinRoomSize = 6;
        private const int MaxRoomSize = 9;
        private const int MaxRoomCountPerDimension = 6;
        private const int RoomPadding = 4;
        private const float NoiseScale = 0.8f;
        private const float RoomShapeThreshold = 0.45f;
        private const float ObstacleThreshold = 0.6f;
        private const float ObstaclePerlinScale = 2.5f;

        public IEnumerator ChangeMap()
        {
            for (var x = 0; x < MaxRoomCountPerDimension; x++)
            {
                for (var y = 0; y < MaxRoomCountPerDimension; y++)
                {
                    _freeTilesInRoom[x + y * MaxRoomCountPerDimension].Clear();
                }
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

            CreateMap();
            AstarPath.active.Scan();
        }

        private void Start()
        {
            for (var x = 0; x < MaxRoomCountPerDimension; x++)
            {
                for (var y = 0; y < MaxRoomCountPerDimension; y++)
                {
                    _freeTilesInRoom.Add(new List<Vector2Int>());
                }
            }

            CreateMap();
            AstarPath.active.Scan();
        }

        private void CreateMap()
        {
            GenerateMapLayout();
            GenerateRooms();
            GenerateCorridors();
            GenerateUnwalkableTiles();
            GenerateObstacles();
            GenerateMapSpawns();
        }

        private void GenerateMapLayout()
        {
            _roomCount = Random.Range(8, 14);
            int startX = Random.Range(0, MaxRoomCountPerDimension), startY = Random.Range(0, MaxRoomCountPerDimension);
            // Debug.Log("start coords: " + startX + " " + startY + " nr rooms: " + _roomCount);
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

            /*
            for (int i = _roomGrid.GetLength(1) - 1; i >= 0; i--)
            {
                var rez = "";
                for (int j = 0; j < _roomGrid.GetLength(0); j++)
                {
                    rez += _roomGrid[j, i] + " ";
                }

                Debug.Log(rez);
            }
            */
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
                        if (noise < RoomShapeThreshold) continue;
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
                _tileGrid[(int)currentPos.x, (int)currentPos.y] = 2;
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

                    if (_tileGrid[x, y] == 1 && GetMatrix4Neighbors(_tileGrid, x, y, 0).Count == 4) _tileGrid[x, y] = 0;
                    if (_tileGrid[x, y] == 0)
                    {
                        InstantiateBorder(x * 2, y * 2, GetMatrix4Neighbors(_tileGrid, x, y, 1).Count > 0 ? 1 : 0);
                    }
                }
            }
        }

        private void CreateBorder(int x, int y, Vector2 dir)
        {
            // Debug.Log("CREATE BORDER: " + x + " " + y + " " + dir);
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

        private void GenerateObstacles()
        {
            float seedX = Random.Range(0, 1000.0f), seedY = Random.Range(0, 1000.0f);
            for (var y = 0; y < MaxRoomCountPerDimension; y++)
            {
                for (var x = 0; x < MaxRoomCountPerDimension; x++)
                {
                    if (_roomGrid[x, y] != 1) continue;

                    for (var roomY = 0; roomY < MaxRoomSize; roomY++)
                    {
                        var tileY = y * (MaxRoomSize + RoomPadding) + roomY;
                        for (var roomX = 0; roomX < MaxRoomSize; roomX++)
                        {
                            var tileX = x * (MaxRoomSize + RoomPadding) + roomX;
                            var chance = Mathf.PerlinNoise(
                                seedX + (float)roomX / MaxRoomSize * ObstaclePerlinScale,
                                seedY + (float)roomY / MaxRoomSize * ObstaclePerlinScale);

                            // Debug.Log(roomX + " " + roomY + " details: " + _tileGrid[tileX, tileY] + " " + chance + " " + GetMatrix8Neighbors(_tileGrid, tileX, tileY, 0).Count);
                            if (_tileGrid[tileX, tileY] == 1 && chance > ObstacleThreshold &&
                                GetMatrix8Neighbors(_tileGrid, tileX, tileY, 0).Count < 2)
                            {
                                Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)],
                                        new Vector2(tileX * 2, tileY * 2), Quaternion.identity).transform.parent =
                                    transform;
                                foreach (var neighbor in GetMatrix8Neighbors(_tileGrid, tileX, tileY, 1))
                                {
                                    _tileGrid[neighbor.x, neighbor.y] = 2;
                                }

                                _tileGrid[tileX, tileY] = 3; // 3 - actual place of the object, 2 - vicinity
                            }

                            if (_tileGrid[tileX, tileY] == 1 || _tileGrid[tileX, tileY] == 2)
                                _freeTilesInRoom[x + y * MaxRoomCountPerDimension].Add(new Vector2Int(tileX, tileY));
                        }
                    }
                }
            }
        }

        private void GenerateMapSpawns()
        {
            Debug.Log(_startCoords + " " + _freeTilesInRoom.Count);
            /*
            for (var i = 0; i < 6; i++)
            {
                string output = "";
                for (var j = 0; j < 6; j++)
                {
                    output += _freeTilesInRoom[i + j * 6].Count + " ";
                }
                Debug.Log(output);
            }
            */
            for (var y = 0; y < MaxRoomCountPerDimension; y++)
            {
                for (var x = 0; x < MaxRoomCountPerDimension; x++)
                {
                    // spawn player
                    if (_startCoords.x == x && _startCoords.y == y)
                    {
                        var roomIndex = x + y * MaxRoomCountPerDimension;
                        var tileIndex = Random.Range(0, _freeTilesInRoom[roomIndex].Count);
                        Debug.Log(roomIndex + " " + tileIndex + " | " + _freeTilesInRoom[roomIndex].Count());
                        var playerSpawnTile = _freeTilesInRoom[roomIndex][tileIndex];
                        _freeTilesInRoom[roomIndex][tileIndex] = _freeTilesInRoom[roomIndex][^1];
                        _freeTilesInRoom[roomIndex].RemoveAt(_freeTilesInRoom[roomIndex].Count - 1);

                        player.transform.position = new Vector2(playerSpawnTile.x * 2 + 0.5f, playerSpawnTile.y * 2);
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
    }
}