using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// IDamageable 인터페이스 정의 및 상속 제거

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

    // === 자폭 연출 변수 ===
    public float blinkInterval = 0.2f;
    public float maxSuicideScale = 2.0f;
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

        // FSM 상태 전환
        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= calculatedMaxHP * RUN_AWAY_HP_PERCENT) state = EnemyState.RunAway;
                else if (dist < traceRange) state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (currentHP <= calculatedMaxHP * RUN_AWAY_HP_PERCENT) state = EnemyState.RunAway;
                // 자폭 범위 안에 들어오면 자폭 카운트다운 시작
                else if (dist < suicideRange) { state = EnemyState.Suicide; if (suicideCoroutine == null) StartSuicideCountdown(); }
                // 추적 및 이동
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

    // === 데미지 처리 (public 함수로 유지) ===
    // IDamageable이 없으므로, 다른 스크립트는 이 public 함수를 직접 호출해야 합니다.
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

    // === 이동 및 지면 부착 로직 ===

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

    // === 자폭 로직 ===

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

            // 크기 확대 연출
            float progress = elapsedTime / suicideDelay;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSuicideScale, progress);

            // 깜빡임 연출
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            // 플레이어에게 데미지 적용
            if (hitCollider.CompareTag("Player"))
            {
                // 기존: PlayerController playerScript = hitCollider.GetComponent<PlayerController>();
                // 💡 PlayerLightHealth 스크립트를 가져옵니다.
                PlayerLightHealth playerHealthScript = hitCollider.GetComponent<PlayerLightHealth>();

                if (playerHealthScript != null)
                {
                    // **데미지를 float 형식으로 전달합니다.**
                    // baseExplosionDamage 또는 calculatedDamage는 int이므로 (float)으로 형변환하여 전달합니다.
                    playerHealthScript.TakeDamage((float)calculatedDamage);
                    Debug.Log($"자폭병이 플레이어에게 {calculatedDamage} 데미지를 주었습니다.");
                }
            }
        }

        Die();
    }

    // DeadZone 처리
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("DeadZone")) Die();
    //}
}