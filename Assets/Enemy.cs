using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Trace, Attack, RunAway }

    public EnemyState state = EnemyState.Idle;

    public float movespeed = 2f;      //이동 속도  

    public float traceRange = 15f;      //추적 시작 거리

    public float attackRange = 6f;      //공격 시작 거리

    public float attackCooldown = 1.5f;

    public GameObject projectilePrefab;     //투사체 프리팹

    public Transform firePoint;             //발사 위치

    private float lastAttackTime;
    public int maxHP = 5;

    public int currentHP;

    private Transform player;        //플레이어 추적용

    public Slider hpSlider;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        currentHP = maxHP;
        hpSlider.value = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        //FSM 상태 전환
        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < attackRange)
                    state = EnemyState.Attack;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist > attackRange)
                    state = EnemyState.Trace;

                else
                    AttackPlayer();
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();

                float runawayDistance = 15f; // 도망 완료 거리 기준

                if (Vector3.Distance(player.position, transform.position) > runawayDistance)
                    state = EnemyState.Idle;
                break;

        }

        //플레이어까지의 방향 구하기
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * movespeed * Time.deltaTime;
        transform.LookAt(player.position);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        hpSlider.value = (float)currentHP / maxHP;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    //적 제거
    void Die()
    {
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * movespeed * Time.deltaTime;
        transform.LookAt(player.position);
    }

    void AttackPlayer()
    {
        //일정 쿨다운마다 발사
        if (Time.deltaTime >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            transform.LookAt(player.position);
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                ep.SetDirection(dir);
            }    
        }
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection; // 반대 방향

        float runSpeed = movespeed * 2f;

        // 이동
        transform.position += runDirection * runSpeed * Time.deltaTime;

        // 적의 시선 방향을 이동 방향으로 설정
        transform.rotation = Quaternion.LookRotation(runDirection);
    }
}
