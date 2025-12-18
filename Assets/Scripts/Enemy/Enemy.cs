using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// IDamageable 인터페이스 구현
public class Enemy : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Suicide }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float suicideRange = 3f;

    // === 넉백 설정 ===
    [Header("Knockback Settings")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    // === 자폭 및 경고 설정 ===
    public float suicideDelay = 3f;
    public float explosionRadius = 3f;
    public Color warningColor = Color.white;
    public int baseExplosionDamage = 10;

    [Header("Block Destruction")]
    public int blockExplosionRadius = 1;

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

    private int calculatedMaxHP;
    private int calculatedDamage;

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
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

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
                if (dist < traceRange) state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (dist < suicideRange)
                {
                    state = EnemyState.Suicide;
                    if (suicideCoroutine == null) StartSuicideCountdown();
                }
                else
                {
                    TracePlayer();
                }
                break;
            case EnemyState.Suicide:
                break;
        }
    }

    // 1. 인터페이스용 단순 피격 함수
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null);
    }

    // 2. 넉백을 포함한 피격 함수
    public void TakeDamage(int damage, Vector3? attackerPos = null)
    {
        if (currentHP <= 0) return;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;
        if (hpSlider != null) hpSlider.value = currentHP;

        if (attackerPos.HasValue && currentHP > 0)
        {
            StopCoroutine("KnockbackRoutine");
            StartCoroutine(KnockbackRoutine(attackerPos.Value));
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    IEnumerator KnockbackRoutine(Vector3 attackerPos)
    {
        Vector3 knockbackDir = (transform.position - attackerPos).normalized;
        knockbackDir += Vector3.up * 0.5f;
        knockbackDir.Normalize();

        enemyRigidbody.isKinematic = false;
        enemyRigidbody.useGravity = true;
        enemyRigidbody.velocity = Vector3.zero;

        enemyRigidbody.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        enemyRigidbody.velocity = Vector3.zero;
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        SnapToGround();
    }

    private IEnumerator BlinkEffect()
    {
        if (enemyRenderer == null) yield break;
        float blinkDuration = 0.1f;

        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);

        enemyRenderer.material.color = (state == EnemyState.Suicide) ? warningColor : originalColor;
        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0;
        StopAllCoroutines();

        // 1. EnemyLoot 컴포넌트 가져오기 (여기에 빛 조각 정보가 들어있음)
        EnemyLoot loot = GetComponent<EnemyLoot>();

        if (loot != null)
        {
            // 2. 확률 계산 (EnemyLoot에 설정된 dropChance 사용)
            // 0~100 사이 랜덤 숫자가 확률보다 낮으면 당첨
            float randomValue = Random.Range(0f, 100f);

            if (randomValue <= loot.dropChance)
            {
                // 3. 당첨되면 매니저에게 "이거 떨궈도 돼?" 하고 물어봄
                if (WorldLightManager.Instance != null && loot.itemPrefab != null)
                {
                    // loot.itemPrefab은 EnemyLoot에 연결해둔 '빛 조각 프리팹'입니다.
                    WorldLightManager.Instance.TryDropLightPiece(transform.position, loot.itemPrefab);
                }
            }
        }
        else
        {
            // 혹시 EnemyLoot가 없는 몬스터라면 그냥 경고 없이 넘어감
        }

        // 4. 적 삭제
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

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
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
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, groundCheckDistance + 1f))
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

        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // 플레이어 체력(빛) 깎기
                PlayerLightHealth playerHealthScript = hitCollider.GetComponent<PlayerLightHealth>();
                if (playerHealthScript != null)
                {
                    playerHealthScript.TakeDamage((float)calculatedDamage);
                }
            }
        }

        Vector3Int centerPos = Vector3Int.RoundToInt(explosionCenter);
        int radius = blockExplosionRadius;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3 targetPos = centerPos + new Vector3Int(x, y, z);
                    int layerMask = LayerMask.GetMask("Block") != 0 ? LayerMask.GetMask("Block") : ~0;

                    Collider[] blockCheck = Physics.OverlapBox(targetPos, Vector3.one * 0.45f, Quaternion.identity, layerMask);

                    foreach (Collider col in blockCheck)
                    {
                        Block block = col.GetComponent<Block>();
                        if (block != null)
                        {
                            block.Hit(block.maxHP + 1);
                        }
                    }
                }
            }
        }

        Die();
    }
}