using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    //public ItemData item;
    public Inventory inventory;
    public Block block;

    public int amount;

    [Header("UI Reference")]
    public Image itemIcon;                      //아이템 아이콘 이미지
    public Text amountText;                     //개수 텍스트
    public GameObject emptySlotImage;           //빈 슬롯 일 때 보여줄 이미지



    // Start is called before the first frame update
    void Start()
    {
        UpdateSlotUI();
    }

    public void SetItem(Block newBlock, int newAmount)        //슬롯에 아이템 설정
    {
        block = newBlock;
        amount = newAmount;
        UpdateSlotUI();
    }

    

    void UpdateSlotUI()
    {
        if(block != null)                        //아이템이 있으면
        {
            itemIcon.sprite = block.itemIcon;        //아이콘 표시
            itemIcon.enabled = true;

            amountText.text = amount > 1 ? amount.ToString() : "";      //개수가 1개보다 많으면 숫자 표시
            if(emptySlotImage != null)
            {
                emptySlotImage.SetActive(false);
            }
        }
        else
        {
            itemIcon.enabled = false;       //아이콘 숨기기
            amountText.text = "";           //텍스트 비우기

            if(emptySlotImage != null)
            {
                emptySlotImage.SetActive(true);     //빈 슬롯 이미지 표시
            }
        }
    }

    public void AddAmount(int value)        //아이템 개수 추가
    {
        amount += value;
        UpdateSlotUI();
    }

    public void RemoveAmount(int value)     //아이템 개수 제거
    {
        amount -= value;
        if(amount <= 0)
        {
            ClearSlot();
        }
        else
        {
            UpdateSlotUI();
        }
    }

    public void ClearSlot()     //슬롯을 비우는 함수
    {
        block = null;
        amount = 0;
        UpdateSlotUI();
    }

}
