using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class CraftingPanel : MonoBehaviour
{
    public Inventory inventory;
    public List<CraftingRecipe> recipeList;
    public GameObject root;
    public TMP_Text plannedText;
    public Button craftButton;
    public Button clearButton;
    public TMP_Text hintText;

    public GameObject playerObject;

    // [추가] 플레이어의 움직임을 제어할 변수
    public MonoBehaviour playerController;

    readonly Dictionary<ItemType, int> planned = new();
    bool isOpen;

    void Start()
    {
        SetOpen(false);
        // 버튼 이벤트 연결 (Null 체크 추가)
        if (craftButton) craftButton.onClick.AddListener(DoCraft);
        if (clearButton) clearButton.onClick.AddListener(ClearPlanned);

        RefreshPlannedUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (root) root.SetActive(open);

        if (open)
        {
            // [켜짐]
            Time.timeScale = 0;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 플레이어 오브젝트의 모든 스크립트를 뒤져서 끕니다
            if (playerObject != null)
            {
                // MonoBehaviour를 상속받은 모든 스크립트를 가져옴
                MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    // 이 스크립트(CraftingPanel), 인벤토리, Transform 등은 끄면 안 됨
                    if (script != this && script.GetType() != typeof(Inventory))
                    {
                        script.enabled = false;
                    }
                }
            }
        }
        else
        {
            // [꺼짐]
            ClearPlanned();
            Time.timeScale = 1;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 다시 다 켜기
            if (playerObject != null)
            {
                MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    script.enabled = true;
                }
            }
        }
    }

    public void AddPlanned(ItemType type, int count = 1)
    {
        if (!planned.ContainsKey(type))
            planned[type] = 0;
        planned[type] += count;

        RefreshPlannedUI();
        SetHint($"{type} x{count} 추가 완료");
    }

    public void ClearPlanned()
    {
        planned.Clear();
        RefreshPlannedUI();
        SetHint("초기화 완료");
    }

    void RefreshPlannedUI()
    {
        if (!plannedText)
            return;

        if (planned.Count == 0)
        {
            plannedText.text = "원하는 레시피를 선택하세요.";
            return;
        }

        var sb = new StringBuilder();

        foreach (var item in planned)
            sb.AppendLine($"{item.Key} x{item.Value}");
        plannedText.text = sb.ToString();
    }

    void SetHint(string msg)
    {
        if (hintText)
            hintText.text = msg;
    }

    void DoCraft()
    {
        if (planned.Count == 0)
        {
            SetHint("재료가 부족합니다.");
            return;
        }

        //인벤 수량 체크
        foreach (var plannedItem in planned)
        {
            if (inventory.GetItemCount(plannedItem.Key) < plannedItem.Value)
            {
                SetHint($"{plannedItem.Key} 가 부족합니다.");
                return;
            }
        }

        var matchedProduct = FindMatch(planned);
        if (matchedProduct == null)
        {
            SetHint("알맞는 레시피가 없습니다.");
            return;
        }

        //재료 소모
        foreach (var itemforConsume in planned)
            inventory.Consume(itemforConsume.Key, itemforConsume.Value);

        //결과물 지급
        foreach (var p in matchedProduct.outputs)
            inventory.Add(p.type, p.count);

        ClearPlanned();

        SetHint($"조합 완료 : {matchedProduct.displayName}");
    }

    CraftingRecipe FindMatch(Dictionary<ItemType, int> planned)
    {
        foreach (var recipe in recipeList)
        {
            //필요한 재료를 충분히 갖췄는지
            bool ok = true;
            foreach (var ing in recipe.inputs)
            {
                if (!planned.TryGetValue(ing.type, out int have) || have != ing.count)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
                return recipe;
        }
        return null;
    }
}