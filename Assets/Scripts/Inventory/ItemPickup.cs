using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    // 🔻 [추가] 정적 변수로 딱 한 번만 실행되었는지 체크 (모든 ItemPickup이 이 변수를 공유함)
    private static bool hasShownFirstNotice = false;

    public ItemType itemType;
    public int amount = 1;

    [Header("자석 설정")]
    public float pickupRange = 3f;
    public float moveSpeed = 10f;
    public float pickupDelay = 0.5f;

    private Transform playerTransform;
    private Rigidbody rb;
    private float spawnTime;
    private bool isMagnetized = false;

    private void Start()
    {
        spawnTime = Time.time;
        rb = GetComponent<Rigidbody>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (Time.time < spawnTime + pickupDelay) return;
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= pickupRange)
        {
            isMagnetized = true;
        }

        if (isMagnetized)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

            if (distance < 0.5f)
            {
                GiveItemToPlayer();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isMagnetized) return;
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

            // 🔻 [추가] 빛 조각을 얻었을 때 최초 1회 공지 로직
            if (itemType == ItemType.LightPiece && !hasShownFirstNotice)
            {
                // 인벤토리 스크립트에 있는 ShowNotice 함수를 호출합니다.
                inventory.ShowNotice("화면을 클릭해 차원을 이동하세요");
                hasShownFirstNotice = true; // 이제 다음부터는 실행되지 않음
            }

            Destroy(gameObject);
        }
    }
}