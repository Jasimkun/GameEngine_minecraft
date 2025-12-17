using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ItemType { Dirt, Grass, Water, Iron, Axe, Sword, Pickaxe, Wood, Stone, Light }

public class Block : MonoBehaviour
{
    [Header("Block Visual Stats")]
    public string itemName;
    public Sprite itemIcon;
    public int maxStack = 99;

    // 파괴되었을 때 나올 파티클 효과
    public GameObject breakEffectPrefab;

    [Header("Block Stat")]
    public ItemType type = ItemType.Dirt;
    [SerializeField] public int maxHP = 3;
    private int currentHP;

    public int dropCount = 1;
    public bool mineable = true;

    // (호환성 유지)
    public Block itemPrefab;

    private void Awake()
    {
        currentHP = maxHP;

        // 1. 콜라이더 자동 추가
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        // 2. 태그 설정
        try
        {
            if (gameObject.CompareTag("Untagged")) gameObject.tag = "Block";
        }
        catch { }
    }

    // [수정됨] Inventory 매개변수 삭제 (이제 필요 없음)
    public void Hit(int damage)
    {
        if (!mineable) return;

        currentHP -= damage;

        // 팁: 피격 효과(Hit Effect)를 여기에 넣으세요.

        if (currentHP <= 0)
        {
            BreakBlock();
        }
    }

    // [수정됨] Inventory 매개변수 삭제
    private void BreakBlock()
    {
        // ❌ [삭제됨] 인벤토리에 바로 넣는 코드 삭제!
        // if (inven != null && dropCount > 0) { inven.Add(type, dropCount); }

        // 1. 파괴 이펙트 생성
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        // =========================================================
        // [중요] 맵 시스템에 알림 -> 맵 시스템이 아이템을 "바닥에 떨궈줌"
        // =========================================================
        NoiseVoxelMap map = FindObjectOfType<NoiseVoxelMap>();
        if (map != null)
        {
            Vector3Int pos = Vector3Int.RoundToInt(transform.position);
            // 이 함수가 호출되면 NoiseVoxelMap이 SpawnBlockDrop을 실행해서 아이템을 떨굽니다.
            map.RegisterBlockDestruction(pos);
        }
        // =========================================================

        // 2. 블록 제거
        Destroy(gameObject);
    }

    public void SetDropItem(ItemType newType, int count)
    {
        type = newType;
        dropCount = count;
    }
}