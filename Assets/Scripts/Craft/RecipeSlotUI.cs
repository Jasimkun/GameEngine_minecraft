using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text; // StringBuilder를 쓰기 위해 필요해요

public class RecipeSlotUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text materialsText; // 재료 텍스트 연결할 곳
    public Button selectButton;

    // 데이터 저장용
    private CraftingRecipe myRecipe;
    private CraftingPanel targetPanel;

    public void Setup(CraftingRecipe recipe, CraftingPanel panel)
    {
        myRecipe = recipe;
        targetPanel = panel;

        // 1. 이름 설정
        if (nameText != null)
            nameText.text = recipe.displayName;

        // 2. 아이콘 이미지 설정 (여기 주석 풀었습니다!)
        if (iconImage != null && recipe.icon != null)
        {
            iconImage.sprite = recipe.icon;

            // 혹시 이미지가 투명하게 나온다면 강제로 불투명하게 만듭니다.
            Color c = iconImage.color;
            c.a = 1f;
            iconImage.color = c;
        }

        // 3. 재료 텍스트 설정 (새로 추가된 부분!)
        if (materialsText != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var input in recipe.inputs)
            {
                // 예: "Wood x5 " 형태로 글자를 만듭니다.
                sb.Append($"{input.type} x{input.count}  ");
            }
            materialsText.text = sb.ToString();
        }

        // 4. 버튼 연결
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnClicked);
        }
    }

    void OnClicked()
    {
        if (targetPanel == null || myRecipe == null) return;

        targetPanel.ClearPlanned();

        foreach (var ingredient in myRecipe.inputs)
        {
            targetPanel.AddPlanned(ingredient.type, ingredient.count);
        }

        Debug.Log($"[RecipeUI] {myRecipe.displayName} 선택됨");
    }
}