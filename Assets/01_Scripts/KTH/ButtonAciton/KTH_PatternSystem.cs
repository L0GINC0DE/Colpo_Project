using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KTH_PatternSystem : MonoBehaviour
{
    public static KTH_PatternSystem Instance;

    [Header("노드")]
    [SerializeField] private KTH_PatternNode[] nodes;

    [Header("라인")]
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Transform lineP;

    [Header("정답(ID 기준)")]
    [SerializeField] private List<int> answerPattern = new();

    private readonly List<int> currentPattern = new();
    private readonly List<LineRenderer> lines = new();

    private KTH_PatternNode lastNode;
    private bool isLocked = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (answerPattern.Count == 0)
            Debug.LogWarning("answerPattern이 비어 있습니다.");

        ShuffleNumbers();
    }

    private void ShuffleNumbers()
    {
        List<int> numbers = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        for (int i = 0; i < numbers.Count; i++)
        {
            int rand = Random.Range(i, numbers.Count);
            (numbers[i], numbers[rand]) = (numbers[rand], numbers[i]);
        }

        int count = Mathf.Min(nodes.Length, numbers.Count);

        for (int i = 0; i < count; i++)
        {
            nodes[i].SetNumber(numbers[i]);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) InputNumber(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) InputNumber(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) InputNumber(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) InputNumber(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) InputNumber(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) InputNumber(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) InputNumber(7);
        if (Input.GetKeyDown(KeyCode.Alpha8)) InputNumber(8);
        if (Input.GetKeyDown(KeyCode.Alpha9)) InputNumber(9);
    }

    public void InputNumber(int number)
    {
        if (isLocked)
            return;

        KTH_PatternNode node = nodes.FirstOrDefault(x => x.DisplayNumber == number);

        if (node == null)
            return;

        AddNode(node);
    }

    private void AddNode(KTH_PatternNode node)
    {
        if (isLocked)
            return;

        if (currentPattern.Contains(node.ID))
            return;

        currentPattern.Add(node.ID);

        if (lastNode != null)
        {
            CreateLine(lastNode.transform, node.transform);
        }

        lastNode = node;

        CheckPattern();
    }

    private void CheckPattern()
    {
        int index = currentPattern.Count - 1;

        if (answerPattern.Count == 0 || index >= answerPattern.Count)
        {
            Debug.LogWarning("정답 패턴 범위를 초과했습니다.");
            ClearPattern();
            return;
        }

        if (currentPattern[index] != answerPattern[index])
        {
            Debug.Log("실패");

            ClearPattern();
            ShuffleNumbers();

            return;
        }

        if (currentPattern.Count == answerPattern.Count)
        {
            Debug.Log("성공");
            isLocked = true;
        }
    }

    public void ClearPattern()
    {
        currentPattern.Clear();
        lastNode = null;
        isLocked = false;

        foreach (LineRenderer line in lines)
        {
            Destroy(line.gameObject);
        }

        lines.Clear();
    }

    private void CreateLine(Transform start, Transform end)
    {
        LineRenderer line = Instantiate(linePrefab,lineP);

        line.positionCount = 2;

        line.SetPosition(0, start.position);
        line.SetPosition(1, end.position);

        lines.Add(line);
    }
}