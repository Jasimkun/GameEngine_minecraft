using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [Header("드롭 설정")]
    public GameObject itemPrefab;
    public ItemType itemType;

    [Range(0, 100)]
    public float dropChance = 10f;  // 기본 확률

    public int minAmount = 1;
    public int maxAmount = 1;

    // 🔻 [추가] 치트키 감지를 위한 Update 함수
    void Update()
    {
        // 키보드 O키를 눌렀을 때 (GetKeyDown은 한 번 누를 때 한 번 실행)
        if (Input.GetKeyDown(KeyCode.O))
        {
            IncreaseDropChance(10f);
        }
    }

    // 🔻 [추가] 확률 증가 함수
    void IncreaseDropChance(float amount)
    {
        dropChance += amount;

        // 확률이 100%를 넘지 않도록 제한
        if (dropChance > 100f) dropChance = 100f;

        Debug.Log($"<color=yellow>[치트]</color> 아이템 드롭 확률이 상승했습니다! 현재 확률: {dropChance}%");
    }

    // 적이 죽는 순간 호출 (기존 로직 동일)
    public void TryDropLoot()
    {
        float randomValue = Random.Range(0f, 100f);

        if (randomValue <= dropChance)
        {
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        if (itemPrefab == null) return;

        int count = Random.Range(minAmount, maxAmount + 1);
        Vector3 spawnPos = transform.position + Vector3.up * 1.0f;

        GameObject droppedItem = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        ItemPickup pickupScript = droppedItem.GetComponent<ItemPickup>();
        if (pickupScript != null)
        {
            pickupScript.itemType = itemType;
            pickupScript.amount = count;
        }
    }
}