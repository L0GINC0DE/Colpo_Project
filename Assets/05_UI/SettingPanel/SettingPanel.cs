using UnityEngine;
using UnityEngine.InputSystem;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isActive = !settingPanel.activeSelf;
            settingPanel.SetActive(isActive);

            Time.timeScale = isActive ? 0f : 1f;
        }
    }
}