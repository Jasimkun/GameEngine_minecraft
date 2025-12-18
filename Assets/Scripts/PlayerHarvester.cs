using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHarvester : MonoBehaviour
{
    [Header("Settings")]
    public float rayDistance = 5f;
    public LayerMask hitMask = ~0;
    public float hitCooldown = 0.5f; // 공격 속도를 고려해 조금 늘림 (0.15 -> 0.5)

    [Header("References")]
    public Inventory inventory;
    public GameObject selectedBlock;

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
                // 적이나 플레이어에게는 블록 설치 미리보기를 띄우지 않음
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

        // [모드 A] 공격 및 채굴 (맨손이거나 도구를 들었을 때)
        if (!hasItemSelected || isTool)
        {
            // 공격은 보통 클릭할 때마다(Down) 나가거나, 꾹 누르면(Button) 연속 공격
            if (Input.GetMouseButtonDown(0) && Time.time >= _nextHitTime)
            {
                _nextHitTime = Time.time + hitCooldown;

                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    int damage = 1;
                    if (isTool) damage = GetToolDamage(currentItemType);

                    // 🔻 [수정됨] IDamageable 인터페이스를 찾아서 공격
                    // Fire 스크립트가 IDamageable을 가지고 있으므로 인식됨
                    IDamageable target = hit.collider.GetComponent<IDamageable>();

                    if (target != null)
                    {
                        target.TakeDamage(damage);
                        Debug.Log($"[공격] {hit.collider.name}을(를) 공격! 데미지: {damage}");

                        // 타격 이펙트 등을 여기에 추가할 수 있음
                        return; // 적을 때렸으면 블록은 안 캠
                    }

                    // 2순위: 블록 채굴
                    // (검으로는 블록을 못 캐게 막음)
                    if (currentItemType == ItemType.Sword) return;

                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
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
                    // 적에게 클릭했을 때는 블록 설치 방지
                    if (hit.collider.GetComponent<IDamageable>() != null) return;

                    Vector3Int placePos = AdjacentCellOnHitFace(hit);

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
        // 🗑️ 4. 아이템 버리기 등 기타 기능 (유지)
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
                    voxelMap.ThrowItem(throwStartPos, currentItemType, count, throwForce);
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                // '빛' 아이템 특수 사용 로직
                if (currentItemType == ItemType.Light)
                {
                    if (inventory.Consume(ItemType.Light, 1))
                    {
                        LaunchLight();
                    }
                }
                // 일반 아이템 버리기
                else
                {
                    if (inventory.Consume(currentItemType, 1))
                    {
                        voxelMap.ThrowItem(throwStartPos, currentItemType, 1, throwForce);
                    }
                }
            }
        }
    }

    // --- Helper Functions ---

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