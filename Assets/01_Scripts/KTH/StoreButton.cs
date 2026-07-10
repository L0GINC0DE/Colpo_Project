using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "StoreScene";

    public void OnClick()
    {
        // 맵 씬을 나가기 전에 갱단 위치를 저장해둔다 - 안 그러면 스토어 갔다 돌아왔을 때
        // GangController.Start()가 세이브된 값을 못 찾아서 스폰 위치로 리셋돼버림.
        if (SaveManager.Instance != null)
            SaveManager.Instance.Save();

        SceneManager.LoadScene(sceneName);
    }
}
