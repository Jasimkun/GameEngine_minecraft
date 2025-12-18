using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireProjectile : MonoBehaviour
{
    // 🔻 [수정] 요청하신 데미지 값으로 설정 (Inspector에서도 수정 가능)
    public float baseInitialDamage = 3f; // 초기 충돌 데미지 3
    public float baseDotDamage = 2f;     // 틱당 지속 데미지 2

    public float dotDuration = 2f;       // 2초 동안 지속
    public float dotInterval = 0.5f;     // 0.5초마다 데미지 (총 4번 들어감)

    public float speed = 10f;
    public float lifeTime = 3f;

    private Vector3 moveDir;

    public void SetDirection(Vector3 dir)
    {
        moveDir = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += moveDir * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어와 충돌했을 때
        if (other.CompareTag("Player"))
        {
            // 🔻 [핵심 수정] 이제 PlayerLightHealth 스크립트를 찾습니다.
            PlayerLightHealth lightHealth = other.GetComponent<PlayerLightHealth>();

            if (lightHealth != null)
            {
                // 초기 데미지 (3)
                lightHealth.TakeDamage(baseInitialDamage);

                // 지속 데미지 (틱당 2)
                lightHealth.StartDamageOverTime(baseDotDamage, dotDuration, dotInterval);
            }

            Destroy(gameObject);
        }
        // 2. 적이 아닌 다른 오브젝트와 충돌
        else if (!other.CompareTag("Enemy"))
        {
            // 벽이나 바닥에 닿으면 그냥 사라짐
            Destroy(gameObject);
        }
    }
}