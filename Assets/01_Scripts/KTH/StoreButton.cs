using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreButton : MonoBehaviour
{
    [SerializeField] private string sceneName = "StoreScene";

    public void OnClick()=>SceneManager.LoadScene(sceneName);
}
