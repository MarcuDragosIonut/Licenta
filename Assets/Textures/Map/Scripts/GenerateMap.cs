using UnityEngine;

namespace Textures.Map.Scripts
{
    public class GenerateMap : MonoBehaviour
    {
        public GameObject grassGroundPrefab;
        public GameObject grassBorderPrefab;
        public GameObject grassCornerPrefab;

        public int mapWidth = 10;
        public int mapHeight = 10;

        private const float NumberOfTilesInPrefab = 6f;

        void Start()
        {
            Generate();
        }

        void Generate()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Vector3 position = new Vector3(x * NumberOfTilesInPrefab, y * NumberOfTilesInPrefab, 0);
                    Instantiate(grassGroundPrefab, position, Quaternion.identity, transform);
                }
            }

            AddBorders(mapWidth, mapHeight, NumberOfTilesInPrefab);
            AddCorners(mapWidth, mapHeight, NumberOfTilesInPrefab);
        }

        void AddBorders(int width, int height, float NumberOfTilesInPrefab)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 topPosition = new Vector3(x * NumberOfTilesInPrefab, height * NumberOfTilesInPrefab, 0);
                Instantiate(grassBorderPrefab, topPosition, Quaternion.Euler(0, 0, 180), transform);

                Vector3 bottomPosition = new Vector3(x * NumberOfTilesInPrefab, -NumberOfTilesInPrefab, 0);
                Instantiate(grassBorderPrefab, bottomPosition, Quaternion.identity, transform);
            }

            for (int y = 0; y < height; y++)
            {
                Vector3 rightPosition = new Vector3(width * NumberOfTilesInPrefab, y * NumberOfTilesInPrefab, 0);
                Instantiate(grassBorderPrefab, rightPosition, Quaternion.Euler(0, 0, 90), transform);

                Vector3 leftPosition = new Vector3(-NumberOfTilesInPrefab, y * NumberOfTilesInPrefab, 0);
                Instantiate(grassBorderPrefab, leftPosition, Quaternion.Euler(0, 0, -90), transform);
            }
        }

        void AddCorners(int width, int height, float NumberOfTilesInPrefab)
        {
            Vector3 topLeft = new Vector3(-NumberOfTilesInPrefab, height * NumberOfTilesInPrefab, 0);
            Instantiate(grassCornerPrefab, topLeft, Quaternion.Euler(0, 0, 0), transform);

            Vector3 topRight = new Vector3(width * NumberOfTilesInPrefab, height * NumberOfTilesInPrefab, 0);
            Instantiate(grassCornerPrefab, topRight, Quaternion.Euler(0, 0, 0), transform);

            Vector3 bottomLeft = new Vector3(-NumberOfTilesInPrefab, -NumberOfTilesInPrefab, 0);
            Instantiate(grassCornerPrefab, bottomLeft, Quaternion.Euler(0, 0, 0), transform);

            Vector3 bottomRight = new Vector3(width * NumberOfTilesInPrefab, -NumberOfTilesInPrefab, 0);
            Instantiate(grassCornerPrefab, bottomRight, Quaternion.Euler(0, 0, 0), transform);
        }
    }
}