using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 텍스트 출력을 위해 필요합니다.

public class Inventory : MonoBehaviour
{
    public Dictionary<ItemType, int> items = new();

    [Header("UI 연결")]
    public InventoryUI invenUI;
    public TMP_Text noticeText; // 화면 중앙에 메시지를 띄울 텍스트 컴포넌트

    private Coroutine noticeCoroutine;

    void Start()
    {
        if (invenUI == null) invenUI = FindObjectOfType<InventoryUI>();

        // 시작할 때 메시지 텍스트는 비워둡니다.
        if (noticeText != null) noticeText.text = "";
    }

    public int GetItemCount(ItemType id)
    {
        items.TryGetValue(id, out var count);
        return count;
    }

    public void Add(ItemType type, int count = 1)
    {
        if (!items.ContainsKey(type)) items[type] = 0;
        items[type] += count;

        Debug.Log($"[Inventory] +{count} {type} (총 {items[type]}개)");

        // 빛 조각이 추가되었을 때만 자동 합체 체크
        if (type == ItemType.LightPiece)
        {
            TryCombineLight();
        }

        if (invenUI != null) invenUI.UpdateInventory(this);
    }

    public bool Consume(ItemType type, int count = 1)
    {
        if (!items.TryGetValue(type, out var have) || have < count) return false;

        items[type] = have - count;
        Debug.Log($"[Inventory] -{count} {type} (총 {items[type]}개)");

        if (items[type] == 0)
        {
            items.Remove(type);
            if (invenUI != null)
            {
                invenUI.selectedIndex = -1;
                invenUI.ResetSelection();
            }
        }

        if (invenUI != null) invenUI.UpdateInventory(this);
        return true;
    }

    // --- 빛 조각 자동 합체 로직 ---
    private void TryCombineLight()
    {
        int pieceCount = GetItemCount(ItemType.LightPiece);

        if (pieceCount >= 3)
        {
            // 1. 조각 소모
            if (Consume(ItemType.LightPiece, 3))
            {
                // 2. 완성된 빛 추가
                Add(ItemType.Light, 1);

                // 3. 화면 중앙 메시지 출력
                ShowNotice("빛 조각이 합쳐졌습니다!" + "화면을 우클릭해 빛을 퍼뜨리세요");
                Debug.Log("[Inventory] 빛 합체 성공!");
            }
        }
    }

    // --- 화면 메시지 출력 로직 ---
    public void ShowNotice(string message)
    {
        if (noticeText == null) return;

        // 이미 메시지가 떠 있는 경우 코루틴을 멈추고 새로 시작
        if (noticeCoroutine != null) StopCoroutine(noticeCoroutine);
        noticeCoroutine = StartCoroutine(NoticeRoutine(message));
    }

    private IEnumerator NoticeRoutine(string message)
    {
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);

        // 3초 동안 보여줌
        yield return new WaitForSecondsRealtime(3f);

        noticeText.text = "";
        noticeText.gameObject.SetActive(false);
        noticeCoroutine = null;
    }
}