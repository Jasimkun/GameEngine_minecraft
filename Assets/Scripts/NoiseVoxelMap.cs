using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

    // 프리팹 연결
    [Header("Block Prefabs")]
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

    [Header("Stone Generation")]
    public int minDepthForStone = 4;

    [Header("Loading Settings")]
    public GameObject loadingPanel;
    public MonoBehaviour playerController;

    private Dictionary<Vector2Int, int> topBlockHeight = new Dictionary<Vector2Int, int>();

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

        GenerateMap();
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

                    ItemType type = ItemType.Dirt;
                    if (y == h) type = ItemType.Grass;
                    else if (h - y >= minDepthForStone) type = ItemType.Stone;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (modifiedBlocks.ContainsKey(pos) && modifiedBlocks[pos] != 0)
                        type = (ItemType)modifiedBlocks[pos];

                    SpawnBlockObject(pos, type);
                    blocksCreatedPerFrame++;
                }

                for (int y = h + 1; y <= waterLevel; y++)
                {
                    if (CheckIsDestroyed(x, y, z)) continue;
                    SpawnBlockObject(new Vector3Int(x, y, z), ItemType.Water);
                    blocksCreatedPerFrame++;
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

    // --- 나무 생성 로직 (데이터 저장 추가됨) ---
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

            if (!CheckIsDestroyed(x, y, z))
            {
                Vector3Int treePos = new Vector3Int(x, y, z);

                // 🛠️ [수정] 나무 데이터 저장 (그래야 캘 때 나무가 나옴)
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

        // 아이템 드롭 (무조건)
        SpawnBlockDrop(pos, typeToDrop);
    }

    // 블록 프리팹을 작게 만들어서 드롭하는 함수
    // 블록 프리팹을 작게 만들어서 드롭하는 함수
    void SpawnBlockDrop(Vector3Int pos, ItemType type)
    {
        GameObject prefabToSpawn = GetPrefabByType(type);
        if (prefabToSpawn == null) return;

        // 🌟 [수정됨] 프리팹에 설정된 dropCount(개수)를 미리 읽어옵니다!
        int amountToDrop = 1;
        Block originBlockScript = prefabToSpawn.GetComponent<Block>();
        if (originBlockScript != null)
        {
            amountToDrop = originBlockScript.dropCount;
        }

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

        // 🌟 [적용] 읽어온 개수를 여기에 대입합니다.
        pickup.amount = amountToDrop;

        pickup.pickupRange = 3f;
        pickup.moveSpeed = 10f;

        Collider col = drop.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Renderer rend = drop.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        Collider dropCol = drop.GetComponent<Collider>();

        // playerTransform은 Start 함수에서 이미 찾아뒀음
        if (playerTransform != null && dropCol != null)
        {
            Collider playerCol = playerTransform.GetComponent<Collider>();

            // 만약 플레이어에 Collider가 없고 CharacterController가 있다면 이걸로 가져옴
            if (playerCol == null) playerCol = playerTransform.GetComponent<CharacterController>();

            if (playerCol != null)
            {
                // "이 아이템과 플레이어는 서로 물리적으로 무시해라!"
                Physics.IgnoreCollision(playerCol, dropCol, true);
            }
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

        Rigidbody rb = drop.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.5f;

        ItemPickup pickup = drop.AddComponent<ItemPickup>();
        pickup.itemType = type;
        pickup.amount = count;
        pickup.pickupDelay = 1.0f; // 1초간 획득 불가
        pickup.pickupRange = 3f;
        pickup.moveSpeed = 10f;

        Collider dropCol = drop.GetComponent<Collider>();
        if (dropCol != null) dropCol.isTrigger = false;

        // 플레이어 충돌 무시
        if (playerTransform != null && dropCol != null)
        {
            Collider playerCol = playerTransform.GetComponent<Collider>();
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
            if (y == h) return ItemType.Grass;
            if (h - y >= minDepthForStone) return ItemType.Stone;
            return ItemType.Dirt;
        }
        return ItemType.Dirt;
    }

    GameObject GetPrefabByType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Dirt: return dirtPrefab;
            case ItemType.Grass: return grassPrefab;
            case ItemType.Water: return null;
            case ItemType.Stone: return stonePrefab;
            case ItemType.Wood: return woodPrefab;
            case ItemType.Iron: return orePrefab;
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
        if (type == ItemType.Water) prefab = waterPrefab; // 물 예외 처리

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