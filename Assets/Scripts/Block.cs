using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Dirt, Grass, Water, Iron, Axe, Sword, Pickaxe }
public class Block : MonoBehaviour
{
    [Header("Block Visual Stats")]
    public string itemName;            //블록이름
    public Sprite itemIcon;            //블록 아이콘 이미지
    public int maxStack = 99;           //최대 겹침 개수

    [Header("Block Stat")]
    public ItemType type = ItemType.Dirt;
    public int maxHP = 3;
    [HideInInspector] public int hp;

    public int dropCount = 1;
    public bool mineable = true;

    [Header("Item Data")]
    public Block itemPrefab;


    private void Awake()
    {
        hp = maxHP;
        if (GetComponent<Collider>() == null) gameObject.AddComponent<BoxCollider>();
        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.tag == "Untagged")
            gameObject.tag = "Block";
    }

    public void Hit(int damage, Inventory inven)
    {
        if (!mineable) return;
        hp -= damage;
        if (hp <= 0)
        {
            if (inven != null && dropCount > 0)
                inven.Add(type, dropCount);

            Destroy(gameObject);
        }
    }
}