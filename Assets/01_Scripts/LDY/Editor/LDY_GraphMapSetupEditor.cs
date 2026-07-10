using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(LDY_GraphMapSetup))]
public class LDY_GraphMapSetupEditor : Editor
{
    private enum EditMode { Select, AddNode, Connect }

    private EditMode mode = EditMode.Select;
    private int selectedIndex = -1;
    private int connectFirstIndex = -1;

    private static readonly Color BaseColor = Color.cyan;
    private static readonly Color NormalColor = Color.white;
    private static readonly Color SelectedColor = Color.yellow;
    private static readonly Color EdgeColor = new Color(1f, 1f, 1f, 0.5f);

    private const float NodeSize = 0.3f;

    public override void OnInspectorGUI()
    {
        var setup = (LDY_GraphMapSetup)target;

        EditorGUILayout.HelpBox(
            "맵은 Scene 뷰에서 편집합니다.\n" +
            "- Select: 노드 클릭 선택 후 드래그로 이동\n" +
            "- Add Node: 빈 곳 클릭 시 노드 생성\n" +
            "- Connect: 노드 두 개를 순서대로 클릭해서 연결/해제\n" +
            "- 노드 선택 후 Delete/Backspace 키로 삭제",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("편집 모드", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        DrawModeButton(EditMode.Select, "Select");
        DrawModeButton(EditMode.AddNode, "Add Node");
        DrawModeButton(EditMode.Connect, "Connect");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"노드 개수: {setup.NodeList.Count}");

        if (selectedIndex >= 0 && selectedIndex < setup.NodeList.Count)
            DrawSelectedNodeInspector(setup);

        SceneView.RepaintAll();
    }

    private void DrawModeButton(EditMode target, string label)
    {
        bool isCurrent = mode == target;
        GUI.backgroundColor = isCurrent ? Color.green : Color.white;
        if (GUILayout.Button(label))
        {
            mode = target;
            connectFirstIndex = -1;
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawSelectedNodeInspector(LDY_GraphMapSetup setup)
    {
        LDY_MapNode node = setup.NodeList[selectedIndex];

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("선택된 노드", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string newId = EditorGUILayout.TextField("id", node.id);
        bool newIsBase = EditorGUILayout.Toggle("isPlayerBase", node.isPlayerBase);
        Vector2 newPos = EditorGUILayout.Vector2Field("position", node.position);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(setup, "Edit Node");

            if (newId != node.id)
                RenameNode(setup.NodeList, node, newId);

            node.isPlayerBase = newIsBase;
            node.position = newPos;
            MarkDirty(setup);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("연결된 노드", EditorStyles.boldLabel);
        for (int i = node.connectedNodeIds.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(node.connectedNodeIds[i]);
            if (GUILayout.Button("연결 해제", GUILayout.Width(80)))
            {
                Undo.RecordObject(setup, "Disconnect Node");
                string otherId = node.connectedNodeIds[i];
                LDY_MapNode other = setup.NodeList.Find(n => n.id == otherId);
                node.connectedNodeIds.RemoveAt(i);
                other?.connectedNodeIds.Remove(node.id);
                MarkDirty(setup);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("노드 삭제"))
        {
            Undo.RecordObject(setup, "Delete Node");
            RemoveNode(setup.NodeList, selectedIndex);
            selectedIndex = -1;
            MarkDirty(setup);
        }
    }

    private void OnSceneGUI()
    {
        var setup = (LDY_GraphMapSetup)target;
        List<LDY_MapNode> nodeList = setup.NodeList;

        DrawEdges(nodeList);
        HandleNodeButtons(setup, nodeList);
        HandleAddNodeClick(setup, nodeList);
        HandleDeleteKey(setup, nodeList);
    }

    private void DrawEdges(List<LDY_MapNode> nodeList)
    {
        var drawn = new HashSet<string>();
        Handles.color = EdgeColor;

        foreach (LDY_MapNode node in nodeList)
        {
            foreach (string neighborId in node.connectedNodeIds)
            {
                LDY_MapNode neighbor = nodeList.Find(n => n.id == neighborId);
                if (neighbor == null)
                    continue;

                string key = string.CompareOrdinal(node.id, neighborId) < 0 ? $"{node.id}|{neighborId}" : $"{neighborId}|{node.id}";
                if (!drawn.Add(key))
                    continue;

                Handles.DrawLine(node.position, neighbor.position);
            }
        }
    }

    private void HandleNodeButtons(LDY_GraphMapSetup setup, List<LDY_MapNode> nodeList)
    {
        for (int i = 0; i < nodeList.Count; i++)
        {
            LDY_MapNode node = nodeList[i];
            Vector3 pos = node.position;
            float size = HandleUtility.GetHandleSize(pos) * NodeSize;

            Handles.color = i == selectedIndex ? SelectedColor : (node.isPlayerBase ? BaseColor : NormalColor);

            if (Handles.Button(pos, Quaternion.identity, size, size, Handles.SphereHandleCap))
                OnNodeClicked(setup, nodeList, i);

            Handles.Label(pos + new Vector3(size, size, 0f), node.id);

            if (i == selectedIndex && mode == EditMode.Select)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(pos, size * 0.6f, Vector3.zero, Handles.RectangleHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(setup, "Move Node");
                    node.position = newPos;
                    MarkDirty(setup);
                }
            }
        }
    }

    private void OnNodeClicked(LDY_GraphMapSetup setup, List<LDY_MapNode> nodeList, int index)
    {
        switch (mode)
        {
            case EditMode.Select:
                selectedIndex = index;
                break;

            case EditMode.Connect:
                if (connectFirstIndex < 0)
                {
                    connectFirstIndex = index;
                }
                else if (connectFirstIndex != index)
                {
                    Undo.RecordObject(setup, "Toggle Connection");
                    ToggleConnection(nodeList[connectFirstIndex], nodeList[index]);
                    MarkDirty(setup);
                    connectFirstIndex = -1;
                }
                break;
        }

        Repaint();
    }

    private void HandleAddNodeClick(LDY_GraphMapSetup setup, List<LDY_MapNode> nodeList)
    {
        if (mode != EditMode.AddNode)
            return;

        Event e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0)
            return;

        Vector2 worldPos = GetMouseWorldPosition(e.mousePosition);

        Undo.RecordObject(setup, "Add Node");
        nodeList.Add(new LDY_MapNode
        {
            id = GenerateUniqueId(nodeList),
            position = worldPos,
            isPlayerBase = false,
            connectedNodeIds = new List<string>()
        });
        MarkDirty(setup);

        e.Use();
    }

    private void HandleDeleteKey(LDY_GraphMapSetup setup, List<LDY_MapNode> nodeList)
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;
        if (e.keyCode != KeyCode.Delete && e.keyCode != KeyCode.Backspace)
            return;
        if (selectedIndex < 0 || selectedIndex >= nodeList.Count)
            return;

        Undo.RecordObject(setup, "Delete Node");
        RemoveNode(nodeList, selectedIndex);
        selectedIndex = -1;
        MarkDirty(setup);
        e.Use();
    }

    private Vector2 GetMouseWorldPosition(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return Vector2.zero;
    }

    private string GenerateUniqueId(List<LDY_MapNode> nodeList)
    {
        int i = nodeList.Count;
        string id;
        do
        {
            id = "node" + i;
            i++;
        } while (nodeList.Exists(n => n.id == id));
        return id;
    }

    private void RenameNode(List<LDY_MapNode> nodeList, LDY_MapNode node, string newId)
    {
        if (string.IsNullOrEmpty(newId) || nodeList.Exists(n => n != node && n.id == newId))
        {
            Debug.LogWarning($"[LDY_GraphMapSetup] id '{newId}' 는 비어있거나 이미 존재해서 변경할 수 없습니다.");
            return;
        }

        string oldId = node.id;
        foreach (LDY_MapNode n in nodeList)
        {
            int idx = n.connectedNodeIds.IndexOf(oldId);
            if (idx >= 0)
                n.connectedNodeIds[idx] = newId;
        }
        node.id = newId;
    }

    private void ToggleConnection(LDY_MapNode a, LDY_MapNode b)
    {
        if (a.connectedNodeIds.Contains(b.id))
        {
            a.connectedNodeIds.Remove(b.id);
            b.connectedNodeIds.Remove(a.id);
        }
        else
        {
            a.connectedNodeIds.Add(b.id);
            b.connectedNodeIds.Add(a.id);
        }
    }

    private void RemoveNode(List<LDY_MapNode> nodeList, int index)
    {
        LDY_MapNode target = nodeList[index];
        foreach (LDY_MapNode n in nodeList)
            n.connectedNodeIds.Remove(target.id);
        nodeList.RemoveAt(index);
    }

    private void MarkDirty(LDY_GraphMapSetup setup)
    {
        EditorUtility.SetDirty(setup);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(setup.gameObject.scene);
    }
}
