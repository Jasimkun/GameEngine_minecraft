using UnityEngine;
using System.Collections;

public class LavaDamage : MonoBehaviour
{
    [Header("데미지 설정")]
    public float lavaDamagePerTick = 2f;    // 1초당 2 데미지
    public float lavaInterval = 1f;        // 1초 간격

    [Header("용암 탈출 후(화상) 설정")]
    public float burnDuration = 3f;        // 3초 동안 지속
    public float burnInterval = 1f;        // 1초마다

    private Coroutine lavaCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerLightHealth health = other.GetComponent<PlayerLightHealth>();
            if (health != null)
            {
                // 용암 밖으로 나갔을 때를 대비해 실행 중이던 화상 코루틴이 있다면 멈춤
                // (기존 PlayerLightHealth에 구현된 dotCoroutine을 사용할 수도 있지만, 
                // 용암은 '안'과 '밖'의 로직이 명확히 구분되어야 하므로 여기서 직접 관리하는 것이 좋습니다.)

                // 용암 내부 데미지 시작
                if (lavaCoroutine != null) StopCoroutine(lavaCoroutine);
                lavaCoroutine = StartCoroutine(LavaTickRoutine(health));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerLightHealth health = other.GetComponent<PlayerLightHealth>();
            if (health != null)
            {
                // 용암 내부 데미지 중단
                if (lavaCoroutine != null) StopCoroutine(lavaCoroutine);

                // 용암에서 나왔으므로 3초간 지속 데미지 시작 (FireProjectile 방식 호출)
                health.StartDamageOverTime(lavaDamagePerTick, burnDuration, burnInterval);
            }
        }
    }

    // 용암 안에 있을 때 1초마다 반복해서 데미지를 주는 코루틴
    IEnumerator LavaTickRoutine(PlayerLightHealth health)
    {
        while (true)
        {
            health.TakeDamage(lavaDamagePerTick);
            yield return new WaitForSeconds(lavaInterval);
        }
    }
}