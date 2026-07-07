using UnityEngine;
using UnityEngine.InputSystem;

// [테스트/플레이어 조작] 벽 아이템. B로 조준 모드를 켜면(커서가 벽돌 모양으로 바뀜)
// 다음 좌클릭에 찍은 간선(EdgeMarker 콜라이더)을 wallDurationTurns턴 동안 막는다.
// 조준 중엔 AtmClickHandler(훔치기)를 잠깐 꺼서 같은 좌클릭에 훔치기가 같이 발동하지 않게 한다.
[RequireComponent(typeof(Camera))]
public class WallItemHandler : MonoBehaviour
{
    public static WallItemHandler Instance { get; private set; }

    [SerializeField] private int wallCharges = 3;
    [SerializeField] private int wallDurationTurns = 3;

    private int initialWallCharges;
    private Camera cam;
    private AtmClickHandler atmClickHandler;
    private Texture2D cursorTexture;
    private bool armed;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        atmClickHandler = GetComponent<AtmClickHandler>();
        initialWallCharges = wallCharges;
        cursorTexture = BuildCursorTexture();
    }

    // 게임 리스타트: 개수를 되돌리고 조준 모드도 취소한다.
    public void ResetCharges()
    {
        wallCharges = initialWallCharges;
        Disarm();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            ToggleArmed();

        if (!armed)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            EdgeMarker edge = hit.collider.GetComponent<EdgeMarker>();
            if (edge != null)
            {
                int currentTurn = TurnManager.Instance != null ? TurnManager.Instance.currentTurn : 0;
                MapStateManager.Instance.BlockEdge(edge.nodeAId, edge.nodeBId, currentTurn, wallDurationTurns);

                wallCharges--;
                Debug.Log($"[WallItemHandler] {edge.nodeAId}-{edge.nodeBId} 벽 설치 ({wallDurationTurns}턴, 남은 개수: {wallCharges})");
            }
        }

        Disarm();
    }

    private void ToggleArmed()
    {
        if (armed)
        {
            Disarm();
            return;
        }

        if (wallCharges <= 0)
        {
            Debug.Log("[WallItemHandler] 벽 개수를 다 썼습니다.");
            return;
        }

        if (FreezeItemHandler.Instance != null)
            FreezeItemHandler.Instance.Disarm();

        armed = true;
        if (atmClickHandler != null)
            atmClickHandler.enabled = false;

        Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width, cursorTexture.height) * 0.5f, CursorMode.Auto);
        Debug.Log("[WallItemHandler] 벽 조준 모드 - 막을 간선을 좌클릭하세요 (다시 B를 누르면 취소)");
    }

    // 다른 아이템(F: 얼리기)이 조준 모드를 켤 때 이쪽을 취소시킬 수 있도록 공개해둔다.
    public void Disarm()
    {
        if (!armed)
            return;

        armed = false;
        if (atmClickHandler != null)
            atmClickHandler.enabled = true;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 별도 아트 없이 코드로 만드는 임시 커서(벽돌 느낌의 사각 테두리). 나중에 실제 아이콘으로 교체 예정.
    private static Texture2D BuildCursorTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color brick = new Color(0.5f, 0.35f, 0.2f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        const int border = 3;
        for (int i = 0; i < size; i++)
        {
            for (int t = 0; t < border; t++)
            {
                texture.SetPixel(i, t, brick);
                texture.SetPixel(i, size - 1 - t, brick);
                texture.SetPixel(t, i, brick);
                texture.SetPixel(size - 1 - t, i, brick);
            }
        }

        texture.Apply();
        return texture;
    }
}
