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

    [TextArea(0, 15)]
    [SerializeField] private string[] desc;

    private int index = 0;
    
    private string lockedText;
    private int lockedLength;
    
    private void Start()
    {
        inputField.onSubmit.AddListener(OnEnter);
    }
    
    private void Awake()
    {
        lockedText = inputField.text;
        lockedLength = lockedText.Length;
        
        inputField.caretPosition = lockedLength;
        inputField.stringPosition = lockedLength;
        
        inputField.onValueChanged.AddListener(OnChanged);
        inputField.onSelect.AddListener(_ => MoveCaretToEnd());
    }

    private void Update()
    {
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
            inputField.text = "";
            lockedLength = 0;
            lockedText = inputField.text;
            StartCoroutine(ShowText(desc[index]));
            index++;
        }
    }

    private IEnumerator ShowText(string value)
    {
        inputField.text = "";

        foreach (var letter in value)
        {
            inputField.text += letter;
            yield return new WaitForSeconds(0.02f);
        }
    }
}
