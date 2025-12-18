using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 1. 이게 꼭 있어야 씬 이동을 감지합니다!

public class InventoryUI : MonoBehaviour
{
    // ... (기존 스프라이트 변수들은 그대로 유지) ...
    [Header("Sprites")]
    public Sprite dirtSprite;
    public Sprite grassSprite;
    public Sprite waterSprite;
    public Sprite ironSprite;
    public Sprite axeSprite;
    public Sprite swordSprite;
    public Sprite pickaxeSprite;
    public Sprite woodSprite;
    public Sprite stoneSprite;
    public Sprite lightPieceSprite;
    public Sprite netherrackSprite;
    public Sprite endStoneSprite;
    public Sprite stoneAxeSprite;
    public Sprite stonePickaxeSprite;
    public Sprite stoneSwordSprite;
    public Sprite lightSprite;

    [Header("UI References")]
    // 이 리스트가 씬 넘어갈 때 자꾸 연결이 끊겨서 문제입니다.
    public List<Transform> Slot = new List<Transform>();
    public GameObject SlotItem;
    List<GameObject> items = new List<GameObject>();

    public int selectedIndex = -1;

    // -------------------------------------------------------------
    // ✅ [추가] 씬이 바뀔 때마다 슬롯을 다시 연결하는 기능
    // -------------------------------------------------------------
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드되면 슬롯을 다시 찾습니다.
        FindSlots();
    }

    void FindSlots()
    {
        // 1. 기존 리스트가 엉망일 수 있으니 비웁니다.
        Slot.Clear();

        // 2. 현재 내 게임오브젝트(Canvas 혹은 Panel) 아래에 있는 슬롯들을 다 찾습니다.
        // (만약 InventoryUI 스크립트가 GlobalManager에 있고, UI가 그 자식이라면 아래 코드로 충분합니다)
        // 주의: 슬롯의 부모 오브젝트 이름이 "Grid"라고 가정하거나, 
        // 혹은 모든 자식 중에서 이름에 "Slot"이 들어가는 녀석을 찾습니다.

        // 방법 A: 내 자식들 중 'Image' 컴포넌트가 있고 이름이 'Slot'인 것들을 찾는다 (가장 안전)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            // 부모(나 자신)는 제외하고, 이름에 "Slot"이 포함된 녀석만 리스트에 추가
            // (하이어라키에서 슬롯들 이름이 Slot (1), Slot (2) 이런 식이어야 잘 작동합니다)
            if (child != transform && (child.name.Contains("Slot") || child.name.Contains("Cell")))
            {
                Slot.Add(child);
            }
        }

        Debug.Log($"[InventoryUI] 씬 이동 후 슬롯 {Slot.Count}개를 다시 연결했습니다.");
    }

    // -------------------------------------------------------------

    public void UpdateInventory(Inventory myInven)
    {
        // 방어 코드: 슬롯 리스트가 비어있으면 다시 찾기 시도
        if (Slot.Count == 0) FindSlots();

        // 1. 기존 아이템 삭제
        foreach (var slotItems in items)
        {
            if (slotItems != null) Destroy(slotItems);
        }
        items.Clear();

        // 2. 인벤토리 데이터 탐색
        int idx = 0;
        foreach (var item in myInven.items)
        {
            // 슬롯 개수보다 아이템이 많으면 에러나니 체크
            if (idx >= Slot.Count) break;

            // 슬롯이 살아있는지 확인
            if (Slot[idx] == null) continue;

            var go = Instantiate(SlotItem, Slot[idx].transform);
            go.transform.localPosition = Vector3.zero;
            SlotItemPrefab sItem = go.GetComponent<SlotItemPrefab>();
            items.Add(go);

            switch (item.Key)
            {
                case ItemType.Dirt: sItem.ItemSetting(dirtSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Grass: sItem.ItemSetting(grassSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Water: sItem.ItemSetting(waterSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Iron: sItem.ItemSetting(ironSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Axe: sItem.ItemSetting(axeSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Sword: sItem.ItemSetting(swordSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Pickaxe: sItem.ItemSetting(pickaxeSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Wood: sItem.ItemSetting(woodSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Stone: sItem.ItemSetting(stoneSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.LightPiece: sItem.ItemSetting(lightPieceSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Netherrack: sItem.ItemSetting(netherrackSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.EndStone: sItem.ItemSetting(endStoneSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.StoneAxe: sItem.ItemSetting(stoneAxeSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.StonePickaxe: sItem.ItemSetting(stonePickaxeSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.StoneSword: sItem.ItemSetting(stoneSwordSprite, "x" + item.Value.ToString(), item.Key); break;
                case ItemType.Light: sItem.ItemSetting(lightSprite, "x" + item.Value.ToString(), item.Key); break;
            }
            idx++;
        }
    }

    private void Update()
    {
        // Slot.Count 체크 추가
        for (int i = 0; i < Mathf.Min(9, Slot.Count); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SetSelectedIndex(i);
            }
        }
    }

    public void SetSelectedIndex(int idx)
    {
        ResetSelection();
        if (selectedIndex == idx)
        {
            selectedIndex = -1;
        }
        else
        {
            if (idx >= items.Count)
            {
                selectedIndex = -1;
            }
            else
            {
                SetSelection(idx);
                selectedIndex = idx;
            }
        }
    }

    public void ResetSelection()
    {
        // ✅ [수정] 방어 코드 추가 (오류 해결의 핵심)
        if (Slot == null) return;

        foreach (var slot in Slot)
        {
            // 슬롯이 살아있는지 확인
            if (slot != null)
            {
                var img = slot.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }
    }

    void SetSelection(int _idx)
    {
        // ✅ [수정] 인덱스 범위 및 null 체크
        if (Slot != null && _idx >= 0 && _idx < Slot.Count && Slot[_idx] != null)
        {
            var img = Slot[_idx].GetComponent<Image>();
            if (img != null) img.color = Color.yellow;
        }
    }

    public ItemType GetInventorySlot()
    {
        if (selectedIndex >= 0 && selectedIndex < items.Count && items[selectedIndex] != null)
            return items[selectedIndex].GetComponent<SlotItemPrefab>().blockType;
        return ItemType.Dirt; // 기본값 반환 (안전장치)
    }
}