using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 추적 경로 재탐색. F로 조준 모드를 켜고, 먼저 추적 중인 갱단을 좌클릭해서
// 고른 다음, 인접한 노드를 순서대로 좌클릭해서 강제 경로를 그린다(뒤로 가거나 이미 지나온
// 노드로는 못 감 - 무조건 앞으로만). 경로를 그리다가 다시 F를 누르면 지금까지 그린 경로를
// 확정해서 그 갱단에게 적용한다. 사이 안 좋은 갱단끼리 같은 자리로 몰아넣는 용도로 쓴다.
[RequireComponent(typeof(Camera))]
public class LDY_PathRedirectHandler : MonoBehaviour
{
    public static LDY_PathRedirectHandler Instance { get; private set; }

    [SerializeField] private int charges = 3;

    private int initialCharges;
    private Camera cam;
    private LDY_AtmClickHandler atmClickHandler;
    private Texture2D cursorTexture;
    private bool armed;

    private LDY_GangController selectedGang;
    private List<string> pathBuilder;
    private LineRenderer previewLine;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        atmClickHandler = GetComponent<LDY_AtmClickHandler>();
        initialCharges = charges;
        cursorTexture = BuildCursorTexture();
    }

    // 게임 리스타트: 개수를 되돌리고 조준/작성 중이던 경로도 취소한다.
    public void ResetCharges()
    {
        charges = initialCharges;
        Disarm();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            OnButtonPressed();

        if (!armed)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (selectedGang == null)
        {
            TrySelectGang(hit);
            return;
        }

        TryExtendPath(hit);
    }

    private void TrySelectGang(RaycastHit hit)
    {
        LDY_GangController controller = hit.collider.GetComponentInParent<LDY_GangController>();
        if (controller == null || !controller.pursuing)
        {
            Debug.Log("[LDY_PathRedirectHandler] 추적 중인 갱단만 고를 수 있습니다.");
            return;
        }

        selectedGang = controller;
        pathBuilder = new List<string> { controller.gangData.currentNodeId };
        Debug.Log($"[LDY_PathRedirectHandler] {controller.gangData.gangName} 선택 - 이어질 노드를 좌클릭하세요 (F로 확정)");
    }

    private void TryExtendPath(RaycastHit hit)
    {
        LDY_NodeMarker node = hit.collider.GetComponent<LDY_NodeMarker>();
        if (node == null)
            return;

        if (pathBuilder.Contains(node.nodeId))
        {
            Debug.Log("[LDY_PathRedirectHandler] 이미 지나온 노드로는 못 돌아갑니다 (앞으로만 진행 가능).");
            return;
        }

        string lastNodeId = pathBuilder[pathBuilder.Count - 1];
        if (!LDY_GraphMapSetup.Instance.Nodes.TryGetValue(lastNodeId, out LDY_MapNode last) || !last.connectedNodeIds.Contains(node.nodeId))
        {
            Debug.Log("[LDY_PathRedirectHandler] 인접한 노드만 이어서 찍을 수 있습니다.");
            return;
        }

        pathBuilder.Add(node.nodeId);
        UpdatePreviewLine();
    }

    // UI 버튼(SkillMenu)의 OnClick과 키보드(F) 양쪽에서 호출 - armed면 확정, 아니면 조준 시작.
    public void OnButtonPressed()
    {
        if (armed)
            Confirm();
        else
            Arm();
    }

    private void Arm()
    {
        if (charges <= 0)
        {
            Debug.Log("[LDY_PathRedirectHandler] 경로 재지정 개수를 다 썼습니다.");
            return;
        }

        if (LDY_FreezeItemHandler.Instance != null)
            LDY_FreezeItemHandler.Instance.Disarm();
        if (LDY_WallItemHandler.Instance != null)
            LDY_WallItemHandler.Instance.Disarm();
        if (LDY_NoiseItemHandler.Instance != null)
            LDY_NoiseItemHandler.Instance.Disarm();
        if (LDY_AttackSkillItemHandler.Instance != null)
            LDY_AttackSkillItemHandler.Instance.Disarm();

        armed = true;
        selectedGang = null;
        pathBuilder = null;
        if (atmClickHandler != null)
            atmClickHandler.enabled = false;

        Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width, cursorTexture.height) * 0.5f, CursorMode.Auto);
        Debug.Log("[LDY_PathRedirectHandler] 경로 재지정 모드 - 추적 중인 갱단을 좌클릭하세요 (F를 다시 누르면 취소/확정)");
    }

    private void Confirm()
    {
        if (selectedGang != null && pathBuilder != null && pathBuilder.Count >= 2)
        {
            selectedGang.SetOverridePath(pathBuilder);
            charges--;
            Debug.Log($"[LDY_PathRedirectHandler] 경로 확정 (남은 개수: {charges})");
        }
        else
        {
            Debug.Log("[LDY_PathRedirectHandler] 경로가 없어서 취소합니다.");
        }

        Disarm();
    }

    // 다른 아이템(S: 얼리기, A: 벽)이 조준 모드를 켤 때 이쪽을 취소시킬 수 있도록 공개해둔다.
    public void Disarm()
    {
        armed = false;
        selectedGang = null;
        pathBuilder = null;
        if (atmClickHandler != null)
            atmClickHandler.enabled = true;
        if (previewLine != null)
            previewLine.gameObject.SetActive(false);

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void UpdatePreviewLine()
    {
        if (previewLine == null)
        {
            GameObject go = new GameObject("PathRedirectPreview");
            previewLine = go.AddComponent<LineRenderer>();
            previewLine.useWorldSpace = true;
            previewLine.startWidth = 0.1f;
            previewLine.endWidth = 0.1f;
            previewLine.material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };
            previewLine.startColor = Color.yellow;
            previewLine.endColor = Color.yellow;
        }

        previewLine.gameObject.SetActive(true);
        previewLine.positionCount = pathBuilder.Count;
        for (int i = 0; i < pathBuilder.Count; i++)
        {
            if (LDY_GraphMapSetup.Instance.Nodes.TryGetValue(pathBuilder[i], out LDY_MapNode node))
                previewLine.SetPosition(i, new Vector3(node.position.x, node.position.y, -0.07f));
        }
    }

    // 별도 아트 없이 코드로 만드는 임시 커서(대각선 화살표 느낌). 나중에 실제 아이콘으로 교체 예정.
    private static Texture2D BuildCursorTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color yellow = Color.yellow;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        for (int i = 0; i < size; i++)
        {
            texture.SetPixel(i, i, yellow);
            texture.SetPixel(Mathf.Clamp(i + 1, 0, size - 1), i, yellow);
        }

        texture.Apply();
        return texture;
    }
}
