using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Dirt, Grass, Water, Iron, Axe, Sword, Pickaxe, Wood, Stone }

public class Block : MonoBehaviour
{
    [Header("Block Visual Stats")]
    public string itemName;
    public Sprite itemIcon;
    public int maxStack = 99;

    // 파괴되었을 때 나올 파티클 효과 (타격감 향상)
    public GameObject breakEffectPrefab;

    [Header("Block Stat")]
    public ItemType type = ItemType.Dirt;
    [SerializeField] public int maxHP = 3; // Inspector에서 수정 가능하지만 외부에서는 접근 불가
    private int currentHP; // 실제 HP는 내부에서만 관리

    public int dropCount = 1;
    public bool mineable = true;

    // (호환성 유지) 이전 코드나 다른 곳에서 itemPrefab을 참조할 수 있어서 남겨둠
    public Block itemPrefab;

    private void Awake()
    {
        currentHP = maxHP;

        // 1. 콜라이더 자동 추가
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        // 2. 태그 안전하게 설정 (에러 방지용 try-catch)
        try
        {
            if (gameObject.CompareTag("Untagged")) gameObject.tag = "Block";
        }
        catch
        {
            // 태그가 없어도 게임이 멈추지 않도록 처리
        }
    }

    public void Hit(int damage, Inventory inven)
    {
        if (!mineable) return;

        currentHP -= damage;

        // 팁: 여기에 피격 효과(예: 블록이 잠시 흔들리거나 하얗게 번쩍임)를 넣으면 더 좋습니다.

        if (currentHP <= 0)
        {
            BreakBlock(inven);
        }
    }

    private void BreakBlock(Inventory inven)
    {
        // 1. 인벤토리에 아이템 추가
        if (inven != null && dropCount > 0)
        {
            inven.Add(type, dropCount);
        }

        // 2. 파괴 이펙트 생성 (설정된 경우에만)
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        // =========================================================
        // [중요] 맵 시스템에 "나 파괴됐어요!"라고 알리기
        // 이 코드가 있어야 나갔다 들어와도 구멍이 유지됩니다.
        // =========================================================
        NoiseVoxelMap map = FindObjectOfType<NoiseVoxelMap>();
        if (map != null)
        {
            Vector3Int pos = Vector3Int.RoundToInt(transform.position);
            map.RegisterBlockDestruction(pos);
        }
        // =========================================================

        // 3. 블록 제거
        Destroy(gameObject);
    }

    // [중요] NoiseVoxelMap 스크립트에서 나무를 심을 때 이 함수를 부릅니다.
    // 이게 없으면 맵 생성할 때 에러가 날 수 있습니다.
    public void SetDropItem(ItemType newType, int count)
    {
        type = newType;
        dropCount = count;
    }
}