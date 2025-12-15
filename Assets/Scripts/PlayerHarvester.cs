using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHarvester : MonoBehaviour
{
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;
    public float hitCooldown = 0.15f;

    private float _nextHitTime;
    private Camera _cam;
    public Inventory inventory;
    InventoryUI invenUI;

    public GameObject selectedBlock;

    private void Awake()
    {
        _cam = Camera.main;
        if (inventory == null) inventory = gameObject.AddComponent<Inventory>();
        invenUI = FindObjectOfType<InventoryUI>();
    }

    void Update()
    {
        // 1. 현재 어떤 상태인지 판단하기 위한 변수들
        bool hasItemSelected = invenUI.selectedIndex >= 0;
        bool isTool = false;
        ItemType currentItemType = ItemType.Dirt; // 기본값

        // 아이템이 선택되어 있다면, 그게 도구인지 확인
        if (hasItemSelected)
        {
            currentItemType = invenUI.GetInventorySlot();
            isTool = CheckIsTool(currentItemType);
        }

        // 2. 미리보기 블록(투명 블록) 처리
        // 아무것도 안 들었거나, 도구를 들고 있으면 -> 미리보기 끄기
        if (!hasItemSelected || isTool)
        {
            selectedBlock.transform.localScale = Vector3.zero;
        }
        else // 블록을 들고 있으면 -> 미리보기 켜기
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
        // [채굴 모드]: 아무것도 안 들었거나(맨손) OR 도구를 들고 있을 때
        if (!hasItemSelected || isTool)
        {
            if (Input.GetMouseButton(0) && Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + hitCooldown;

                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
                        // 도구에 따른 데미지 계산
                        int damage = 1; // 기본 데미지
                        if (isTool)
                        {
                            damage = GetToolDamage(currentItemType);
                        }

                        // 블록 때리기
                        block.Hit(damage, inventory);
                        // Debug.Log($"공격! 도구: {currentItemType}, 데미지: {damage}");
                    }
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
                        FindObjectOfType<NoiseVoxelMap>().PlaceTile(placePos, currentItemType);
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

    // 도구별 데미지를 반환하는 함수
    int GetToolDamage(ItemType type)
    {
        switch (type)
        {
            case ItemType.Sword: return 3;
            case ItemType.Pickaxe: return 5;
            case ItemType.Axe: return 2;
            default: return 1;
        }
    }

    static Vector3Int AdjacentCellOnHitFace(in RaycastHit hit)
    {
        Vector3 baseCenter = hit.collider.transform.position;   //맞춘 블록의 중심
        Vector3 adjCenter = baseCenter + hit.normal;
        return Vector3Int.RoundToInt(adjCenter);
    }
}