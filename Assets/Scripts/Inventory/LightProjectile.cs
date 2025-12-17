using UnityEngine;
using System.Collections;

public class LightProjectile : MonoBehaviour
{
    [Header("설정")]
    public float ascendSpeed = 3f;
    public Material daySkybox; // 여기에 SkyNoon 머티리얼을 드래그해서 넣으세요.
    public float transitionDuration = 5f;

    private Light mainSun;

    void Start()
    {
        // Directional Light 찾기
        mainSun = GameObject.FindWithTag("MainLight")?.GetComponent<Light>();

        // 4초 후 상승 정점 및 폭발 연출 시작
        Invoke("TheGreatAwakening", 4f);
    }

    void Update()
    {
        // 하늘로 천천히 상승
        transform.Translate(Vector3.up * ascendSpeed * Time.deltaTime);
    }

    void TheGreatAwakening()
    {
        StartCoroutine(EndingSequence());
    }

    IEnumerator EndingSequence()
    {
        float elapsed = 0f;

        // 1. 아주아주 밝아지는 단계 (White Out 효과 준비)
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // 해 강도 폭발적 증가 (아주 밝게!)
            if (mainSun != null)
                mainSun.intensity = Mathf.Lerp(0.1f, 8.0f, t);

            // 현재 스카이박스 노출도 최대치로 (화면을 하얗게 채움)
            RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(1f, 10f, t));

            // 환경광도 하얗게
            RenderSettings.ambientLight = Color.Lerp(Color.black, Color.white, t);

            yield return null;
        }

        // 2. 피크 시점에서 스카이박스 교체 (화면이 거의 하얄 때)
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
            DynamicGI.UpdateEnvironment(); // 조명 리얼타임 업데이트
        }

        // 3. 다시 자연스러운 낮의 밝기로 돌아오는 단계
        elapsed = 0f;
        float fadeOutDuration = 3f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            // 낮의 적정 수치로 복구
            if (mainSun != null)
                mainSun.intensity = Mathf.Lerp(8.0f, 1.2f, t);

            RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(10f, 1.0f, t));

            yield return null;
        }

        Debug.Log("세상의 빛이 완전히 되돌아왔습니다!");
        Destroy(gameObject);
    }
}