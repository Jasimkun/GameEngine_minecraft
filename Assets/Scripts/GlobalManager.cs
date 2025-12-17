using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager instance;

    private void Awake()
    {
        // 1. 이미 게임 매니저가 존재하면? (씬 이동 후 중복 생성 방지)
        if (instance != null)
        {
            Destroy(gameObject); // 새로 생긴 짝퉁은 파괴한다.
            return;
        }

        // 2. 내가 진짜다!
        instance = this;

        // 3. 나(그리고 내 자식들 전부)는 씬이 바뀔 때 파괴되지 않는다.
        DontDestroyOnLoad(gameObject);
    }
}