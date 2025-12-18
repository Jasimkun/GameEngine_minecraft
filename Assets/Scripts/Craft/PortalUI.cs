using UnityEngine;
using UnityEngine.UI; // 버튼(Button)과 패널 제어를 위해 필요
using UnityEngine.SceneManagement;
using TMPro; // ✅ [중요] TextMeshPro를 쓰기 위해 추가!

public class PortalUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelObj; // 패널 전체 (껏다 켰다 할 거)

    public Button button1;
    public TMP_Text text1; // ✅ [변경] Text -> TMP_Text (버튼 1의 자식 텍스트)

    public Button button2;
    public TMP_Text text2; // ✅ [변경] Text -> TMP_Text (버튼 2의 자식 텍스트)

    // 이동 가능한 씬 이름 목록 (반드시 Build Settings에 등록된 이름과 같아야 함!)
    private string[] allScenes = { "Overworld", "Nether", "End" };

    private bool isOpen = false;

    void Start()
    {
        // 시작할 때 닫아두기
        ClosePortal();

        // 버튼에 클릭 리스너 연결
        // 람다식(Lambda)을 사용해 버튼 클릭 시 텍스트 내용을 전달
        button1.onClick.AddListener(() => OnClickButton(text1.text));
        button2.onClick.AddListener(() => OnClickButton(text2.text));
    }

    public void TogglePortal()
    {
        if (isOpen) ClosePortal();
        else OpenPortal();
    }

    public void OpenPortal()
    {
        isOpen = true;
        if (panelObj != null) panelObj.SetActive(true);

        // 마우스 커서 보이게 풀기 (FPS 모드 해제)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 현재 씬 이름 알아내기
        string currentScene = SceneManager.GetActiveScene().name;

        // 현재 씬을 제외한 나머지 씬들을 버튼에 할당
        int btnIndex = 0;
        foreach (string sceneName in allScenes)
        {
            if (sceneName == currentScene) continue; // 내 씬은 패스

            if (btnIndex == 0)
            {
                if (text1 != null) text1.text = sceneName;
                btnIndex++;
            }
            else if (btnIndex == 1)
            {
                if (text2 != null) text2.text = sceneName;
                btnIndex++;
            }
        }
    }

    public void ClosePortal()
    {
        isOpen = false;
        if (panelObj != null) panelObj.SetActive(false);

        // 다시 마우스 커서 잠그기 (게임 플레이로 복귀)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 버튼 눌렀을 때 실행되는 함수
    void OnClickButton(string sceneName)
    {
        Debug.Log($"[Portal] {sceneName}으로 이동합니다...");

        // 씬 이름이 비어있으면 이동 안 함
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneManager.LoadScene(sceneName);

        // 씬 로드 후 처리는 씬이 바뀌면서 UI가 사라지므로 신경 안 써도 됨
        ClosePortal();
    }
}