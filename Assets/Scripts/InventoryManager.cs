using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Setting")]
    public int inventorySize = 20;      //인벤토리 슬롯 개수
    public GameObject inventoryUI;      //UI 패널
    public Transform itemSlotParanet;   //슬롯들이 들어갈 부모 오브젝트
    public GameObject itemSlotPrefab;   //슬롯 프리팹

    [Header("Input")]
    public KeyCode inventoryKey = KeyCode.I;                            //인벤토리 열기 키
    public List<InventorySlot> slots = new List<InventorySlot>();       //모든 슬롯 리스트
    private bool isInventoryOpen = false;                               //인벤토리가 열려있는지 확인

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    // Start is called before the first frame update
    void Start()
    {
        CreateInventorySlots();
        //inventoryUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(inventoryKey)) // 'I' 키 (인벤토리 열기)
        //{
            //ToggleInventory();
        //}

        if (Input.GetKeyDown(KeyCode.O))
        {
            ClearAllInventory();
        }
    }

    void CreateInventorySlots()         //인벤토리 슬롯들을 생성하는 함수
    {
        for(int i = 0; i < inventorySize; i++)
        {
            GameObject slotObject = Instantiate(itemSlotPrefab, itemSlotParanet);
            InventorySlot slot = slotObject.GetComponent<InventorySlot>();
            slots.Add(slot);            //리스트에 추가
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);

        //if(isInventoryOpen)
        //{
        //    Cursor.lockState = CursorLockMode.None;
        //    Cursor.visible = true;
        //}
        //else
        //{
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = false;
        //}
    }

    public bool AddItem(Block blockToAdd, int amount = 1)
    {
        // 1단계 : 스택
        foreach (InventorySlot slot in slots)
        {
            if (slot.block != null && slot.block.type == blockToAdd.type && slot.amount < blockToAdd.maxStack)
            {
                int spaceLeft = blockToAdd.maxStack - slot.amount; 
                int amountToAdd = Mathf.Min(amount, spaceLeft);
                slot.AddAmount(amountToAdd);

                amount -= amountToAdd;

                if (amount <= 0)
                {
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot.block == null)
            {
                slot.SetItem(blockToAdd, amount); 
                return true;
            }
        }

        Debug.Log("인벤토리가 가득 참");
        return false;
    }

    public void RemoveItem(Block item, int amount = 1)
    {
        foreach(InventorySlot slot in slots)
        {
            if(slot.block == item)
            {
                slot.RemoveAmount(amount);
                return;
            }
        }
    }

    public int GetItemCount(Block item)
    {
        int count = 0;
        foreach (InventorySlot slot in slots)
        {
            if(slot.block == item)
            {
                count += slot.amount;
            }
        }
        return count;
    }

    public void ClearAllInventory()
    {
        foreach (InventorySlot slot in slots)
        {
            slot.ClearSlot(); // 각 슬롯의 ClearSlot() 함수 호출
        }
        Debug.Log("인벤토리 전체가 초기화되었습니다.");
    }
}
