using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [Header("드롭 설정")]
    public GameObject itemPrefab;   // 드롭할 아이템 프리팹 (위의 ItemPickup이 붙은 것)
    public ItemType itemType;       // 드롭할 아이템 종류 (예: LightFragment)

    [Range(0, 100)]
    public float dropChance = 10f;  // 확률 (10% 등)

    public int minAmount = 1;       // 최소 개수
    public int maxAmount = 1;       // 최대 개수

    // 적이 죽는 순간 이 함수를 호출하세요!
    public void TryDropLoot()
    {
        // 1. 확률 계산 (0~100 사이 랜덤 숫자가 확률보다 낮으면 당첨)
        float randomValue = Random.Range(0f, 100f);

        if (randomValue <= dropChance)
        {
            // 2. 당첨! 아이템 생성
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        if (itemPrefab == null) return;

        // 개수 랜덤 결정 (보통 1개겠지만)
        int count = Random.Range(minAmount, maxAmount + 1);

        // 아이템 생성 위치 (적의 위치보다 살짝 위)
        Vector3 spawnPos = transform.position + Vector3.up * 1.0f;

        GameObject droppedItem = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        // 생성된 아이템에 정보 입력
        ItemPickup pickupScript = droppedItem.GetComponent<ItemPickup>();
        if (pickupScript != null)
        {
            pickupScript.itemType = itemType;
            pickupScript.amount = count;
        }
    }
}