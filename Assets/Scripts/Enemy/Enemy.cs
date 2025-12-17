using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    // === 상태 열거형 (RunAway 삭제됨) ===
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
    // private const float RUN_AWAY_HP_PERCENT = 0.2f; // 삭제됨

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
                // 도망 로직 삭제: 바로 추적 거리 체크
                if (dist < traceRange) state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                // 도망 로직 삭제: 자폭 범위 or 계속 추적
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

                // case EnemyState.RunAway: 삭제됨
        }
    }

    public void TakeDamage(int damage, Vector3? attackerPos = null)
    {
        if (currentHP <= 0) return;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;
        if (hpSlider != null) hpSlider.value = currentHP;

        // 넉백 실행
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

        GetComponent<EnemyLoot>().TryDropLoot();

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

    // RunAwayFromPlayer() 함수 삭제됨

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
                                block.Hit(block.maxHP + 1);
                            }
                        }
                    }
                }
            }
        }
        Die();
    }
}