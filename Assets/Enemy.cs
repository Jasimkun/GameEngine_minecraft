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

    // === 넉백 설정 (추가됨) ===
    [Header("Knockback Settings")]
    public float knockbackForce = 5f; // 밀려나는 힘
    public float knockbackDuration = 0.3f; // 밀려나는 시간 (스턴 시간)

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

        // 평소에는 물리 연산을 꺼둡니다 (직접 이동 제어 위함)
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;
        // 충돌은 하되 회전해서 넘어지지 않도록 설정
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

        // 넉백 중(isKinematic이 false일 때)에는 AI 이동 로직을 멈춥니다.
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

    // [수정됨] 데미지와 함께 때린 사람의 위치(attackerPos)를 받습니다.
    // 만약 위치를 모르면 null을 넣으세요.
    public void TakeDamage(int damage, Vector3? attackerPos = null)
    {
        if (currentHP <= 0) return;

        // 깜빡임 효과
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        // 체력 감소
        currentHP -= damage;
        if (hpSlider != null) hpSlider.value = currentHP;

        // 넉백 실행 (공격자가 있을 경우에만)
        if (attackerPos.HasValue && currentHP > 0)
        {
            StopCoroutine("KnockbackRoutine"); // 이미 넉백 중이라면 끊고 새로 시작
            StartCoroutine(KnockbackRoutine(attackerPos.Value));
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // [추가됨] 넉백 코루틴
    IEnumerator KnockbackRoutine(Vector3 attackerPos)
    {
        // 1. 밀려날 방향 계산 (내 위치 - 공격자 위치)
        Vector3 knockbackDir = (transform.position - attackerPos).normalized;

        // 2. 약간 위로 튀어 오르게 설정 (마인크래프트 느낌)
        knockbackDir += Vector3.up * 0.5f;
        knockbackDir.Normalize();

        // 3. 물리 엔진 활성화
        enemyRigidbody.isKinematic = false;
        enemyRigidbody.useGravity = true;
        enemyRigidbody.velocity = Vector3.zero; // 기존 속도 초기화

        // 4. 힘 가하기 (Impulse는 순간적인 힘)
        enemyRigidbody.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);

        // 5. 넉백 시간만큼 대기 (이 동안은 Update에서 이동 로직이 멈춤)
        yield return new WaitForSeconds(knockbackDuration);

        // 6. 물리 엔진 비활성화 및 상태 복구
        enemyRigidbody.velocity = Vector3.zero;
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        // 7. 공중에 떠있지 않도록 바닥으로 붙이기
        SnapToGround();
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
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, groundCheckDistance + 1f))
        {
            // 부드러운 이동을 위해 위치 보정 (넉백 직후 너무 딱딱하게 붙지 않도록)
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
                    Collider[] blockCheck = Physics.OverlapBox(targetPos, Vector3.one * 0.45f, Quaternion.identity, LayerMask.GetMask("Block") != 0 ? LayerMask.GetMask("Block") : ~0);

                    foreach (Collider col in blockCheck)
                    {
                        if (col.CompareTag("Block"))
                        {
                            Block block = col.GetComponent<Block>();
                            if (block != null)
                            {
                                block.Hit(block.maxHP + 1, null);
                            }
                        }
                    }
                }
            }
        }
        Die();
    }
}