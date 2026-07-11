using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "StoreScene";

    public void OnClick()
    {
        // 맵 씬을 나가기 전에 자동 저장 - 안 그러면 스토어 갔다 돌아왔을 때 갱단 위치/자금
        // 등이 리셋돼버린다. 다른 패널/씬 전환 진입점이 새로 생기면 거기서도 이렇게
        // SaveSystem.Instance.AutoSave(...)를 호출해주면 된다.
        if (LDY_SaveSystem.Instance != null)
            LDY_SaveSystem.Instance.AutoSave("패널 전환");

        SceneManager.LoadScene(sceneName);
    }
}
