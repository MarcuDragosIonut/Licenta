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
        public GameObject[] chestPrefabs;
        public GameObject[] enemyPrefabs;
        public GameObject[] bossPrefabs;
        public int borderLength;

        private int[,] _roomGrid = new int[MaxRoomsPerDimension, MaxRoomsPerDimension];
        private readonly Vector2Int[][] _roomSizes = new Vector2Int[MaxRoomsPerDimension][];

        private int[,] _tileGrid =
            new int[MaxRoomsPerDimension * (MaxRoomSize + RoomPadding),
                MaxRoomsPerDimension * (MaxRoomSize + RoomPadding)];

        private readonly List<List<Vector2Int>> _freeTilesInRoom = new();
        private Vector2Int _startCoords;
        private Vector2Int _endCoords;
        private int _roomCount;
        private GameObject _portal;
        private int _enemiesRemaining;
        private int _totalEnemyCount;
        private bool _isBossAlive;

        private const int MinRoomSize = 6;
        private const int MaxRoomSize = 9;
        private const int MaxRoomsPerDimension = 6;
        private const int RoomPadding = 4;
        private const float NoiseScale = 0.8f;
        private const float RoomShapeThreshold = 0.45f;
        private const float ObstacleThreshold = 0.6f;
        private const float ObstaclePerlinScale = 2.5f;

        private static int _mapIndex = 1;

        public IEnumerator ChangeMap()
        {
            Vector2 portalPosition = _portal.transform.position;
            Destroy(_portal);
            _portal = Instantiate(portalPrefabs[1], portalPosition, Quaternion.identity);
            _portal.transform.parent = transform;

            yield return new WaitForSeconds(0.5f);

            for (var x = 0; x < MaxRoomsPerDimension; x++)
            {
                for (var y = 0; y < MaxRoomsPerDimension; y++)
                {
                    _freeTilesInRoom[x + y * MaxRoomsPerDimension].Clear();
                }
            }

            _roomGrid = new int[MaxRoomsPerDimension, MaxRoomsPerDimension];
            _tileGrid = new int[MaxRoomsPerDimension * (MaxRoomSize + RoomPadding),
                MaxRoomsPerDimension * (MaxRoomSize + RoomPadding)];

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            _mapIndex++;
            CreateMap();
            AstarPath.active.Scan();
        }

        public bool CanTeleportToNextMap()
        {
            return !_isBossAlive && _enemiesRemaining <= _totalEnemyCount / 3;
        }

        public void DecrementEnemyCount()
        {
            _enemiesRemaining--;
        }

        public List<Vector2Int> GetFreeTilesInRoom(int x, int y)
        {
            int roomX = x / (2 * (MaxRoomSize + RoomPadding)), roomY = y / (2 * (MaxRoomSize + RoomPadding));
            var roomIndex = roomX + roomY * MaxRoomsPerDimension;
            return _freeTilesInRoom[roomIndex];
        }

        private void Start()
        {
            for (var x = 0; x < MaxRoomsPerDimension; x++)
            {
                _roomSizes[x] = new Vector2Int[MaxRoomsPerDimension];
                for (var y = 0; y < MaxRoomsPerDimension; y++)
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
            GenerateEntities();
        }

        private void GenerateMapLayout()
        {
            // select starting room
            _roomCount = Random.Range(8, 14);
            int startX = Random.Range(0, MaxRoomsPerDimension), startY = Random.Range(0, MaxRoomsPerDimension);
            _roomGrid[startX, startY] = 1;
            _startCoords = new Vector2Int(startX, startY);

            var roomCandidates = new List<Vector2Int>(); // which rooms could be generated
            roomCandidates.AddRange(GetMatrix4Neighbors(_roomGrid, startX, startY, 0));
            var exploredCandidates = new HashSet<Vector2Int>(roomCandidates); // rooms that were generated
            var endRoomIndex = Random.Range(1, _roomCount);
            for (var roomIndex = 1; roomIndex < _roomCount; roomIndex++)
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
        }

        private void GenerateRooms()
        {
            for (var i = 0; i < MaxRoomsPerDimension; i++)
            {
                for (var j = 0; j < MaxRoomsPerDimension; j++)
                {
                    if (_roomGrid[i, j] > 0)
                        GenerateRoom(i * (MaxRoomSize + RoomPadding), j * (MaxRoomSize + RoomPadding));
                }
            }
        }

        private void GenerateRoom(int lowX, int lowY)
        {
            float xSeed = Random.Range(0.0f, 1000.0f), ySeed = Random.Range(0.0f, 1000.0f);
            var roomLength = Random.Range(MinRoomSize, MaxRoomSize + 1);
            var lengthOffset = (MaxRoomSize - roomLength) / 2;
            var roomWidth = Random.Range(MinRoomSize, MaxRoomSize + 1);
            var widthOffset = (MaxRoomSize - roomWidth) / 2;
            // Offsets are for putting the room in the middle of its tile
            _roomSizes[lowX / (MaxRoomSize + RoomPadding)][lowY / (MaxRoomSize + RoomPadding)] =
                new Vector2Int(roomWidth, roomLength);
            // Room sizes will be needed for corridor generation
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
            var unusedPaths = new List<Tuple<Vector2Int, Vector2Int>>();
            var traversedRooms = new HashSet<Vector2Int>();
            var currentCoords = _startCoords;
            var corridorCandidates = new List<Tuple<Vector2Int, Vector2Int>>();
            // generate main corridors, create connected graph
            while (traversedRooms.Count < _roomCount - 1)
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
                    // delete every possible corridor that leads to the already connected room
                    if (!traversedRooms.Contains(chosenPath.Item2)) break;
                    unusedPaths.Add(chosenPath);
                }

                GenerateCorridor(chosenPath);
                // Debug.Log("used room: " + currentCoords + " used path: " + chosenPath);
                currentCoords = chosenPath.Item2; // destination becomes new current node
                // if (traversedRooms.Count == _roomCount) break;
            }

            unusedPaths.AddRange(corridorCandidates); // add unused corridors at the end
            var loopCountChances = new[] { 0f, 0.2f, 0.5f, 0.85f, 1.0f };
            var loopRandomValue = Random.Range(0.0f, 1.0f);
            for (var loopCount = 0; loopCount < loopCountChances.Length - 1; loopCount++)
            {
                if (loopRandomValue < loopCountChances[loopCount + 1])
                {
                    Debug.Log(loopCount + " " + unusedPaths.Count);
                    for (var i = 0; i < loopCount && unusedPaths.Count > 0; i++)
                    {
                        var chosenIndex = Random.Range(0, unusedPaths.Count);
                        GenerateCorridor(unusedPaths[chosenIndex]);
                        unusedPaths[chosenIndex] = unusedPaths[^1];
                        unusedPaths.RemoveAt(unusedPaths.Count - 1);
                    }

                    break;
                }
            }
        }

        private void GenerateCorridor(Tuple<Vector2Int, Vector2Int> path)
        {
            var firstRoomPos = path.Item1;
            var secondRoomPos = path.Item2;
            int room1Width = _roomSizes[firstRoomPos.x][firstRoomPos.y].x,
                room1Length = _roomSizes[firstRoomPos.x][firstRoomPos.y].y;
            int room2Width = _roomSizes[secondRoomPos.x][secondRoomPos.y].x,
                room2Length = _roomSizes[secondRoomPos.x][secondRoomPos.y].y;

            var direction = new Vector2Int(
                secondRoomPos.x -
                firstRoomPos.x, // rooms are always adjacent, result will be the direction the corridor moves in
                secondRoomPos.y - firstRoomPos.y);
            // currentPos will be one tile behind the edge of the room

            var firstRoomRangeStart = direction.x != 0 ? (MaxRoomSize - room1Width) / 2 + 2 : (MaxRoomSize - room1Length) / 2 + 2;
            var firstRoomRangeEnd = direction.x != 0 ? (MaxRoomSize - room1Width) / 2 + room1Width - 2 : (MaxRoomSize - room1Length) / 2 + room1Length - 2;
            var secondRoomRangeStart = direction.x != 0 ? (MaxRoomSize - room2Width) / 2 + 2 : (MaxRoomSize - room2Length) / 2 + 2;
            var secondRoomRangeEnd = direction.x != 0 ? (MaxRoomSize - room2Width) / 2 + room2Width - 2 : (MaxRoomSize - room2Length) / 2 + room2Length - 2;

            var firstRoomCorridorPos = Random.Range(firstRoomRangeStart, firstRoomRangeEnd);
            var secondRoomCorridorPos = Random.Range(secondRoomRangeStart, secondRoomRangeEnd);

            var currentPos = new Vector2(0, 0);
            var distanceIntoFirstRoom = 0;
            var distanceIntoSecondRoom = 0;
            switch (direction.x)
            {
                case -1:
                    currentPos =
                        new Vector2(
                            (MaxRoomSize + RoomPadding) * firstRoomPos.x + (MaxRoomSize - room1Width) / 2 + 2,
                            (MaxRoomSize + RoomPadding) * firstRoomPos.y + firstRoomCorridorPos);
                    distanceIntoFirstRoom = (MaxRoomSize - room1Width) / 2;
                    distanceIntoSecondRoom = MaxRoomSize - room2Width - (MaxRoomSize - room2Width) / 2;
                    break;
                case 1:
                    currentPos =
                        new Vector2(
                            (MaxRoomSize + RoomPadding) * firstRoomPos.x + room1Width + (MaxRoomSize - room1Width) / 2 - 2,
                            (MaxRoomSize + RoomPadding) * firstRoomPos.y + firstRoomCorridorPos);
                    distanceIntoFirstRoom = MaxRoomSize - room1Width - (MaxRoomSize - room1Width) / 2;
                    distanceIntoSecondRoom = (MaxRoomSize - room2Width) / 2;
                    break;
            }

            switch (direction.y)
            {
                case -1:
                    currentPos =
                        new Vector2(
                            (MaxRoomSize + RoomPadding) * firstRoomPos.x + firstRoomCorridorPos,
                            (MaxRoomSize + RoomPadding) * firstRoomPos.y + (MaxRoomSize - room1Length) / 2 + 2);
                    distanceIntoFirstRoom = (MaxRoomSize - room1Length) / 2;
                    distanceIntoSecondRoom = MaxRoomSize - room2Length - (MaxRoomSize - room2Length) / 2;
                    break;
                case 1:
                    currentPos =
                        new Vector2(
                            (MaxRoomSize + RoomPadding) * firstRoomPos.x + firstRoomCorridorPos,
                            (MaxRoomSize + RoomPadding) * firstRoomPos.y + room1Length +
                            (MaxRoomSize - room1Length) / 2 - 2);
                    distanceIntoFirstRoom = MaxRoomSize - room1Length - (MaxRoomSize - room1Length) / 2;
                    distanceIntoSecondRoom = (MaxRoomSize - room2Length) / 2;
                    break;
            }

            var paddingSeparationPoint = Random.Range(1, RoomPadding);
            int counter = 0,
                firstTileLimit = distanceIntoFirstRoom + paddingSeparationPoint + 2,
                secondTileLimit = distanceIntoSecondRoom + RoomPadding - paddingSeparationPoint + 2;
            while (true)
            {
                if (_tileGrid[(int)currentPos.x, (int)currentPos.y] == 0)
                {
                    var newTile = Instantiate(groundPrefabs[Random.Range(0, groundPrefabs.Length)], currentPos * 2,
                        Quaternion.identity, transform);
                    // newTile.GetComponent<Tilemap>().color = counter < firstTileLimit ? Color.blue : Color.red;
                }

                _tileGrid[(int)currentPos.x, (int)currentPos.y] = 4; // 4 = essential to corridor
                counter++;
                if (counter == firstTileLimit + secondTileLimit + 1) break;
                if (counter == firstTileLimit)
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
                        // tile.GetComponent<Tilemap>().color = Color.magenta;
                        _tileGrid[(int)currentPos.x, (int)currentPos.y] = 4;
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
                        // InstantiateBorder(x * 2, y * 2, GetMatrix4Neighbors(_tileGrid, x, y, 1).Count > 0 ? 1 : 0);
                        InstantiateBorder(x * 2, y * 2, (roomGridX + roomGridY) % 2 == 0 ? 1 : 0);
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
            // var borderPrefabIndex = objectIndex == 1 ? 0 : 1;
            var borderPrefabIndex = Random.Range(0, borderPrefabs.Length);
            GameObject borderObject = Instantiate(borderPrefabs[borderPrefabIndex],
                new Vector2(x, y), Quaternion.identity);
            borderObject.transform.parent = transform;
        }

        private void GenerateObstacles()
        {
            float seedX = Random.Range(0, 1000.0f), seedY = Random.Range(0, 1000.0f);
            for (var y = 0; y < MaxRoomsPerDimension; y++)
            {
                for (var x = 0; x < MaxRoomsPerDimension; x++)
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
                                _freeTilesInRoom[x + y * MaxRoomsPerDimension].Add(new Vector2Int(tileX, tileY));
                        }
                    }
                }
            }
        }

        private void GenerateEntities()
        {
            var totalEnemyCount = 0;
            var numberOfChests = _mapIndex % 3 == 0 ? _roomCount / 2 : _roomCount / 3;
            var chestsPerRoom = new int[_roomCount];
            for (var i = 0; i < numberOfChests; i++)
            {
                var chosenIndex = Random.Range(0, _roomCount);
                while (chestsPerRoom[chosenIndex] >= 2) chosenIndex = (chosenIndex + 1) % _roomCount;
                chestsPerRoom[chosenIndex]++;
            }

            var existingRoomIndex = 0;
            // Debug.Log("room count: " + _roomCount);
            for (var y = 0; y < MaxRoomsPerDimension; y++)
            {
                for (var x = 0; x < MaxRoomsPerDimension; x++)
                {
                    var roomIndex = x + y * MaxRoomsPerDimension;

                    if (_roomGrid[x, y] == 0) continue;

                    while (chestsPerRoom[existingRoomIndex]-- > 0)
                    {
                        var chestSpawnTile = PopRandomFreeTile(roomIndex);

                        Instantiate(chestPrefabs[Random.Range(0, chestPrefabs.Length)],
                            new Vector2(chestSpawnTile.x * 2 - Random.Range(0, 2),
                                chestSpawnTile.y * 2 + Random.Range(0, 2)), Quaternion.identity, transform);
                    }

                    if (_endCoords.x == x && _endCoords.y == y) // spawn portal
                    {
                        var portalSpawnTile = PopRandomFreeTile(roomIndex);

                        _portal = Instantiate(portalPrefabs[0],
                            new Vector2(portalSpawnTile.x * 2, portalSpawnTile.y * 2), Quaternion.identity, transform);
                    }

                    if (_startCoords.x == x && _startCoords.y == y) // spawn player
                    {
                        var playerSpawnTile = PopRandomFreeTile(roomIndex);

                        player.transform.position = new Vector2(playerSpawnTile.x * 2 + 0.5f, playerSpawnTile.y * 2);
                    }
                    else // spawn enemies if it's not the starting room
                    {
                        // spawn boss if conditions are met
                        if (_endCoords.x == x && _endCoords.y == y)
                        {
                            var bossSpawnTile = PopRandomFreeTile(roomIndex);
                            Instantiate(bossPrefabs[Random.Range(0, bossPrefabs.Length)],
                                new Vector2(bossSpawnTile.x * 2, bossSpawnTile.y * 2), Quaternion.identity, transform);
                        }

                        var enemyCount = Random.Range(0, MaxRoomSize / 2 + MaxRoomSize % 2);
                        if (enemyCount == 0)
                            enemyCount =
                                Random.Range(0.0f, 1.0f) > 0.5f ? 1 : 0; // making it rare for empty rooms to exist

                        totalEnemyCount += enemyCount;

                        while (enemyCount-- > 0)
                        {
                            var enemySpawnTile = PopRandomFreeTile(roomIndex);
                            Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)],
                                new Vector2(enemySpawnTile.x * 2, enemySpawnTile.y * 2), Quaternion.identity,
                                transform);
                        }

                        existingRoomIndex++;
                        // Debug.Log("room index: " + existingRoomIndex);
                    }
                }
            }

            _totalEnemyCount = _enemiesRemaining = totalEnemyCount;
        }

        private Vector2Int PopRandomFreeTile(int roomIndex)
        {
            var tileIndex = Random.Range(0, _freeTilesInRoom[roomIndex].Count);
            var chosenTile = _freeTilesInRoom[roomIndex][tileIndex];
            _freeTilesInRoom[roomIndex][tileIndex] = _freeTilesInRoom[roomIndex][^1];
            _freeTilesInRoom[roomIndex].RemoveAt(_freeTilesInRoom[roomIndex].Count - 1);
            return chosenTile;
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