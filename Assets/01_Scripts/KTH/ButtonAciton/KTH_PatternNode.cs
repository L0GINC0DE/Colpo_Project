using TMPro;
using UnityEngine;

public class KTH_PatternNode : MonoBehaviour
{
    [Header("고정 ID")]
    public int ID;

    [Header("현재 표시되는 숫자")]
    public int DisplayNumber;

    private TextMeshPro text;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshPro>();

        if (text == null)
            Debug.LogError($"{name} : TextMeshPro를 찾을 수 없습니다.");
    }

    public void SetNumber(int number)
    {
        DisplayNumber = number;

        if (text != null)
            text.text = number.ToString();
    }
}