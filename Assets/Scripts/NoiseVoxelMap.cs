using UnityEngine;
using System.Collections.Generic;

public class NoiseVoxelMap : MonoBehaviour
{
    public float offsetX;
    public float offsetZ;

    public int width = 20;
    public int depth = 20;
    public int maxHeight = 16;
    public int waterLevel = 4;

    [SerializeField] public float noiseScale = 20f;

    // 프리팹
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject waterPrefab;
    public GameObject orePrefab;
    public GameObject woodPrefab;

    // 광물 생성 설정
    public int oreMaxHeight = 7;
    [SerializeField] public float oreNoiseScale = 45f;
    [Range(0.0f, 1.0f)] public float oreThreshold = 0.55f;
    [Range(0.0f, 1.0f)] public float oreChance = 0.15f;

    // 나무 생성 설정
    [Header("Tree Generation")]
    public int minTrees = 5;
    public int maxTrees = 10;
    public ItemType woodDropType = ItemType.Wood;
    public int woodDropAmount = 3;

    private Dictionary<Vector2Int, int> topBlockHeight = new Dictionary<Vector2Int, int>();


    void Start()
    {
        offsetX = Random.Range(-9999f, 9999f);
        offsetZ = Random.Range(-9999f, 9999f);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float nx = (x + offsetX) / noiseScale;
                float nz = (z + offsetZ) / noiseScale;
                float noise = Mathf.PerlinNoise(nx, nz);
                int h = Mathf.FloorToInt(noise * maxHeight);
                if (h <= 0) h = 1;

                for (int y = 0; y <= h; y++)
                {
                    if (y == h)
                        PlaceGrass(x, y, z);
                    else
                        PlaceDirt(x, y, z);
                }

                topBlockHeight[new Vector2Int(x, z)] = h;

                for (int y = h + 1; y <= waterLevel; y++)
                {
                    PlaceWater(x, y, z);
                }
            }
        }

        PlaceTrees();
    }

    private void PlaceTrees()
    {
        if (woodPrefab == null) return;

        int numberOfTrees = Random.Range(minTrees, maxTrees + 1);

        List<Vector2Int> availablePositions = new List<Vector2Int>(topBlockHeight.Keys);
        List<Vector2Int> safePositions = new List<Vector2Int>();

        // 물 위에 있는 좌표는 제외 (잔디 블록 높이가 물 레벨보다 높거나 같아야 함)
        foreach (var posXZ in availablePositions)
        {
            int highestBlockY = topBlockHeight[posXZ];
            if (highestBlockY >= waterLevel)
            {
                safePositions.Add(posXZ);
            }
        }

        if (safePositions.Count < numberOfTrees)
        {
            numberOfTrees = safePositions.Count;
        }

        List<Vector2Int> treePositions = new List<Vector2Int>();

        for (int i = 0; i < numberOfTrees; i++)
        {
            int randomIndex = Random.Range(0, safePositions.Count);
            Vector2Int posXZ = safePositions[randomIndex];

            treePositions.Add(posXZ);
            safePositions.RemoveAt(randomIndex);
        }

        foreach (var posXZ in treePositions)
        {
            int x = posXZ.x;
            int z = posXZ.y;
            int y = topBlockHeight[posXZ] + 1;

            PlaceWood(x, y, z);
        }
    }


    private void PlaceWater(int x, int y, int z)
    {
        var go = Instantiate(waterPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
        go.name = $"Water_{x}_{y}_{z}";
    }

    private void PlaceDirt(int x, int y, int z)
    {
        var go = Instantiate(dirtPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
        go.name = $"Dirt_{x}_{y}_{z}";
    }

    private void PlaceGrass(int x, int y, int z)
    {
        var go = Instantiate(grassPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
        go.name = $"Grass_{x}_{y}_{z}";
    }

    private void PlaceIron(int x, int y, int z)
    {

    }

    private void PlaceWood(int x, int y, int z)
    {
        var go = Instantiate(woodPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
        go.name = $"Wood_{x}_{y}_{z}";

        var blockComponent = go.GetComponent<Block>();
        if (blockComponent != null)
        {
            blockComponent.SetDropItem(woodDropType, woodDropAmount);
        }
    }


    public void PlaceTile(Vector3Int pos, ItemType type)
    {
        switch (type)
        {
            case ItemType.Dirt:
                PlaceDirt(pos.x, pos.y, pos.z);
                break;
            case ItemType.Grass:
                PlaceGrass(pos.x, pos.y, pos.z);
                break;
            case ItemType.Water:
                PlaceWater(pos.x, pos.y, pos.z);
                break;
            case ItemType.Iron:
                PlaceIron(pos.x, pos.y, pos.z);
                break;
            case ItemType.Wood:
                PlaceWood(pos.x, pos.y, pos.z);
                break;
        }
    }
}