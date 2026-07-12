using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DLJ_Store : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private string storeID;

    [SerializeField] private string[] skillName;
    
    [TextArea(0, 15)]
    [SerializeField] private string[] desc;

    [SerializeField] private TextMeshProUGUI currentMoney;

    private int money = 100;
    private int index = 0;

    private int skills = 0;
    
    private string lockedText;
    private int lockedLength;
    private Coroutine showTextCoroutine;

    private bool onMain = true;
    
    private void Awake()
    {
        lockedText = inputField.text;
        lockedLength = lockedText.Length;
        
        inputField.caretPosition = lockedLength;
        inputField.stringPosition = lockedLength;
        
        inputField.onValueChanged.AddListener(OnChanged);
        inputField.onSubmit.AddListener(OnEnter);
        inputField.onSelect.AddListener(_ => MoveCaretToEnd());

        //money = LSO_MoneyManager.Instance.Money;
    }

    private void Update()
    {
        currentMoney.text = money.ToString();
        if (inputField.isFocused && inputField.caretPosition < lockedLength)
            MoveCaretToEnd();
        
        if (inputField.selectionStringAnchorPosition < lockedLength || inputField.selectionStringFocusPosition < lockedLength)
            MoveCaretToEnd();
    }

    private void MoveCaretToEnd()
    {
        int pos = Mathf.Max(lockedLength, inputField.text.Length);
        inputField.caretPosition = pos;
        inputField.stringPosition = pos;
        inputField.selectionStringAnchorPosition = pos;
        inputField.selectionStringFocusPosition = pos;
    }

    private void OnChanged(string value)
    {
        if (!value.StartsWith(lockedText))
        {
            string type = value.Length > lockedLength ? value.Substring(Mathf.Min(value.Length, lockedLength)) : "";
            inputField.text = lockedText + type;
            MoveCaretToEnd();
        }
    }

    private void OnEnter(string arg0)
    {
        string command = arg0.Substring(lockedLength).Trim();
        Store(command);
    }

    private void Store(string value)
    {
        if (string.Equals(value, storeID, StringComparison.OrdinalIgnoreCase))
        {
            lockedLength = 0;
            lockedText = "";
            inputField.SetTextWithoutNotify("");

            if (showTextCoroutine != null)
                StopCoroutine(showTextCoroutine);

            index++;
            showTextCoroutine = StartCoroutine(ShowText(desc[index]));
            onMain = false;
        }

        foreach (string skill in skillName)
        {
            if (string.Equals(skill, value, StringComparison.OrdinalIgnoreCase) && !onMain)
            {
                skills++;
                Debug.Log(skills);
                inputField.SetTextWithoutNotify(lockedText);
            }
        }

        if (string.Equals(value, "Leave", StringComparison.OrdinalIgnoreCase) && !onMain)
        {
            onMain = true;
            index = 0;
            StartCoroutine(ShowText(desc[index]));
        }
        
        StartCoroutine(SelectIF());
    }

    private IEnumerator ShowText(string value)
    {
        inputField.readOnly = true;
        inputField.SetTextWithoutNotify("");

        foreach (var letter in value)
        {
            inputField.SetTextWithoutNotify(inputField.text + letter);
            yield return new WaitForSeconds(0.01f);
        }

        lockedText = inputField.text;
        lockedLength = lockedText.Length;
        MoveCaretToEnd();
        inputField.readOnly = false;
        StartCoroutine(SelectIF());
        showTextCoroutine = null;
    }

    private IEnumerator SelectIF()
    {
        yield return null;
        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);
        inputField.Select();
        inputField.ActivateInputField();
        MoveCaretToEnd();
    }
}
