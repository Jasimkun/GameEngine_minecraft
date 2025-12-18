using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // AmbientMode 접근을 위해 추가

public class LightProjectile : MonoBehaviour
{
    [Header("🚀 속도 설정")]
    public float ascendSpeed = 40f;
    public float riseTime = 1.5f;

    [Header("연출 연결")]
    public Material daySkybox;
    public float transitionDuration = 3f;

    private Light mainSun;
    private Rigidbody rb;

    // 💾 기존 안개 거리 설정만 저장 (색깔은 저장 안 함 -> 낮에는 밝은 안개여야 하니까)
    private FogMode originalFogMode;
    private float originalFogStart;
    private float originalFogEnd;
    private bool originalFogEnabled;

    void Start()
    {
        // 1. 태양 찾기
        GameObject lightObj = GameObject.FindWithTag("MainLight");
        if (lightObj != null)
            mainSun = lightObj.GetComponent<Light>();

        // 2. 물리 끄기
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
        }

        // 3. 렉 방지용 안개 거리(Linear, 15~22)만 기억하기
        originalFogEnabled = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;

        // 4. 상승 시작
        Invoke("TheGreatAwakening", riseTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * ascendSpeed * Time.deltaTime);
    }

    void TheGreatAwakening()
    {
        StartCoroutine(EndingSequence());
    }

    IEnumerator EndingSequence()
    {
        float elapsed = 0f;

        // --- 1단계: 화이트 아웃 (White Out) ---

        // 안개 설정: 화면을 덮기 위한 임시 설정
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.white; // 안개를 하얗게!

        // 환경광(Ambient) 모드를 'Color'로 변경해야 색상 변경이 확실하게 먹힘
        RenderSettings.ambientMode = AmbientMode.Flat;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // 1. 안개 밀도 높이기 (화면 하얗게)
            RenderSettings.fogDensity = Mathf.Lerp(0f, 1.0f, t);

            // 2. ✨ 태양 빛: 하얗게 + 엄청 밝게(플래시 효과)
            if (mainSun != null)
            {
                mainSun.color = Color.Lerp(mainSun.color, Color.white, t); // 색상을 하얗게
                mainSun.intensity = Mathf.Lerp(1.0f, 8.0f, t); // 밝기 폭발
            }

            // 3. ✨ 환경광(Ambient): 검은색 -> 하얀색
            RenderSettings.ambientLight = Color.Lerp(Color.black, Color.white, t);

            yield return null;
        }

        // --- 2단계: 스카이박스 교체 ---
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            DynamicGI.UpdateEnvironment();
        }

        yield return new WaitForSeconds(0.5f);

        // --- 3단계: 안개 걷어내기 & 밝기 정착 ---
        elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // 안개 걷어내기
            RenderSettings.fogDensity = Mathf.Lerp(1.0f, 0f, t);

            // ✨ 태양 밝기: 8.0 -> 4.5 (요청하신 밝기)
            if (mainSun != null)
                mainSun.intensity = Mathf.Lerp(8.0f, 4.5f, t);

            yield return null;
        }

        // --- 4단계: 최종 상태 확정 (Day Mode) ---

        // 1. 렉 방지용 안개 거리 복구 (Linear, 15~22)
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        RenderSettings.fog = true; // 안개 켜기

        // 2. ✨ 안개 색깔은 '흰색'으로 유지! 
        // (낮이니까 멀리 있는 땅이 검은색이 아니라 하얗게 흐려져야 자연스러움)
        RenderSettings.fogColor = Color.white;
        // 만약 스카이박스와 더 잘 어울리게 하려면 하늘색(new Color(0.7f, 0.8f, 1f)) 추천

        // 3. ✨ 조명 최종 확정
        RenderSettings.ambientLight = Color.white; // 환경광 화이트 유지
        if (mainSun != null)
        {
            mainSun.color = Color.white; // 태양색 화이트
            mainSun.intensity = 4.5f;    // 강도 4.5 고정
        }

        if (WorldLightManager.Instance != null)
        {
            WorldLightManager.Instance.ConfirmPeace();
        }

        Debug.Log("🎉 눈부신 빛의 세상이 되었습니다! (Intensity: 4.5)");
        Destroy(gameObject);
    }

    // 강제 종료 시에는 다시 원래대로(검은 안개 등) 복구하고 싶다면 아래 코드 유지
    // 하지만 게임 내에서는 '밝은 세상'이 계속 유지되어야 하므로 OnDestroy에는 넣지 않음
    private void OnApplicationQuit()
    {
        // 끄고 나갈 때는 에디터 눈뽕 방지를 위해 적당히 복구
        RenderSettings.fogColor = Color.black;
    }
}