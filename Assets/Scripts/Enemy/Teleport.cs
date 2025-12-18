using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Teleport : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, Teleporting }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 1.5f;

    // === 순간이동 설정 ===
    public float teleportCooldown = 5.0f;
    public float teleportDistance = 3.0f;
    public int maxTeleportAttempts = 10;
    private float lastTeleportTime;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.5f; // 조금 넉넉하게
    public float groundOffset = 0.0f; // 필요에 따라 조절 (0.5f 등)

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public int baseAttackDamage = 3;
    private float lastAttackTime;

    // === 체력 설정 ===
    public int baseMaxHP = 10;
    public int currentHP;
    public int experienceValue = 5;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;
    private Collider enemyCollider;
    private Coroutine blinkCoroutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        lastTeleportTime = Time.time;

        currentHP = baseMaxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = baseMaxHP;
            hpSlider.value = currentHP;
        }

        enemyRenderer = GetComponentInChildren<Renderer>(true);
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null) { enemyRigidbody = gameObject.AddComponent<Rigidbody>(); }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        enemyCollider = GetComponent<Collider>();

        // 🌟 [핵심] 플레이어와 물리적 충돌 무시 (밀침 방지)
        if (player != null && enemyCollider != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null) playerCollider = player.GetComponent<CharacterController>();

            if (playerCollider != null)
            {
                Physics.IgnoreCollision(playerCollider, enemyCollider, true);
            }
        }

        StartCoroutine(CheckForTeleport());
    }

    void Update()
    {
        if (player == null) return;
        if (enemyRigidbody.useGravity) return; // 떨어지는 중이면 로직 중지
        if (state == EnemyState.Teleporting) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange) state = EnemyState.Trace;
                break;
            case EnemyState.Trace:
                TryFallCheck();
                if (dist < attackRange) state = EnemyState.Attack;
                else if (dist > traceRange) state = EnemyState.Idle;
                else TracePlayer();
                break;
            case EnemyState.Attack:
                TryFallCheck();
                if (dist > attackRange) state = EnemyState.Trace;
                else AttackPlayer();
                break;
            case EnemyState.Teleporting:
                break;
        }
    }

    IEnumerator CheckForTeleport()
    {
        while (true)
        {
            yield return new WaitForSeconds(teleportCooldown);

            if (player != null && state != EnemyState.Teleporting && currentHP > 0)
            {
                float dist = Vector3.Distance(player.position, transform.position);
                // 추적 범위 밖이거나, 멀리 있을 때 접근용
                if (dist > traceRange || dist > 5f)
                {
                    TeleportToPlayerSide();
                }
            }
        }
    }

    void TeleportToPlayerSide()
    {
        EnemyState previousState = state;
        state = EnemyState.Teleporting;

        // [이펙트 삭제됨]

        Vector3 targetPosition = Vector3.zero;
        bool foundGround = false;

        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            Vector3 randomCircle = Random.insideUnitCircle.normalized * teleportDistance;
            Vector3 potentialPosition = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // 해당 위치의 높이를 플레이어 높이 기준으로 잡고 땅 체크
            potentialPosition.y = player.position.y;

            if (CheckGround(potentialPosition))
            {
                targetPosition = potentialPosition;
                foundGround = true;
                break;
            }
        }

        if (foundGround)
        {
            transform.position = targetPosition;
            SnapToGround();
        }
        else
        {
            // 땅 못 찾으면 플레이어 위로 이동 후 떨어짐
            transform.position = player.position + Vector3.up * 1.0f;
            Fall();
        }

        lastTeleportTime = Time.time;
        state = previousState;
    }

    void Fall()
    {
        enemyRigidbody.isKinematic = false;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        enemyRigidbody.useGravity = true;
        state = EnemyState.Idle;
    }

    void TryFallCheck()
    {
        if (!CheckGround(transform.position))
        {
            Fall();
        }
        else
        {
            SnapToGround();
        }
    }

    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        // 🌟 [수정됨] VoxelCollapse 의존성 제거: 단순히 아래에 무언가(Collider)가 있는지 확인
        if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out hit, groundCheckDistance))
        {
            // 플레이어나 자기 자신, 혹은 트리거가 아닌지 확인하면 더 좋음
            if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
            {
                return true;
            }
        }
        return false;
    }

    void SnapToGround()
    {
        if (!enemyRigidbody.isKinematic)
        {
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.useGravity = false;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        RaycastHit hit;
        // 🌟 [수정됨] VoxelCollapse 제거: Raycast가 맞은 지점(hit.point)을 바로 사용
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, groundCheckDistance + 1f))
        {
            if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
    }

    // === IDamageable 구현 ===
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
        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);
        enemyRenderer.material.color = originalColor;
        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0;
        StopAllCoroutines();

        // 🌟 아이템 드랍 (EnemyLoot)
        EnemyLoot loot = GetComponent<EnemyLoot>();
        if (loot != null) loot.TryDropLoot();

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

        // 🌟 기울지 않고 바라보기
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void AttackPlayer()
    {
        SnapToGround();

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            // 🌟 PlayerLightHealth 사용 (이펙트 제거됨)
            PlayerLightHealth playerHealth = player.GetComponent<PlayerLightHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(baseAttackDamage);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
        }
    }
}