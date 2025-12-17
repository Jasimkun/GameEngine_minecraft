using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class NoiseVoxelMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapID = 0;
    public bool resetMapData = false;

    // 게임 세션 동안 초기화 여부 체크 (게임을 껐다 켜면 초기화됨)
    private static bool isSessionInitialized = false;

    // --- 데이터 저장용 ---
    // 플레이어가 변경한 블록 정보 (0:파괴됨, 그외:설치된 블록타입)
    private Dictionary<Vector3Int, int> modifiedBlocks = new Dictionary<Vector3Int, int>();

    // 현재 눈에 보이는(활성화된) 블록 관리 (좌표로 검색용)
    public Dictionary<Vector3Int, GameObject> activeBlocks = new Dictionary<Vector3Int, GameObject>();

    // --- 맵 설정 변수들 ---
    public float offsetX;
    public float offsetZ;

    public int width = 20;
    public int depth = 20;
    public int maxHeight = 16;
    public int waterLevel = 4;

    [SerializeField] public float noiseScale = 20f;

    // --- 프리팹 연결 ---
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

    [Header("Tree Generation")]
    public int minTrees = 5;
    public int maxTrees = 10;
    public ItemType woodDropType = ItemType.Wood;
    public int woodDropAmount = 3;

    [Header("Stone Generation")]
    public int minDepthForStone = 4;
    public int maxDepthForStone = 5;

    // 지형 높이 저장용
    private Dictionary<Vector2Int, int> topBlockHeight = new Dictionary<Vector2Int, int>();

    void Start()
    {
        // 1. 게임 시작 시 데이터 초기화 로직
        if (!isSessionInitialized)
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("새 게임 시작: 모든 맵 데이터가 초기화되었습니다.");
            isSessionInitialized = true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // 2. 저장된 맵 데이터 불러오기
        LoadMapData();

        // 3. 맵 시드(Seed) 설정
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

        // 4. 맵 생성 시작
        GenerateMap();
    }

    void GenerateMap()
    {
        StartCoroutine(GenerateMapRoutine());
    }

    IEnumerator GenerateMapRoutine()
    {
        topBlockHeight.Clear();

        // 기존에 생성된 블록이 있다면 모두 삭제 (초기화)
        foreach (var block in activeBlocks.Values)
        {
            if (block != null) Destroy(block);
        }
        activeBlocks.Clear();

        // [단계 1] 높이 데이터 먼저 계산 (전체 지형의 높이를 알아야 숨김 처리가 가능)
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
            }
        }

        int blocksCreatedPerFrame = 0;

        // [단계 2] 실제 블록 생성 루프
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int h = topBlockHeight[new Vector2Int(x, z)];

                // 지형 생성 (바닥부터 높이 h까지)
                for (int y = 0; y <= h; y++)
                {
                    // 파괴된 블록이면 생성 스킵
                    if (CheckIsDestroyed(x, y, z)) continue;

                    // 💡 [최적화 핵심] 사방이 막혀있으면 생성하지 않음 (투명 처리)
                    if (IsHidden(x, y, z)) continue;

                    // 생성 타입 결정
                    ItemType type = ItemType.Dirt;
                    if (y == h) type = ItemType.Grass;
                    else if (h - y >= minDepthForStone) type = ItemType.Stone;

                    // 플레이어가 설치한 블록 정보가 있다면 덮어쓰기
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0)
                        type = (ItemType)modifiedBlocks[pos];

                    SpawnBlockObject(pos, type);
                    blocksCreatedPerFrame++;
                }

                // 물 생성
                for (int y = h + 1; y <= waterLevel; y++)
                {
                    if (CheckIsDestroyed(x, y, z)) continue;

                    // 물은 투명하니까 보통 다 그려줍니다.
                    SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Water);
                    blocksCreatedPerFrame++;
                }
            }

            // 프레임 드랍 방지를 위해 끊어서 생성
            if (blocksCreatedPerFrame > 100)
            {
                blocksCreatedPerFrame = 0;
                yield return null;
            }
        }

        // 나무 생성 및 플레이어 이동
        PlaceTrees();
        MovePlayerToCenter();

        // 시야 거리 최적화 코루틴 시작
        if (playerTransform != null) StartCoroutine(OptimizeBlocksRoutine());

        Debug.Log("맵 생성 완료!");
    }

    // 💡 [핵심 함수 1] 해당 좌표가 사방에 꽉 막혀있는지 확인
    bool IsHidden(int x, int y, int z)
    {
        // 맵의 가장자리는 뚫려있다고 가정 (보여야 하니까)
        if (x <= 0 || x >= width - 1 || z <= 0 || z >= depth - 1 || y <= 0) return false;

        // 상하좌우앞뒤 6면 확인
        if (!IsSolid(x, y + 1, z)) return false; // 위가 뚫렸으면 보여야 함
        if (!IsSolid(x, y - 1, z)) return false; // 아래가 뚫렸으면 보여야 함
        if (!IsSolid(x + 1, y, z)) return false;
        if (!IsSolid(x - 1, y, z)) return false;
        if (!IsSolid(x, y, z + 1)) return false;
        if (!IsSolid(x, y, z - 1)) return false;

        // 여기까지 왔으면 6면이 다 막힌 것 -> 숨겨도 됨
        return true;
    }

    // 💡 [핵심 함수 2] 해당 좌표에 블록이 존재하는지 판단 (논리적 확인)
    bool IsSolid(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);

        // 1. 유저가 파괴했으면 공기(False)
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] == 0) return false;

        // 2. 유저가 설치했으면 고체(True)
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0) return true;

        // 3. 자연 지형 높이보다 아래인가?
        if (topBlockHeight.TryGetValue(new Vector2Int(x, z), out int h))
        {
            if (y <= h) return true;
        }

        // 4. 물은 투명하므로 Solid로 치지 않음 (물 뒤의 땅은 보여야 함)
        if (y <= waterLevel) return false;

        return false; // 공기
    }

    // 💡 [핵심 함수 3] 블록이 파괴되었을 때 주변 블록을 다시 보여줌
    public void UpdateNeighbors(Vector3Int brokenPos)
    {
        Vector3Int[] neighbors = {
            brokenPos + Vector3Int.up,
            brokenPos + Vector3Int.down,
            brokenPos + Vector3Int.left,
            brokenPos + Vector3Int.right,
            brokenPos + Vector3Int.forward,
            brokenPos + Vector3Int.back
        };

        foreach (var pos in neighbors)
        {
            // 이미 눈에 보이는 블록이면 패스
            if (activeBlocks.ContainsKey(pos)) continue;

            // 데이터 상으로는 블록이 있어야 하는 자리인가?
            if (IsSolid(pos.x, pos.y, pos.z))
            {
                // 그렇다면 이제 드러나야 한다! -> 생성
                ItemType type = GetBlockTypeAt(pos.x, pos.y, pos.z);
                SpawnBlockObject(pos, type);
            }
        }
    }

    // 좌표의 블록 타입 알아내기
    ItemType GetBlockTypeAt(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        // 유저 설치 블록 확인
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0)
            return (ItemType)modifiedBlocks[pos];

        // 자연 생성 로직 확인
        if (topBlockHeight.TryGetValue(new Vector2Int(x, z), out int h))
        {
            if (y == h) return ItemType.Grass;
            if (h - y >= minDepthForStone) return ItemType.Stone;
            return ItemType.Dirt;
        }
        return ItemType.Dirt; // 기본값
    }

    // === 외부 호출 및 기능 함수들 ===

    // 블록 파괴 시 호출 (Block.cs에서 호출)
    public void RegisterBlockDestruction(Vector3Int pos)
    {
        // 1. 파괴 정보 저장
        if (modifiedBlocks.ContainsKey(pos)) modifiedBlocks[pos] = 0;
        else modifiedBlocks.Add(pos, 0);
        SaveMapData();

        // 2. 현재 보이는 블록 리스트에서 제거
        if (activeBlocks.ContainsKey(pos)) activeBlocks.Remove(pos);

        // 3. 주변에 숨어있던 친구들 깨우기 (매우 중요)
        UpdateNeighbors(pos);
    }

    // 블록 설치 시 호출 (PlayerHarvester.cs에서 호출)
    public void PlaceTile(Vector3Int pos, ItemType type)
    {
        int typeID = (int)type;
        if (modifiedBlocks.ContainsKey(pos)) modifiedBlocks[pos] = typeID;
        else modifiedBlocks.Add(pos, typeID);
        SaveMapData();

        SpawnBlockObject(pos, type);
        // 설치 시에는 주변을 가리는 로직이 필요할 수 있지만, 복잡도를 위해 생략 (기능상 문제 없음)
    }

    bool CheckIsDestroyed(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] == 0) return true;
        return false;
    }

    // 실제 게임 오브젝트 생성 함수
    void SpawnBlockObject(Vector3Int pos, ItemType type)
    {
        // 이미 생성된 블록이면 중복 생성 방지
        if (activeBlocks.ContainsKey(pos)) return;

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

            // 활성 블록 리스트에 등록
            activeBlocks.Add(pos, go);
        }
    }

    void MovePlayerToCenter()
    {
        if (playerTransform == null) return;
        int centerX = width / 2;
        int centerZ = depth / 2;
        int targetY = maxHeight + 5;

        Vector2Int centerPos = new Vector2Int(centerX, centerZ);
        if (topBlockHeight.ContainsKey(centerPos)) targetY = topBlockHeight[centerPos] + 2;

        // 물 높이보다 낮으면 물 위로
        if (targetY <= waterLevel) targetY = waterLevel + 2;

        playerTransform.position = new Vector3(centerX, targetY, centerZ);

        // 물리 속도 초기화
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = Vector3.zero;
    }

    // === 저장 및 로드 ===
    void SaveMapData()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in modifiedBlocks)
        {
            sb.Append($"{kvp.Key.x},{kvp.Key.y},{kvp.Key.z},{kvp.Value}|");
        }
        PlayerPrefs.SetString($"MapData_{mapID}", sb.ToString());
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

    // === 기타 배치 함수들 (Load시 복구용) ===
    void RestorePlacedBlocks()
    {
        foreach (var kvp in modifiedBlocks)
        {
            if (kvp.Value != 0) SpawnBlockObject(kvp.Key, (ItemType)kvp.Value);
        }
    }

    // === 단순 배치 헬퍼 함수 ===
    private void PlaceWater(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Water); }
    private void PlaceDirt(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Dirt); }
    private void PlaceStone(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Stone); }
    private void PlaceGrass(int x, int y, int z) { SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Grass); }
    private void PlaceWood(int x, int y, int z)
    {
        SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Wood);
    }

    private void PlaceTrees()
    {
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
            // 해당 위치가 파괴되지 않았으면 나무 생성
            if (!CheckIsDestroyed(x, y, z)) SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Wood);
        }
    }

    // === 최적화 루틴 ===
    IEnumerator OptimizeBlocksRoutine()
    {
        float viewDistSqr = viewDistance * viewDistance;
        while (true)
        {
            Vector3 playerPos = playerTransform.position;
            // Dictionary는 루프 돌면서 수정 불가능하므로 리스트로 키만 복사
            List<GameObject> checkList = new List<GameObject>(activeBlocks.Values);

            foreach (var go in checkList)
            {
                if (go == null) continue;
                float distSqr = (go.transform.position - playerPos).sqrMagnitude;
                if (distSqr > viewDistSqr)
                {
                    if (go.activeSelf) go.SetActive(false);
                }
                else
                {
                    if (!go.activeSelf) go.SetActive(true);
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
}