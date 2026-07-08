using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TextManager : MonoBehaviour
{
    public TextMeshProUGUI text;//¶ß±â¿ë
    public GameObject _text;//²ô±â¿ë
    char[] letters = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 
        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
    char currentLetter;
    public TextMeshProUGUI scoretext;
    public TextMeshProUGUI timetext;
    [SerializeField] GameObject textManager;
    [SerializeField] int score = 10;
    [SerializeField] float time = 10;

    private void Start()
    {
        scoretext.text = "Score: " + score;
        timetext.text = "Time: " + time;
        NextQuestion();
    }

    void NextQuestion()
    {
        _text.SetActive(true);
        currentLetter = letters[Random.Range(0, letters.Length)];
        text.text = currentLetter.ToString();
    }
    private void Update()
    {
        time -= Time.deltaTime;
        timetext.text = "Time: " + time;
        if (time <= 0)
        {
            Debug.Log("°ÔÀÓ¿À¹ö");
            Time.timeScale = 0f;
            textManager.SetActive(false);
        }
        if (Keyboard.current == null)
            return;
        if (score <= 0)
        {
            Debug.Log("½Â¸®");
            Time.timeScale = 0f;
            textManager.SetActive(false);
            timetext.text = "Time: " + 0;
            timetext.text = "Time: " + 0;
        }

        foreach (char letter in letters)
        {
            Key key = (Key)System.Enum.Parse(typeof(Key), letter.ToString());

            if (Keyboard.current[key].wasPressedThisFrame)
            {
                if (letter == currentLetter)
                {
                    score -= 1;
                    scoretext.text = "Score: " + score;
                    StartCoroutine(WaitForNextQuestion());
                }
                else
                {
                    Debug.Log("Áü");
                    score = 10;
                    scoretext.text = "Score: " + score;
                }
                break;
            }
        }
    }

    IEnumerator WaitForNextQuestion()
    {
        _text.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        NextQuestion();
    }
}
