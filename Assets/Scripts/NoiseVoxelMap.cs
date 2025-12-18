using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement; // 씬 이름 확인용

public class NoiseVoxelMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapID = 0;
    public bool resetMapData = false;
    private static bool isSessionInitialized = false;

    // 데이터 저장용
    private Dictionary<Vector3Int, int> modifiedBlocks = new Dictionary<Vector3Int, int>();
    public Dictionary<Vector3Int, GameObject> activeBlocks = new Dictionary<Vector3Int, GameObject>();

    // 맵 설정 변수
    public float offsetX;
    public float offsetZ;
    public int width = 20;
    public int depth = 20;
    public int maxHeight = 16;
    public int waterLevel = 4;
    [SerializeField] public float noiseScale = 20f;

    // --- 테마 설정 변수 (자동 설정됨) ---
    private ItemType currentSurface; // 표면 블록
    private ItemType currentFill;    // 채움 블록
    private ItemType currentFluid;   // 액체
    private bool generateTrees = true;
    private bool generateStone = true;

    // 프리팹 연결
    [Header("All Block Prefabs")]
    public GameObject grassPrefab;
    public GameObject dirtPrefab;
    public GameObject waterPrefab;
    public GameObject orePrefab;
    public GameObject woodPrefab;
    public GameObject stonePrefab;

    [Header("Biome Prefabs")]
    public GameObject netherrackPrefab;
    public GameObject lavaPrefab;
    public GameObject endStonePrefab;

    [Header("Optimization")]
    public float viewDistance = 25f;
    public float checkInterval = 0.5f;
    private Transform playerTransform;

    [Header("Tree Generation")]
    public int minTrees = 5;
    public int maxTrees = 10;

    [Header("Stone Generation")]
    public int minDepthForStone = 4;

    [Header("Loading Settings")]
    public GameObject loadingPanel;
    public MonoBehaviour playerController;

    private Dictionary<Vector2Int, int> topBlockHeight = new Dictionary<Vector2Int, int>();

    public static NoiseVoxelMap Instance;

    private void Awake() // Start 대신 Awake 권장
    {
        // 씬이 시작되자마자 "내가 대장이다"라고 등록
        Instance = this;
    }

    void Start()
    {
        if (!isSessionInitialized)
        {
            PlayerPrefs.DeleteAll();
            isSessionInitialized = true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        LoadMapData();

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

        // 씬 이름에 따라 바이옴(테마) 설정
        SetupBiomeByScene();

        GenerateMap();
    }

    // 🌟 씬 이름을 확인해서 테마를 설정하는 함수
    void SetupBiomeByScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        // Debug.Log($"[Map] 현재 씬: {sceneName} / 테마 적용 중...");

        if (sceneName.Contains("Nether")) // 네더 (지옥)
        {
            currentSurface = ItemType.Netherrack;
            currentFill = ItemType.Netherrack;
            currentFluid = ItemType.Lava;
            generateTrees = false;
            generateStone = false; // 돌 생성 안 함
            waterLevel = 4;
        }
        else if (sceneName.Contains("End")) // 엔드 (우주)
        {
            currentSurface = ItemType.EndStone;
            currentFill = ItemType.EndStone;
            currentFluid = ItemType.Dirt; // 의미 없음 (수위 -10)
            generateTrees = false;
            generateStone = false;
            waterLevel = -10; // 물/용암 없음
        }
        else // 기본 (오버월드)
        {
            currentSurface = ItemType.Grass;
            currentFill = ItemType.Dirt;
            currentFluid = ItemType.Water;
            generateTrees = true;
            generateStone = true;
            // waterLevel은 인스펙터 설정값(4) 따름
        }
    }

    void GenerateMap()
    {
        StartCoroutine(GenerateMapRoutine());
    }

    IEnumerator GenerateMapRoutine()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);

        Rigidbody playerRB = null;
        if (playerTransform != null)
        {
            playerRB = playerTransform.GetComponent<Rigidbody>();
            if (playerRB != null) playerRB.isKinematic = true;
            if (playerController != null) playerController.enabled = false;
        }

        topBlockHeight.Clear();
        foreach (var block in activeBlocks.Values) if (block != null) Destroy(block);
        activeBlocks.Clear();

        // 높이 계산
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

        // 블록 생성
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int h = topBlockHeight[new Vector2Int(x, z)];

                for (int y = 0; y <= h; y++)
                {
                    if (CheckIsDestroyed(x, y, z)) continue;
                    if (IsHidden(x, y, z)) continue;

                    ItemType type = currentFill; // 기본값

                    if (y == h) type = currentSurface; // 표면
                    else if (generateStone && h - y >= minDepthForStone) type = ItemType.Stone; // 돌 (오버월드만)

                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0)
                        type = (ItemType)modifiedBlocks[pos];

                    SpawnBlockObject(pos, type);
                    blocksCreatedPerFrame++;
                }

                // 액체 생성 (물 or 용암)
                if (waterLevel > 0)
                {
                    for (int y = h + 1; y <= waterLevel; y++)
                    {
                        if (CheckIsDestroyed(x, y, z)) continue;
                        SpawnBlockObject(new Vector3Int(x, y, z), currentFluid);
                        blocksCreatedPerFrame++;
                    }
                }
            }

            if (blocksCreatedPerFrame > 100)
            {
                blocksCreatedPerFrame = 0;
                yield return null;
            }
        }

        PlaceTrees();
        MovePlayerToCenter();

        yield return new WaitForSeconds(0.5f);

        if (loadingPanel != null) loadingPanel.SetActive(false);

        if (playerTransform != null)
        {
            if (playerRB != null)
            {
                playerRB.isKinematic = false;
                playerRB.velocity = Vector3.zero;
            }
            if (playerController != null) playerController.enabled = true;
        }

        StartCoroutine(OptimizeBlocksRoutine());
    }

    private void PlaceTrees()
    {
        if (!generateTrees) return; // 테마에 따라 나무 안 만듦
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

            if (!CheckIsDestroyed(x, y, z))
            {
                Vector3Int treePos = new Vector3Int(x, y, z);

                // 나무 데이터 저장 (캘 때 나무 나오게)
                if (!modifiedBlocks.ContainsKey(treePos))
                    modifiedBlocks.Add(treePos, (int)ItemType.Wood);
                else
                    modifiedBlocks[treePos] = (int)ItemType.Wood;

                SpawnBlockObject(treePos, ItemType.Wood);
            }
        }
    }

    // --- 블록 파괴 및 아이템 드롭 ---
    public void RegisterBlockDestruction(Vector3Int pos)
    {
        ItemType typeToDrop = GetBlockTypeAt(pos.x, pos.y, pos.z);

        if (modifiedBlocks.ContainsKey(pos)) modifiedBlocks[pos] = 0;
        else modifiedBlocks.Add(pos, 0);
        SaveMapData();

        if (activeBlocks.ContainsKey(pos)) activeBlocks.Remove(pos);
        UpdateNeighbors(pos);

        SpawnBlockDrop(pos, typeToDrop);
    }

    void SpawnBlockDrop(Vector3Int pos, ItemType type)
    {
        GameObject prefabToSpawn = GetPrefabByType(type);
        if (prefabToSpawn == null) return;

        // 프리팹에서 dropCount 읽기
        int amountToDrop = 1;
        Block originBlockScript = prefabToSpawn.GetComponent<Block>();
        if (originBlockScript != null) amountToDrop = originBlockScript.dropCount;

        Vector3 randomOffset = Random.insideUnitSphere * 0.2f;
        Vector3 spawnPos = new Vector3(pos.x, pos.y, pos.z) + Vector3.one * 0.5f + randomOffset;

        GameObject drop = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        drop.name = "Drop_" + type.ToString();

        // 블록 -> 아이템 변환
        Block blockScript = drop.GetComponent<Block>();
        if (blockScript != null) Destroy(blockScript);
        drop.transform.localScale = Vector3.one * 0.25f;

        Rigidbody rb = drop.AddComponent<Rigidbody>();
        rb.mass = 1f;

        ItemPickup pickup = drop.AddComponent<ItemPickup>();
        pickup.itemType = type;
        pickup.amount = amountToDrop; // 수정됨
        pickup.pickupRange = 3f;
        pickup.moveSpeed = 10f;

        Collider col = drop.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        // 플레이어 충돌 무시
        if (playerTransform != null && col != null)
        {
            Collider playerCol = playerTransform.GetComponent<Collider>();
            if (playerCol == null) playerCol = playerTransform.GetComponent<CharacterController>();
            if (playerCol != null) Physics.IgnoreCollision(playerCol, col, true);
        }

        Renderer rend = drop.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }
    }

    // --- 아이템 던지기 (Q키/우클릭) ---
    public void ThrowItem(Vector3 startPos, ItemType type, int count, Vector3 throwDirection)
    {
        GameObject prefabToSpawn = GetPrefabByType(type);
        if (prefabToSpawn == null) return;

        GameObject drop = Instantiate(prefabToSpawn, startPos, Quaternion.identity);
        drop.name = "Thrown_" + type.ToString();

        Block blockScript = drop.GetComponent<Block>();
        if (blockScript != null) Destroy(blockScript);
        drop.transform.localScale = Vector3.one * 0.25f;

        Rigidbody rb = drop.GetComponent<Rigidbody>();
        if (rb == null) rb = drop.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.5f;

        ItemPickup pickup = drop.AddComponent<ItemPickup>();
        pickup.itemType = type;
        pickup.amount = count;
        pickup.pickupDelay = 1.0f;
        pickup.pickupRange = 3f;
        pickup.moveSpeed = 10f;

        Collider dropCol = drop.GetComponent<Collider>();
        if (dropCol != null) dropCol.isTrigger = false;

        // 플레이어 충돌 무시
        if (playerTransform != null && dropCol != null)
        {
            Collider playerCol = playerTransform.GetComponent<Collider>();
            if (playerCol == null) playerCol = playerTransform.GetComponent<CharacterController>();
            if (playerCol != null) Physics.IgnoreCollision(playerCol, dropCol, true);
        }

        rb.AddForce(throwDirection, ForceMode.Impulse);
    }

    // --- 유틸리티 및 기타 함수들 ---

    bool IsHidden(int x, int y, int z)
    {
        if (x <= 0 || x >= width - 1 || z <= 0 || z >= depth - 1 || y <= 0) return false;
        if (!IsSolid(x, y + 1, z)) return false;
        if (!IsSolid(x, y - 1, z)) return false;
        if (!IsSolid(x + 1, y, z)) return false;
        if (!IsSolid(x - 1, y, z)) return false;
        if (!IsSolid(x, y, z + 1)) return false;
        if (!IsSolid(x, y, z - 1)) return false;
        return true;
    }

    bool IsSolid(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] == 0) return false;
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0) return true;
        if (topBlockHeight.TryGetValue(new Vector2Int(x, z), out int h))
        {
            if (y <= h) return true;
        }
        return false;
    }

    public void UpdateNeighbors(Vector3Int brokenPos)
    {
        Vector3Int[] neighbors = {
            brokenPos + Vector3Int.up, brokenPos + Vector3Int.down,
            brokenPos + Vector3Int.left, brokenPos + Vector3Int.right,
            brokenPos + Vector3Int.forward, brokenPos + Vector3Int.back
        };

        foreach (var pos in neighbors)
        {
            if (activeBlocks.ContainsKey(pos)) continue;
            if (IsSolid(pos.x, pos.y, pos.z))
            {
                ItemType type = GetBlockTypeAt(pos.x, pos.y, pos.z);
                SpawnBlockObject(pos, type);
            }
        }
    }

    ItemType GetBlockTypeAt(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0)
            return (ItemType)modifiedBlocks[pos];

        if (topBlockHeight.TryGetValue(new Vector2Int(x, z), out int h))
        {
            if (y == h) return currentSurface; // 테마 적용
            if (generateStone && h - y >= minDepthForStone) return ItemType.Stone;
            return currentFill; // 테마 적용
        }
        return currentFill;
    }

    GameObject GetPrefabByType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Dirt: return dirtPrefab;
            case ItemType.Grass: return grassPrefab;
            case ItemType.Water: return waterPrefab;
            case ItemType.Stone: return stonePrefab;
            case ItemType.Wood: return woodPrefab;
            case ItemType.Iron: return orePrefab;

            // 바이옴 전용 블록
            case ItemType.Netherrack: return netherrackPrefab;
            case ItemType.Lava: return lavaPrefab;
            case ItemType.EndStone: return endStonePrefab;

            default: return null;
        }
    }

    public void PlaceTile(Vector3Int pos, ItemType type)
    {
        int typeID = (int)type;
        if (modifiedBlocks.ContainsKey(pos)) modifiedBlocks[pos] = typeID;
        else modifiedBlocks.Add(pos, typeID);
        SaveMapData();
        SpawnBlockObject(pos, type);
    }

    bool CheckIsDestroyed(int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] == 0) return true;
        return false;
    }

    void SpawnBlockObject(Vector3Int pos, ItemType type)
    {
        if (activeBlocks.ContainsKey(pos)) return;
        GameObject prefab = GetPrefabByType(type);
        if (type == ItemType.Water || type == ItemType.Lava) prefab = GetPrefabByType(type);

        if (prefab != null)
        {
            var go = Instantiate(prefab, (Vector3)pos, Quaternion.identity, transform);
            go.name = $"{type}_{pos.x}_{pos.y}_{pos.z}";
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

        // 용암/물보다 위에서 시작하도록
        if (targetY <= waterLevel) targetY = waterLevel + 2;

        playerTransform.position = new Vector3(centerX, targetY, centerZ);
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = Vector3.zero;
    }

    void SaveMapData()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in modifiedBlocks)
            sb.Append($"{kvp.Key.x},{kvp.Key.y},{kvp.Key.z},{kvp.Value}|");
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

    IEnumerator OptimizeBlocksRoutine()
    {
        float viewDistSqr = viewDistance * viewDistance;
        while (true)
        {
            if (playerTransform == null) yield break;
            Vector3 playerPos = playerTransform.position;
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