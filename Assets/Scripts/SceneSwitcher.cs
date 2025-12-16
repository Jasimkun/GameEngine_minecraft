using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string scene1Name = "Nether";
    public string scene2Name = "End";
    public string scene3Name = "OverWorld";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            SceneManager.LoadScene(scene1Name);
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            SceneManager.LoadScene(scene2Name);
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            SceneManager.LoadScene(scene3Name);
        }
    }
}