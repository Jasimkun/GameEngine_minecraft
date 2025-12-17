using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemType itemType;
    public int amount = 1;

    [Header("자석 설정")]
    public float pickupRange = 3f;  // 이 거리 안에 들어오면 끌려옴
    public float moveSpeed = 10f;   // 날아오는 속도
    public float pickupDelay = 0.5f; // 생성 직후 획득 방지 시간

    private Transform playerTransform;
    private Rigidbody rb;
    private float spawnTime;
    private bool isMagnetized = false; // 자석 모드 켜졌는지 여부

    private void Start()
    {
        spawnTime = Time.time;
        rb = GetComponent<Rigidbody>();

        // 플레이어 미리 찾아두기 (태그가 Player인 오브젝트)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        // 1. 아직 획득 대기 시간이면 무시
        if (Time.time < spawnTime + pickupDelay) return;
        if (playerTransform == null) return;

        // 2. 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 3. 자석 범위 안에 들어오면 날아오기 시작!
        if (distance <= pickupRange)
        {
            isMagnetized = true;
        }

        if (isMagnetized)
        {
            // 물리 효과 끄기 (땅에 비비지 않고 날아오게 함)
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;  // 물리 끄기
                rb.useGravity = false;  // 중력 끄기

                // (선택) 충돌체도 꺼서 벽 뚫고 오게 하려면:
                // GetComponent<Collider>().enabled = false; 
            }

            // 플레이어 쪽으로 이동
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

            // 아주 가까워지면(0.5m) 획득 처리
            if (distance < 0.5f)
            {
                GiveItemToPlayer();
            }
        }
    }

    // 혹시라도 몸으로 직접 부딪혔을 때를 위한 백업
    private void OnCollisionEnter(Collision collision)
    {
        if (isMagnetized) return; // 자석 모드일 땐 위에서 처리하므로 패스
        if (collision.gameObject.CompareTag("Player"))
        {
            GiveItemToPlayer();
        }
    }

    void GiveItemToPlayer()
    {
        Inventory inventory = playerTransform.GetComponent<Inventory>();
        if (inventory == null) inventory = playerTransform.GetComponentInParent<Inventory>();

        if (inventory != null)
        {
            inventory.Add(itemType, amount);
            // Debug.Log($"[Item] {itemType} 자석 획득!");
            Destroy(gameObject);
        }
    }
}