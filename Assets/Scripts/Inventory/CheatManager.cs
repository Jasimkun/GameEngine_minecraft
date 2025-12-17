using UnityEngine;

public class CheatManager : MonoBehaviour
{
    private Inventory inventory;

    void Start()
    {
        // 씬에서 인벤토리를 찾습니다.
        inventory = FindObjectOfType<Inventory>();

        if (inventory == null)
        {
            Debug.LogError("[Cheat] 씬에 Inventory 스크립트가 없습니다! 스크립트가 붙어있는지 확인하세요.");
        }
    }

    void Update()
    {
        // 상단 숫자 0 혹은 키패드 0 체크
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            if (inventory != null)
            {
                Debug.Log("[Cheat] 0키 누름 - 빛 조각 추가 시도");
                // ★ 주의: 여기서 ItemType.LightPiece가 실제 Enum 이름과 같은지 꼭 확인!
                inventory.Add(ItemType.LightPiece, 1);
            }
            else
            {
                // 실시간으로 다시 찾기 시도
                inventory = FindObjectOfType<Inventory>();
                if (inventory == null) Debug.LogError("[Cheat] 인벤토리를 찾을 수 없습니다.");
            }
        }
    }
}