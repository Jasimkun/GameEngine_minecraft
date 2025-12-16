using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, RunAway, Suicide }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float suicideRange = 3f;

    // === 자폭 및 경고 설정 ===
    public float suicideDelay = 3f;
    public float explosionRadius = 3f;
    public Color warningColor = Color.white;
    public int baseExplosionDamage = 10;

    // 💡 [추가] 블록 파괴 범위 설정 (3x3x3 큐브를 위해 반지름 1 설정)
    [Header("Block Destruction")]
    public int blockExplosionRadius = 1; // 1이면 3x3x3 범위 (중앙 포함)

    // === 자폭 연출 변수 ===
    public float blinkInterval = 0.2f;
    public float maxSuicideScale = 1.5f;
    private Vector3 originalScale;
    private Coroutine suicideCoroutine;
    private Coroutine blinkCoroutine;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 체력 설정 ===
    public int baseMaxHP = 10;
    public int currentHP;

    // === 최종 스탯 및 도주 기준 ===
    private int calculatedMaxHP;
    private int calculatedDamage;
    private const float RUN_AWAY_HP_PERCENT = 0.2f;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        calculatedMaxHP = baseMaxHP;
        calculatedDamage = baseExplosionDamage;
        currentHP = calculatedMaxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = calculatedMaxHP;
            hpSlider.value = currentHP;
        }

        enemyRenderer = GetComponentInChildren<Renderer>();
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        originalScale = transform.localScale;
    }

    void Update()
    {
        if (player == null) return;
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return;
        if (state == EnemyState.Suicide) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= calculatedMaxHP * RUN_AWAY_HP_PERCENT) state = EnemyState.RunAway;
                else if (dist < traceRange) state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (currentHP <= calculatedMaxHP * RUN_AWAY_HP_PERCENT) state = EnemyState.RunAway;
                else if (dist < suicideRange) { state = EnemyState.Suicide; if (suicideCoroutine == null) StartSuicideCountdown(); }
                else TracePlayer();
                break;

            case EnemyState.Suicide:
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();
                float runawayDistance = 15f;
                if (Vector3.Distance(player.position, transform.position) > runawayDistance) state = EnemyState.Idle;
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private IEnumerator BlinkEffect()
    {
        if (enemyRenderer == null) yield break;
        float blinkDuration = 0.1f;
        Color original = enemyRenderer.material.color;

        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);

        enemyRenderer.material.color = (state == EnemyState.Suicide) ? warningColor : original;
        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0;
        StopAllCoroutines();
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        Vector3 lookTarget = player.position; lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;
        float runSpeed = movespeed * 2f;

        Vector3 movement = new Vector3(runDirection.x, 0, runDirection.z) * runSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }
        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            return true;
        }
        return false;
    }

    void SnapToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
        }
    }

    private void StartSuicideCountdown()
    {
        suicideCoroutine = StartCoroutine(SuicideCountdown());
    }

    IEnumerator SuicideCountdown()
    {
        float elapsedTime = 0f;
        float blinkTimer = 0f;
        bool isBlinkOn = true;

        if (enemyRenderer != null && blinkCoroutine == null) enemyRenderer.material.color = warningColor;

        while (elapsedTime < suicideDelay)
        {
            elapsedTime += Time.deltaTime;
            blinkTimer += Time.deltaTime;

            float progress = elapsedTime / suicideDelay;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSuicideScale, progress);

            if (blinkTimer >= blinkInterval)
            {
                blinkTimer -= blinkInterval;
                isBlinkOn = !isBlinkOn;
                if (enemyRenderer != null && blinkCoroutine == null)
                {
                    enemyRenderer.material.color = isBlinkOn ? warningColor : originalColor;
                }
            }
            yield return null;
        }
        suicideCoroutine = null;
        Explode();
    }

    void Explode()
    {
        Vector3 explosionCenter = transform.position;

        // 1. 플레이어에게 데미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerLightHealth playerHealthScript = hitCollider.GetComponent<PlayerLightHealth>();
                if (playerHealthScript != null)
                {
                    playerHealthScript.TakeDamage((float)calculatedDamage);
                }
            }
        }

        // 2. 💡 [추가된 로직] 주변 블록 파괴
        Vector3Int centerPos = Vector3Int.RoundToInt(explosionCenter);
        int radius = blockExplosionRadius;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3 targetPos = centerPos + new Vector3Int(x, y, z);

                    // 해당 위치에 있는 블록을 찾음
                    // Physics.OverlapSphere 대신 Physics.OverlapBox를 사용하거나 
                    // Physics.OverlapSphere를 사용하되, 블록 레이어만 체크하는 것이 더 효율적일 수 있습니다.

                    // Simple Raycast/Overlap 대신, Voxel 맵 구조를 이용해 해당 좌표의 Collider를 검색합니다.
                    Collider[] blockCheck = Physics.OverlapBox(targetPos, Vector3.one * 0.45f, Quaternion.identity, LayerMask.GetMask("Block") != 0 ? LayerMask.GetMask("Block") : ~0);

                    foreach (Collider col in blockCheck)
                    {
                        // 태그로 한 번 더 확인하여 Block 컴포넌트를 가져옴
                        if (col.CompareTag("Block"))
                        {
                            Block block = col.GetComponent<Block>();
                            if (block != null)
                            {
                                // Block.Hit 함수를 사용하여 파괴 (인벤토리 추가 방지 위해 null 전달)
                                // 데미지는 블록을 한 번에 파괴할 수 있는 큰 값으로 설정
                                block.Hit(block.maxHP + 1, null);
                            }
                        }
                    }
                }
            }
        }

        Die();
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("DeadZone")) Die();
    //}
}