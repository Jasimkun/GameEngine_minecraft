using UnityEngine;
using UnityEngine.UI; // 슬라이더(UI)를 제어하기 위해 필수!

public class PlayerLightHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public float maxHealth = 100f;  // 최대 체력
    public float currentHealth;     // 현재 체력

    [Header("Light Settings")]
    public Light playerLight;       // 플레이어 주변을 밝히는 Point Light
    public float maxIntensity = 10f; // 체력이 꽉 찼을 때의 빛 밝기 (기본 10)

    [Header("UI Settings")]
    public Slider healthSlider;     // 빛 게이지 슬라이더

    void Start()
    {
        // 1. 게임 시작 시 체력 초기화
        currentHealth = maxHealth;

        // 2. 슬라이더 최대값 설정
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // 3. 빛 밝기 초기화
        UpdateLightVisuals();
    }

    // 외부(적, 함정 등)에서 이 함수를 호출해서 데미지를 줌
    public void TakeDamage(float damage)
    {
        // 체력 감소
        currentHealth -= damage;

        // 체력이 0 밑으로 내려가지 않게 고정
        if (currentHealth < 0) currentHealth = 0;

        // 시각 효과 업데이트 (빛 & 슬라이더)
        UpdateLightVisuals();

        // 사망 체크
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 힐링 기능 (필요할 것 같아서 추가!)
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateLightVisuals();
    }

    void UpdateLightVisuals()
    {
        // 핵심 로직: 현재 체력이 몇 퍼센트 남았는지 계산 (0.0 ~ 1.0)
        float healthRatio = currentHealth / maxHealth;

        // 1. Light Intensity 조절
        if (playerLight != null)
        {
            // 최대 밝기(10) * 남은비율
            // 예: 체력 50% 남음 -> 10 * 0.5 = 5 (밝기 반토막)
            playerLight.intensity = maxIntensity * healthRatio;
        }

        // 2. UI 슬라이더 조절
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    void Die()
    {
        Debug.Log("플레이어 사망! 빛이 소멸했습니다.");
        // 여기에 게임 오버 화면을 띄우거나 재시작 로직을 넣으면 됨

        // (선택사항) 사망 시 빛을 완전히 꺼버리기
        if (playerLight != null) playerLight.intensity = 0;
    }

    // 테스트용: 스페이스바를 누르면 데미지를 입음
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TakeDamage(10); // 10씩 데미지 테스트
        }
    }
}