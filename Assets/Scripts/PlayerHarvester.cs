using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ItemType은 Block.cs 파일에서 정의된 것으로 가정합니다.
// public enum ItemType { Dirt, Grass, Water, Iron, Axe, Sword, Pickaxe, Wood }

public class PlayerHarvester : MonoBehaviour
{
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;
    public float hitCooldown = 0.15f;

    private float _nextHitTime;
    private Camera _cam;
    public Inventory inventory;
    InventoryUI invenUI;
    // 맵 관리를 위해 NoiseVoxelMap 참조 (설치 시 필요)
    private NoiseVoxelMap voxelMap;

    public GameObject selectedBlock;

    private void Awake()
    {
        _cam = Camera.main;
        if (inventory == null) inventory = gameObject.AddComponent<Inventory>();
        invenUI = FindObjectOfType<InventoryUI>();
        // 맵 관리 스크립트 찾기 (설치 로직을 위해 필요)
        voxelMap = FindObjectOfType<NoiseVoxelMap>();
    }

    void Update()
    {
        // 1. 현재 어떤 상태인지 판단하기 위한 변수들
        bool hasItemSelected = invenUI.selectedIndex >= 0;
        bool isTool = false;
        ItemType currentItemType = ItemType.Dirt; // 기본값

        if (hasItemSelected)
        {
            currentItemType = invenUI.GetInventorySlot();
            isTool = CheckIsTool(currentItemType);
        }

        // 2. 미리보기 블록(투명 블록) 처리 
        if (!hasItemSelected || isTool)
        {
            selectedBlock.transform.localScale = Vector3.zero;
        }
        else
        {
            Ray rayDebug = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(rayDebug, out var hitDebug, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                Vector3Int placePos = AdjacentCellOnHitFace(hitDebug);
                selectedBlock.transform.localScale = Vector3.one;
                selectedBlock.transform.position = placePos;
                selectedBlock.transform.rotation = Quaternion.identity;
            }
            else
            {
                selectedBlock.transform.localScale = Vector3.zero;
            }
        }

        // 3. 마우스 클릭 처리
        // [채굴/공격 모드]: 아무것도 안 들었거나(맨손) OR 도구를 들고 있을 때
        if (!hasItemSelected || isTool)
        {
            if (Input.GetMouseButton(0) && Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + hitCooldown;

                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    int damage = 1;
                    if (isTool)
                    {
                        damage = GetToolDamage(currentItemType);
                    }

                    // ====== 💡 적 공격 로직 ======
                    var enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage, transform.position);
                        return; // 적을 공격했으면 다른 로직을 건너뜁니다.
                    }
                    // ======================================

                    // ====== 💡 블록 채굴 로직 (적을 맞추지 않았을 때 실행) ======

                    // ⚔️ [핵심 수정] 검(Sword)을 들고 있다면, 적을 공격하지 않았을 경우 블록도 채굴하지 않습니다.
                    if (currentItemType == ItemType.Sword)
                    {
                        // 검은 블록에 데미지를 줄 수 없습니다.
                        return;
                    }

                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
                        block.Hit(damage, inventory);
                    }
                    // =========================================================
                }
            }
        }
        // [설치 모드]: 블록을 들고 있을 때 (도구가 아닐 때)
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3Int placePos = AdjacentCellOnHitFace(hit);

                    // 설치 시도
                    if (inventory.Consume(currentItemType, 1))
                    {
                        // 맵 관리 클래스의 PlaceTile 호출
                        if (voxelMap != null)
                        {
                            voxelMap.PlaceTile(placePos, currentItemType);
                        }
                    }
                }
            }
        }
    }

    // 아이템이 도구인지 판별하는 함수
    bool CheckIsTool(ItemType type)
    {
        return type == ItemType.Axe || type == ItemType.Pickaxe || type == ItemType.Sword;
    }

    // 도구별 데미지를 반환하는 함수 (적 공격에도 사용됨)
    int GetToolDamage(ItemType type)
    {
        switch (type)
        {
            case ItemType.Sword: return 3;
            case ItemType.Pickaxe: return 5;
            case ItemType.Axe: return 2;
            default: return 1; // 맨손 데미지
        }
    }

    static Vector3Int AdjacentCellOnHitFace(in RaycastHit hit)
    {
        Vector3 baseCenter = hit.collider.transform.position;
        Vector3 adjCenter = baseCenter + hit.normal;
        return Vector3Int.RoundToInt(adjCenter);
    }
}