using UnityEngine;
using UnityEngine.UI;

public class PlayerLightHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Light Settings")]
    public Light playerLight;
    public float maxIntensity = 10f;

    [Header("UI Settings")]
    public Slider healthSlider;

    // 🔻 [추가] 지속 데미지 중복 방지용 코루틴 변수
    private Coroutine dotCoroutine;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        UpdateLightVisuals();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        UpdateLightVisuals();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 🔻 [추가] 지속 데미지(DoT) 시작 함수
    public void StartDamageOverTime(float damagePerTick, float duration, float interval)
    {
        // 이미 불타고 있다면 기존 불을 끄고 새로 붙임 (선택 사항)
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        dotCoroutine = StartCoroutine(DoTRoutine(damagePerTick, duration, interval));
    }

    // 🔻 [추가] 지속 데미지 코루틴
    System.Collections.IEnumerator DoTRoutine(float damage, float duration, float interval)
    {
        float timer = 0f;
        while (timer < duration && currentHealth > 0)
        {
            yield return new WaitForSeconds(interval);
            TakeDamage(damage); // 틱당 데미지 적용
            timer += interval;
        }
        dotCoroutine = null;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateLightVisuals();
    }

    void UpdateLightVisuals()
    {
        float healthRatio = currentHealth / maxHealth;

        if (playerLight != null)
        {
            playerLight.intensity = maxIntensity * healthRatio;
        }

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    void Die()
    {
        Debug.Log("플레이어 사망! 빛이 소멸했습니다.");
        if (playerLight != null) playerLight.intensity = 0;

        // 여기에 게임오버 UI나 씬 재시작 로직 추가
    }
}