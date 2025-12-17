using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ItemType은 별도 파일이나 상단에 정의되어 있다고 가정
// public enum ItemType { Dirt, Grass, Water, Iron, Axe, Sword, Pickaxe, Wood, Stone, Light }

public class PlayerHarvester : MonoBehaviour
{
    [Header("Settings")]
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;
    public float hitCooldown = 0.15f;

    [Header("References")]
    public Inventory inventory;
    public GameObject selectedBlock; // 미리보기용 반투명 블록

    private float _nextHitTime;
    private Camera _cam;
    private InventoryUI invenUI;
    private NoiseVoxelMap voxelMap;


    public GameObject lightProjectilePrefab;


    private void Awake()
    {
        _cam = Camera.main;

        if (inventory == null) inventory = gameObject.AddComponent<Inventory>();
        invenUI = FindObjectOfType<InventoryUI>();
        voxelMap = FindObjectOfType<NoiseVoxelMap>();
    }

    void Update()
    {
        // 1. 현재 인벤토리 상태 확인
        bool hasItemSelected = invenUI.selectedIndex >= 0;
        bool isTool = false;
        ItemType currentItemType = ItemType.Dirt; // 기본값

        if (hasItemSelected)
        {
            currentItemType = invenUI.GetInventorySlot();
            isTool = CheckIsTool(currentItemType);
        }

        // =========================================================
        // 🏗️ 2. 미리보기 블록(Preview) 처리
        // =========================================================
        // 도구를 들고 있거나, 맨손(아무것도 선택 안 함)일 때는 미리보기를 숨깁니다.
        if (!hasItemSelected || isTool)
        {
            if (selectedBlock) selectedBlock.transform.localScale = Vector3.zero;
        }
        else
        {
            // 블록을 들고 있을 때만 미리보기 표시
            Ray rayDebug = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(rayDebug, out var hitDebug, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                Vector3Int placePos = AdjacentCellOnHitFace(hitDebug);
                if (selectedBlock)
                {
                    selectedBlock.transform.localScale = Vector3.one;
                    selectedBlock.transform.position = placePos;
                    selectedBlock.transform.rotation = Quaternion.identity;
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

        // [모드 A] 공격 및 채굴 (맨손이거나 도구를 들었을 때)
        if (!hasItemSelected || isTool)
        {
            if (Input.GetMouseButton(0) && Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + hitCooldown;

                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    int damage = 1;
                    if (isTool) damage = GetToolDamage(currentItemType);

                    // 1순위: 적(Enemy) 공격
                    var enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage, transform.position);
                        return; // 적을 때렸으면 블록은 안 캠
                    }

                    // 2순위: 블록 채굴
                    // (검으로는 블록을 못 캐게 막음)
                    if (currentItemType == ItemType.Sword) return;

                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
                        // [수정됨] 인벤토리 전달 삭제 (맵 시스템이 드롭 처리함)
                        block.Hit(damage);
                    }
                }
            }
        }
        // [모드 B] 블록 설치 (블록 아이템을 들고 있을 때)
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3Int placePos = AdjacentCellOnHitFace(hit);

                    // 인벤토리에서 아이템 소모 성공 시 설치
                    if (inventory.Consume(currentItemType, 1))
                    {
                        if (voxelMap != null)
                        {
                            voxelMap.PlaceTile(placePos, currentItemType);
                        }
                    }
                }
            }
        }

        // =========================================================
        // 🗑️ 4. 아이템 버리기 기능 (Q키 & 우클릭)
        // =========================================================
        if (hasItemSelected) // 아이템을 들고 있을 때만 작동
        {
            // 던질 시작 위치 (카메라 앞)
            Vector3 throwStartPos = transform.position + _cam.transform.forward * 1.0f + Vector3.up * 1.5f;
            // 던질 힘과 방향
            Vector3 throwForce = _cam.transform.forward * 8f;

            // [Q키]: 뭉텅이로 버리기
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // 인벤토리에 이 아이템이 몇 개 있는지 확인
                // (Inventory.cs에 GetItemCount 함수가 있어야 합니다!)
                int count = inventory.GetItemCount(currentItemType);

                if (count > 0 && inventory.Consume(currentItemType, count))
                {
                    voxelMap.ThrowItem(throwStartPos, currentItemType, count, throwForce);
                }
            }

            // [우클릭]: 1개씩 버리기
            // (설치 모드일 때 좌클릭과 겹치지 않으므로 안전)
            if (Input.GetMouseButtonDown(1))
            {
                if (inventory.Consume(currentItemType, 1))
                {
                    voxelMap.ThrowItem(throwStartPos, currentItemType, 1, throwForce);
                }
            }
        }

        if (hasItemSelected && currentItemType == ItemType.Light)
        {
            // 완성된 '빛'을 들고 우클릭했을 때
            if (Input.GetMouseButtonDown(1))
            {
                if (inventory.Consume(ItemType.Light, 1))
                {
                    LaunchLight();
                }
            }
        }
    }

    // --- Helper Functions ---

    bool CheckIsTool(ItemType type)
    {
        return type == ItemType.Axe || type == ItemType.Pickaxe || type == ItemType.Sword || type == ItemType.StoneAxe || type == ItemType.StoneAxe || type == ItemType.StonePickaxe || type == ItemType.StoneSword;
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
        // 플레이어 앞쪽에 빛 오브젝트 생성
        Vector3 spawnPos = transform.position + _cam.transform.forward * 1.5f + Vector3.up * 1.5f;
        GameObject lightObj = Instantiate(lightProjectilePrefab, spawnPos, Quaternion.identity);

        // 인벤토리 UI 알림 활용
        inventory.ShowNotice("빛이 하늘로 떠오릅니다! 세상이 밝아집니다.");
    }
}