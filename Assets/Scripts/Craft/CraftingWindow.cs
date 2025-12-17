using UnityEngine;
using System.Collections.Generic;

public class CraftingWindow : MonoBehaviour
{
    [Header("연결할 것들")]
    public CraftingPanel craftingPanel;   // ★ 오른쪽 제작 패널 연결
    public Transform contentParent;       // ★ 스크롤뷰의 Content 오브젝트 연결
    public GameObject slotPrefab;         // ★ 아까 만든 슬롯 프리팹 연결

    [Header("데이터")]
    public List<CraftingRecipe> allRecipes; // 표시할 모든 레시피 목록

    void Start()
    {
        GenerateRecipeList();
    }

    void GenerateRecipeList()
    {
        // 1. 기존에 있던 목록 다 지우기 (중복 방지)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 레시피 개수만큼 슬롯 생성
        foreach (var recipe in allRecipes)
        {
            GameObject newSlot = Instantiate(slotPrefab, contentParent);

            // 3. 슬롯 스크립트 가져오기
            RecipeSlotUI slotUI = newSlot.GetComponent<RecipeSlotUI>();

            // 4. 슬롯에게 "너는 이 레시피 담당이고, 클릭되면 저 패널을 조종해"라고 명령
            slotUI.Setup(recipe, craftingPanel);
        }
    }
}