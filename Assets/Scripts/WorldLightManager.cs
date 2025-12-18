using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이름을 알기 위해 필요

public class WorldLightManager : MonoBehaviour
{
    public static WorldLightManager Instance;

    [Header("상태 확인용")]
    public bool IsLightRestored = false; // 빛이 돌아왔는가? (평화 모드)

    // 이미 빛 조각을 얻은 씬의 이름을 적어두는 장부
    [SerializeField]
    private List<string> collectedScenes = new List<string>();

    private void Awake()
    {
        // 씬이 바뀌어도 파괴되지 않고 유지되도록 설정 (싱글톤)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ✨ 씬 이동해도 나(매니저)는 살아남는다!
        }
        else
        {
            Destroy(gameObject); // 이미 매니저가 있으면 나는 사라짐 (중복 방지)
            return;
        }
    }

    // 🌟 [기능 1] 적이 죽을 때 "드롭해도 돼?" 물어보는 함수
    public void TryDropLightPiece(Vector3 position, GameObject lightPiecePrefab)
    {
        // 1. 이미 세상이 평화로워졌으면 드롭 금지
        if (IsLightRestored) return;

        string currentScene = SceneManager.GetActiveScene().name;

        // 2. 이 씬 장부에 이름이 없으면? (아직 안 먹음)
        if (!collectedScenes.Contains(currentScene))
        {
            // 드롭 허가! 아이템 생성
            Instantiate(lightPiecePrefab, position, Quaternion.identity);

            // 장부에 기록 (이제 이 씬에서는 안 나옴)
            collectedScenes.Add(currentScene);
            Debug.Log($"[{currentScene}]에서 빛 조각 획득! (현재 모은 개수: {collectedScenes.Count}/3)");
        }
        else
        {
            // 이미 먹었음
            Debug.Log($"[{currentScene}] 이미 획득함. 드롭 패스!");
        }
    }

    // 🌟 [기능 2] 빛 발사체 연출이 끝나면 호출됨 -> 몬스터 전멸 & 평화 선포
    public void ConfirmPeace()
    {
        if (IsLightRestored) return;

        IsLightRestored = true; // 평화 모드 ON

        // 현재 맵에 있는 모든 적(Enemy 태그) 찾아서 삭제
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        Debug.Log("✨ 세상에 완전한 빛이 돌아왔습니다! 몬스터가 사라집니다.");
    }
}