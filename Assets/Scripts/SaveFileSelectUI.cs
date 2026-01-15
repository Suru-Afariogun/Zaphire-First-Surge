using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveFileSelectUI : MonoBehaviour {
    public void OnFile1Selected() {
        GameManager.Instance.selectedSaveFile = "Save File 1";
        SceneManager.LoadScene("Boss Select");
    }
     public void OnFile2Selected() {
        GameManager.Instance.selectedSaveFile = "Save File 2";
        SceneManager.LoadScene("Boss Select");
    }
     public void OnFile3Selected() {
        GameManager.Instance.selectedSaveFile = "Save File 3";
        SceneManager.LoadScene("Boss Select");
    }
}
