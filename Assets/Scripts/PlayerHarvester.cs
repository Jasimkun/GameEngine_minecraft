using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHarvester : MonoBehaviour
{
    [Header("Settings")]
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;
    public float hitCooldown = 0.5f;

    [Header("References")]
    public Inventory inventory;
    public GameObject selectedBlock;

    private float _nextHitTime;
    private Camera _cam;
    private InventoryUI invenUI;

    // ❌ [삭제] 옛날 맵을 기억하는 변수는 이제 필요 없음
    // private NoiseVoxelMap voxelMap; 

    public GameObject lightProjectilePrefab;

    private void Awake()
    {
        _cam = Camera.main;

        if (inventory == null) inventory = gameObject.AddComponent<Inventory>();
        invenUI = FindObjectOfType<InventoryUI>();

        // ❌ [삭제] 여기서 맵을 찾으면 1번 씬 맵만 기억하게 됨
        // voxelMap = FindObjectOfType<NoiseVoxelMap>(); 
    }

    void Update()
    {
        // 1. 현재 인벤토리 상태 확인
        bool hasItemSelected = invenUI.selectedIndex >= 0;
        bool isTool = false;
        ItemType currentItemType = ItemType.Dirt;

        if (hasItemSelected)
        {
            currentItemType = invenUI.GetInventorySlot();
            isTool = CheckIsTool(currentItemType);
        }

        // =========================================================
        // 🏗️ 2. 미리보기 블록(Preview) 처리
        // =========================================================
        if (!hasItemSelected || isTool)
        {
            if (selectedBlock) selectedBlock.transform.localScale = Vector3.zero;
        }
        else
        {
            Ray rayDebug = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(rayDebug, out var hitDebug, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hitDebug.collider.CompareTag("Enemy") || hitDebug.collider.CompareTag("Player"))
                {
                    if (selectedBlock) selectedBlock.transform.localScale = Vector3.zero;
                }
                else
                {
                    Vector3Int placePos = AdjacentCellOnHitFace(hitDebug);
                    if (selectedBlock)
                    {
                        selectedBlock.transform.localScale = Vector3.one;
                        selectedBlock.transform.position = placePos;
                        selectedBlock.transform.rotation = Quaternion.identity;
                    }
                }
            }
            else
            {
                if (selectedBlock) selectedBlock.transform.localScale = Vector3.zero;
            }
        }

        // =========================================================
        // 🖱️ 3. 좌클릭 처리 (공격 / 채굴 / 설치)
        // =========================================================

        // [모드 A] 공격 및 채굴
        if (!hasItemSelected || isTool)
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + hitCooldown;

                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    int damage = 1;
                    if (isTool) damage = GetToolDamage(currentItemType);

                    IDamageable target = hit.collider.GetComponent<IDamageable>();

                    if (target != null)
                    {
                        target.TakeDamage(damage);
                        return;
                    }

                    if (currentItemType == ItemType.Sword) return;

                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
                        block.Hit(damage);
                    }
                }
            }
        }
        // [모드 B] 블록 설치
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponent<IDamageable>() != null) return;

                    Vector3Int placePos = AdjacentCellOnHitFace(hit);

                    // 1. 아이템 소모 시도
                    if (inventory.Consume(currentItemType, 1))
                    {
                        // ✅ [수정] Instance를 통해 현재 씬의 살아있는 맵에게 명령
                        if (NoiseVoxelMap.Instance != null)
                        {
                            NoiseVoxelMap.Instance.PlaceTile(placePos, currentItemType);
                        }
                        else
                        {
                            Debug.LogError("현재 씬에 NoiseVoxelMap이 없습니다! 설치 실패.");
                            // (선택사항) 실패했으면 아이템 다시 돌려주기 로직 추가 가능
                        }
                    }
                }
            }
        }

        // =========================================================
        // 🗑️ 4. 아이템 버리기 등 기타 기능
        // =========================================================
        if (hasItemSelected)
        {
            Vector3 throwStartPos = transform.position + _cam.transform.forward * 1.0f + Vector3.up * 1.5f;
            Vector3 throwForce = _cam.transform.forward * 8f;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                int count = inventory.GetItemCount(currentItemType);
                if (count > 0 && inventory.Consume(currentItemType, count))
                {
                    // ✅ [수정] 여기도 Instance 사용
                    if (NoiseVoxelMap.Instance != null)
                        NoiseVoxelMap.Instance.ThrowItem(throwStartPos, currentItemType, count, throwForce);
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (currentItemType == ItemType.Light)
                {
                    if (inventory.Consume(ItemType.Light, 1))
                    {
                        LaunchLight();
                    }
                }
                else
                {
                    if (inventory.Consume(currentItemType, 1))
                    {
                        // ✅ [수정] 여기도 Instance 사용
                        if (NoiseVoxelMap.Instance != null)
                            NoiseVoxelMap.Instance.ThrowItem(throwStartPos, currentItemType, 1, throwForce);
                    }
                }
            }
        }
    }

    // --- Helper Functions (그대로 유지) ---
    bool CheckIsTool(ItemType type)
    {
        return type == ItemType.Axe || type == ItemType.Pickaxe || type == ItemType.Sword ||
               type == ItemType.StoneAxe || type == ItemType.StonePickaxe || type == ItemType.StoneSword;
    }

    int GetToolDamage(ItemType type)
    {
        switch (type)
        {
            case ItemType.Sword: return 2;
            case ItemType.Pickaxe: return 3;
            case ItemType.Axe: return 2;
            case ItemType.StoneAxe: return 3;
            case ItemType.StonePickaxe: return 5;
            case ItemType.StoneSword: return 4;
            default: return 1;
        }
    }

    static Vector3Int AdjacentCellOnHitFace(in RaycastHit hit)
    {
        Vector3 baseCenter = hit.collider.transform.position;
        Vector3 adjCenter = baseCenter + hit.normal;
        return Vector3Int.RoundToInt(adjCenter);
    }

    void LaunchLight()
    {
        Vector3 spawnPos = transform.position + _cam.transform.forward * 1.5f + Vector3.up * 1.5f;
        GameObject lightObj = Instantiate(lightProjectilePrefab, spawnPos, Quaternion.identity);
        inventory.ShowNotice("빛이 하늘로 떠오릅니다! 세상이 밝아집니다.");
    }
}