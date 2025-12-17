using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text; // 문자열 처리를 위해 필요

public class NoiseVoxelMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapID = 0; // 맵마다 1, 2, 3 다르게 설정 필수!
    public bool resetMapData = false; // 체크하고 시작하면 초기화됨

    // --- 변경 사항 저장용 데이터 ---
    // 키: 좌표(x,y,z), 값: 0이면 파괴됨(Air), 그 외에는 설치된 ItemType 번호
    private Dictionary<Vector3Int, int> modifiedBlocks = new Dictionary<Vector3Int, int>();

    // --- 기존 변수들 ---
    public float offsetX;
    public float offsetZ;

    public int width = 20;
    public int depth = 20;
    public int maxHeight = 16;
    public int waterLevel = 4;

    [SerializeField] public float noiseScale = 20f;

    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject waterPrefab;
    public GameObject orePrefab;
    public GameObject woodPrefab;
    public GameObject stonePrefab;

    [Header("Optimization")]
    public float viewDistance = 25f;
    public float checkInterval = 0.5f;
    private Transform playerTransform;
    private List<GameObject> allBlocks = new List<GameObject>();

    [Header("Tree Generation")]
    public int minTrees = 5;
    public int maxTrees = 10;
    public ItemType woodDropType = ItemType.Wood;
    public int woodDropAmount = 3;

    [Header("Stone Generation")]
    public int minDepthForStone = 4;
    public int maxDepthForStone = 5;

    private Dictionary<Vector2Int, int> topBlockHeight = new Dictionary<Vector2Int, int>();

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // 1. 저장된 맵 데이터 불러오기
        LoadMapData();

        // 2. 맵 시드 설정 (기존과 동일)
        string keyX = $"Map_{mapID}_SeedX";
        string keyZ = $"Map_{mapID}_SeedZ";

        if (PlayerPrefs.HasKey(keyX))
        {
            offsetX = PlayerPrefs.GetFloat(keyX);
            offsetZ = PlayerPrefs.GetFloat(keyZ);
        }
        else
        {
            offsetX = Random.Range(-9999f, 9999f);
            offsetZ = Random.Range(-9999f, 9999f);
            PlayerPrefs.SetFloat(keyX, offsetX);
            PlayerPrefs.SetFloat(keyZ, offsetZ);
            PlayerPrefs.Save();
        }

        GenerateMap(); // 맵 생성 분리함
    }

    void GenerateMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float nx = (x + offsetX) / noiseScale;
                float nz = (z + offsetZ) / noiseScale;
                float noise = Mathf.PerlinNoise(nx, nz);
                int h = Mathf.FloorToInt(noise * maxHeight);
                if (h <= 0) h = 1;

                topBlockHeight[new Vector2Int(x, z)] = h;

                // 지형 생성
                for (int y = 0; y <= h; y++)
                {
                    // **핵심**: 저장된 데이터에 "파괴됨(0)"이라고 되어있으면 생성 안 함
                    if (CheckIsDestroyed(x, y, z)) continue;

                    int depthFromTop = h - y;
                    if (depthFromTop >= minDepthForStone && depthFromTop <= maxDepthForStone)
                        PlaceStone(x, y, z);
                    else if (y == h)
                        PlaceGrass(x, y, z);
                    else
                        PlaceDirt(x, y, z);
                }

                // 물 생성
                for (int y = h + 1; y <= waterLevel; y++)
                {
                    if (CheckIsDestroyed(x, y, z)) continue; // 물도 파괴되었으면 생성 안 함
                    PlaceWater(x, y, z);
                }
            }
        }

        // 나무 생성 (나무 위치의 바닥이 파괴되지 않았을 때만)
        PlaceTrees();

        // **추가**: 플레이어가 직접 설치했던 블록들 복구하기
        RestorePlacedBlocks();

        if (playerTransform != null) StartCoroutine(OptimizeBlocksRoutine());
    }

    // --- 저장/로드 시스템 (핵심) ---

    // 외부(Block.cs 등)에서 블록이 파괴될 때 호출
    public void RegisterBlockDestruction(Vector3Int pos)
    {
        // 0은 'Air'(파괴됨)을 의미
        if (modifiedBlocks.ContainsKey(pos)) modifiedBlocks[pos] = 0;
        else modifiedBlocks.Add(pos, 0);

        SaveMapData(); // 즉시 저장
    }

    // 외부(PlayerHarvester.cs)에서 블록을 설치할 때 호출 (기존 PlaceTile 수정)
    public void PlaceTile(Vector3Int pos, ItemType type)
    {
        // 1. 변경 사항 저장 (Type을 int로 변환해서 저장)
        int typeID = (int)type;
        if (modifiedBlocks.ContainsKey(pos)) modifiedBlocks[pos] = typeID;
        else modifiedBlocks.Add(pos, typeID);
        SaveMapData();

        // 2. 실제 블록 생성
        SpawnBlockObject(pos, type);
    }

    // 좌표가 "파괴된 상태"인지 확인
    bool CheckIsDestroyed(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        // 키가 존재하고 값이 0이면 파괴된 것
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] == 0) return true;

        // 키가 존재하고 값이 0이 아니면(다른 블록 설치), 일단 원래 지형은 안 만듬 (Restore에서 생성함)
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0) return true;

        return false;
    }

    void RestorePlacedBlocks()
    {
        foreach (var kvp in modifiedBlocks)
        {
            // 값이 0이 아니면 플레이어가 설치한 블록임
            if (kvp.Value != 0)
            {
                SpawnBlockObject(kvp.Key, (ItemType)kvp.Value);
            }
        }
    }

    void SpawnBlockObject(Vector3Int pos, ItemType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case ItemType.Dirt: prefab = dirtPrefab; break;
            case ItemType.Grass: prefab = grassPrefab; break;
            case ItemType.Water: prefab = waterPrefab; break;
            case ItemType.Stone: prefab = stonePrefab; break;
            case ItemType.Wood: prefab = woodPrefab; break;
            case ItemType.Iron: prefab = orePrefab; break;
        }

        if (prefab != null)
        {
            var go = Instantiate(prefab, (Vector3)pos, Quaternion.identity, transform);
            go.name = $"{type}_{pos.x}_{pos.y}_{pos.z}";
            allBlocks.Add(go);
        }
    }

    // 데이터를 문자열로 변환해 저장 (간단한 방식)
    void SaveMapData()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in modifiedBlocks)
        {
            // 포맷: x,y,z,typeID|
            sb.Append($"{kvp.Key.x},{kvp.Key.y},{kvp.Key.z},{kvp.Value}|");
        }
        PlayerPrefs.SetString($"MapData_{mapID}", sb.ToString());
        // Debug.Log("맵 데이터 저장됨");
    }

    void LoadMapData()
    {
        if (resetMapData)
        {
            PlayerPrefs.DeleteKey($"MapData_{mapID}");
            PlayerPrefs.DeleteKey($"Map_{mapID}_SeedX");
            PlayerPrefs.DeleteKey($"Map_{mapID}_SeedZ");
            return;
        }

        string data = PlayerPrefs.GetString($"MapData_{mapID}", "");
        if (string.IsNullOrEmpty(data)) return;

        string[] entries = data.Split('|');
        foreach (string entry in entries)
        {
            if (string.IsNullOrEmpty(entry)) continue;
            string[] parts = entry.Split(',');
            if (parts.Length == 4)
            {
                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);
                int z = int.Parse(parts[2]);
                int type = int.Parse(parts[3]);
                modifiedBlocks[new Vector3Int(x, y, z)] = type;
            }
        }
    }

    // --- 기존 단순 배치 함수들 (생성 로직에서만 사용됨) ---
    private void PlaceWater(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Water); }
    private void PlaceDirt(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Dirt); }
    private void PlaceStone(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Stone); }
    private void PlaceGrass(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Grass); }
    private void PlaceWood(int x, int y, int z)
    {
        SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Wood);
        // (주의) 나무 드랍 설정 로직은 SpawnBlockObject 내부에 통합하거나 별도 처리 필요하지만, 
        // 복잡도를 줄이기 위해 여기서는 기본 생성만 합니다. 필요하면 추가하세요.
    }

    private void PlaceTrees()
    {
        // (기존 나무 로직 유지하되 PlaceWood 호출)
        if (woodPrefab == null) return;
        int numberOfTrees = Random.Range(minTrees, maxTrees + 1);
        List<Vector2Int> availablePositions = new List<Vector2Int>(topBlockHeight.Keys);
        List<Vector2Int> safePositions = new List<Vector2Int>();

        foreach (var posXZ in availablePositions)
        {
            int highestBlockY = topBlockHeight[posXZ];
            if (highestBlockY >= waterLevel) safePositions.Add(posXZ);
        }

        if (safePositions.Count < numberOfTrees) numberOfTrees = safePositions.Count;
        List<Vector2Int> treePositions = new List<Vector2Int>();
        for (int i = 0; i < numberOfTrees; i++)
        {
            int randomIndex = Random.Range(0, safePositions.Count);
            treePositions.Add(safePositions[randomIndex]);
            safePositions.RemoveAt(randomIndex);
        }
        foreach (var posXZ in treePositions)
        {
            int x = posXZ.x;
            int z = posXZ.y;
            int y = topBlockHeight[posXZ] + 1;
            // 나무가 생성될 자리가 파괴된 상태가 아니면 생성
            if (!CheckIsDestroyed(x, y, z)) PlaceWood(x, y, z);
        }
    }

    // OptimizeBlocksRoutine은 그대로 유지...
    IEnumerator OptimizeBlocksRoutine()
    {
        float viewDistSqr = viewDistance * viewDistance;
        while (true)
        {
            Vector3 playerPos = playerTransform.position;
            for (int i = allBlocks.Count - 1; i >= 0; i--)
            {
                GameObject block = allBlocks[i];
                if (block == null) { allBlocks.RemoveAt(i); continue; }
                float distSqr = (block.transform.position - playerPos).sqrMagnitude;
                if (distSqr > viewDistSqr) { if (block.activeSelf) block.SetActive(false); }
                else { if (!block.activeSelf) block.SetActive(true); }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
}