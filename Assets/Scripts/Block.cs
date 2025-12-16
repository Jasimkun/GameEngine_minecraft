using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 [핵심] ItemType 열거형 정의 (Wood 포함)
public enum ItemType { Dirt, Grass, Water, Iron, Axe, Sword, Pickaxe, Wood, Stone }

public class Block : MonoBehaviour
{
    [Header("Block Visual Stats")]
    public string itemName;            // 블록 이름
    public Sprite itemIcon;            // 블록 아이콘 이미지
    public int maxStack = 99;          // 최대 겹침 개수

    [Header("Block Stat")]
    public ItemType type = ItemType.Dirt; // 이 블록을 캤을 때 드롭될 아이템의 타입
    public int maxHP = 3;
    [HideInInspector] public int hp;

    public int dropCount = 1; // 드롭될 아이템의 개수
    public bool mineable = true;

    [Header("Item Data")]
    public Block itemPrefab;


    private void Awake()
    {
        hp = maxHP;
        // 콜라이더 및 태그 설정
        if (GetComponent<Collider>() == null) gameObject.AddComponent<BoxCollider>();
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
            gameObject.tag = "Block";
    }

    // 🌲 [핵심 함수] 외부에서 블록의 드롭 아이템 정보 설정 (나무 생성 시 사용)
    public void SetDropItem(ItemType dropType, int amount)
    {
        this.type = dropType;
        this.dropCount = amount;
        this.itemName = dropType.ToString();
    }

    // 플레이어의 PlayerHarvester에서 호출되어 데미지를 받는 함수
    public void Hit(int damage, Inventory inven)
    {
        if (!mineable) return;
        hp -= damage;

        if (hp <= 0)
        {
            // 블록 파괴 및 아이템 드롭
            if (inven != null && dropCount > 0)
            {
                // 인벤토리에 설정된 type(드롭 아이템 타입)과 dropCount(개수)를 추가
                inven.Add(type, dropCount);
            }

            Destroy(gameObject);
        }
    }
}