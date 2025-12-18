using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // ✨ 1. UI 클릭 감지를 위해 필수!

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

    // [수정] 포털 UI 연결용 변수
    public PortalUI portalUI;
    // 혹은 private으로 쓰고 아래 Awake에서 찾아도 됩니다.

    public GameObject lightProjectilePrefab;

    private void Awake()
    {
        _cam = Camera.main;

        if (inventory == null) inventory = gameObject.AddComponent<Inventory>();
        invenUI = FindObjectOfType<InventoryUI>();

        // ✨ 2. (중요) true를 넣어서 '꺼져있는' PortalUI도 찾을 수 있게 수정!
        if (portalUI == null)
            portalUI = FindObjectOfType<PortalUI>(true);
    }

    void Update()
    {
        // ✨ 3. (핵심) 마우스가 UI 위에 있다면 플레이어의 행동(공격, 설치, 포털토글)을 멈춤!
        // 이걸 넣어야 버튼을 눌렀을 때 포털이 닫히지 않고 버튼 기능이 작동함
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // ---------------------------------------------------------

        // 1. 현재 인벤토리 상태 및 아이템 확인
        bool hasItemSelected = invenUI.selectedIndex >= 0;
        bool isTool = false;
        ItemType currentItemType = ItemType.Dirt;

        if (hasItemSelected)
        {
            currentItemType = invenUI.GetInventorySlot();
            isTool = CheckIsTool(currentItemType);
        }

        // 현재 든 아이템이 '빛 조각'인지 확인
        bool isLightPiece = (hasItemSelected && currentItemType == ItemType.LightPiece);

        // =========================================================
        // 🏗️ 2. 미리보기 블록(Preview) 처리
        // =========================================================

        // 빛 조각(isLightPiece)일 때도 미리보기를 끕니다.
        if (!hasItemSelected || isTool || isLightPiece)
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
        // 🖱️ 3. 좌클릭 처리 (포털 열기 / 공격 / 채굴 / 설치)
        // =========================================================

        // [CASE 1] 빛 조각을 들고 있을 때 -> 포털 UI 열기
        if (isLightPiece)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (portalUI != null)
                {
                    portalUI.TogglePortal(); // 포털 UI 켜기/끄기
                }
                else
                {
                    Debug.LogWarning("PortalUI를 찾을 수 없습니다! Awake에서 (true)를 넣었는지 확인하세요.");
                }
            }
        }
        // [CASE 2] 맨손이거나 도구를 들었을 때 -> 공격 및 채굴
        else if (!hasItemSelected || isTool)
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
                        return; // 적을 때렸으면 블록은 안 캠
                    }

                    // 검(Sword)으로는 블록 채굴 불가
                    if (currentItemType == ItemType.Sword) return;

                    var block = hit.collider.GetComponent<Block>();
                    if (block != null)
                    {
                        block.Hit(damage);
                    }
                }
            }
        }
        // [CASE 3] 블록 아이템을 들고 있을 때 -> 설치
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponent<IDamageable>() != null) return;

                    Vector3Int placePos = AdjacentCellOnHitFace(hit);

                    // 아이템 1개 소모
                    if (inventory.Consume(currentItemType, 1))
                    {
                        // 싱글톤 Instance를 통해 현재 맵에 설치
                        if (NoiseVoxelMap.Instance != null)
                        {
                            NoiseVoxelMap.Instance.PlaceTile(placePos, currentItemType);
                        }
                        else
                        {
                            Debug.LogError("현재 씬에 NoiseVoxelMap이 없습니다! 설치 실패.");
                        }
                    }
                }
            }
        }

        // =========================================================
        // 🗑️ 4. 아이템 버리기(Q) 및 우클릭 특수 기능
        // =========================================================
        if (hasItemSelected)
        {
            Vector3 throwStartPos = transform.position + _cam.transform.forward * 1.0f + Vector3.up * 1.5f;
            Vector3 throwForce = _cam.transform.forward * 8f;

            // Q키: 아이템 버리기
            if (Input.GetKeyDown(KeyCode.Q))
            {
                int count = inventory.GetItemCount(currentItemType);
                if (count > 0 && inventory.Consume(currentItemType, count))
                {
                    if (NoiseVoxelMap.Instance != null)
                        NoiseVoxelMap.Instance.ThrowItem(throwStartPos, currentItemType, count, throwForce);
                }
            }

            // 우클릭: 특수 기능 (빛 조각 쏘아올리기 등)
            if (Input.GetMouseButtonDown(1))
            {
                if (currentItemType == ItemType.LightPiece || currentItemType == ItemType.Light)
                {
                    if (inventory.Consume(currentItemType, 1))
                    {
                        LaunchLight();
                    }
                }
                else
                {
                    // 일반 아이템 1개 버리기
                    if (inventory.Consume(currentItemType, 1))
                    {
                        if (NoiseVoxelMap.Instance != null)
                            NoiseVoxelMap.Instance.ThrowItem(throwStartPos, currentItemType, 1, throwForce);
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
        if (lightProjectilePrefab != null)
            Instantiate(lightProjectilePrefab, spawnPos, Quaternion.identity);
        inventory.ShowNotice("빛이 하늘로 떠오릅니다! 세상이 밝아집니다.");
    }
}