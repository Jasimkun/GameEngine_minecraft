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

    void Update()
    {
        // 🔻 [치트키] 숫자키 9을 눌렀을 때 (키패드 9 또는 상단 9 둘 다 작동하게 하려면 Alpha0 사용)
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
        {
            CheatHeal(10f);
        }
    }

    // 치트용 회복 함수
    private void CheatHeal(float amount)
    {
        Heal(amount);
        Debug.Log($"<color=cyan>[치트]</color> 체력을 {amount}만큼 회복했습니다. 현재 체력: {currentHealth}");
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
        Debug.Log("플레이어 사망! 게임을 종료합니다.");

        if (playerLight != null)
            playerLight.intensity = 0;

        // 게임 종료 실행
        QuitGame();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        // 유니티 에디터에서 플레이 모드를 중지시킴
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 빌드된 실제 게임 프로그램을 종료함
            Application.Quit();
#endif
    }
}