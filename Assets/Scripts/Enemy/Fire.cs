using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 인터페이스 정의
public interface IDamageable
{
    void TakeDamage(int damage);
}

public class Fire : MonoBehaviour, IDamageable
{
    public enum EnemyState { Idle, Trace, Attack }
    public EnemyState state = EnemyState.Idle;

    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;

    public float attackCooldown = 5.0f;
    public GameObject fireProjectilePrefab;
    public Transform firePoint;
    private float lastAttackTime;

    public int maxHP = 10;
    public int currentHP;
    public int experienceValue = 5;

    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;

    private Coroutine blinkCoroutine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("Fire 몬스터가 Renderer를 찾지 못했습니다!", this.gameObject);
        }

        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null) { enemyRigidbody = gameObject.AddComponent<Rigidbody>(); }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        // 회전 제약 설정 (물리적으로도 넘어지지 않게)
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (player == null) return;
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange) state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (dist < attackRange) state = EnemyState.Attack;
                else if (dist < traceRange) TracePlayer();
                else state = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                if (dist > attackRange) state = EnemyState.Trace;
                else AttackPlayer();
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
        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);
        enemyRenderer.material.color = originalColor;
        blinkCoroutine = null;
    }

    void Die()
    {
        // 🌟 여기에 확률적으로 '빛 조각'을 드랍하는 코드를 추가하세요.
        Destroy(gameObject);
    }

    // 🔻 [핵심 수정] Y축 높이를 무시하고 바라보는 함수 추가
    void LookAtPlayerFlat()
    {
        if (player == null) return;

        // 플레이어의 위치를 가져오되, 높이(Y)는 내 높이와 똑같이 맞춤
        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);

        // 수정된 위치를 바라봄 (이제 기울지 않음)
        transform.LookAt(targetPos);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;

        // [수정] 기울어지지 않게 바라보기
        LookAtPlayerFlat();
    }

    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootFireProjectile();
        }

        // [수정] 공격 중에도 기울어지지 않게 바라보기
        LookAtPlayerFlat();
    }

    void ShootFireProjectile()
    {
        if (fireProjectilePrefab != null && firePoint != null)
        {
            // [수정] 발사 순간에도 정면 바라보기
            LookAtPlayerFlat();

            // 총알 생성
            GameObject proj = Instantiate(fireProjectilePrefab, firePoint.position, firePoint.rotation);
            FireProjectile fp = proj.GetComponent<FireProjectile>();

            if (fp != null)
            {
                // 🌟 중요: 몸은 정면을 보지만, 총알은 플레이어 쪽으로 날아가야 함
                Vector3 dir = (player.position - firePoint.position).normalized;
                fp.SetDirection(dir);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
            return;
        }

        // 플레이어 투사체 충돌 처리 (필요시 주석 해제)
        /*
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            TakeDamage(1); 
            Destroy(other.gameObject);
        }
        */
    }
}