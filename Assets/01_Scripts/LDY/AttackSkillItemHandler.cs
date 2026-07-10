using UnityEngine;
using UnityEngine.InputSystem;

// [플레이스홀더] 공격형 스킬(기밀정보유출). 실제 효과는 아직 미정이라 GangManager 쪽은
// 로그만 찍는다 - 조준/버튼 흐름만 다른 아이템이랑 똑같이 맞춰뒀다. X로 조준 모드를 켜면
// (커서가 빨간 X 모양으로 바뀜) 다음 좌클릭에 맞은 갱단에게 스킬을 쓴다.
// 공격형 스킬은 턴을 소모하므로 TurnManager.PerformGangWindowAction을 거쳐서 실행한다.
[RequireComponent(typeof(Camera))]
public class AttackSkillItemHandler : MonoBehaviour
{
    public static AttackSkillItemHandler Instance { get; private set; }

    [SerializeField] private int charges = 3;

    private int initialCharges;
    private Camera cam;
    private AtmClickHandler atmClickHandler;
    private Texture2D cursorTexture;
    private bool armed;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        atmClickHandler = GetComponent<AtmClickHandler>();
        initialCharges = charges;
        cursorTexture = BuildCursorTexture();
    }

    // 게임 리스타트: 개수를 되돌리고 조준 모드도 취소한다.
    public void ResetCharges()
    {
        charges = initialCharges;
        Disarm();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.xKey.wasPressedThisFrame)
            ToggleArmed();

        if (!armed)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GangController controller = hit.collider.GetComponentInParent<GangController>();
            if (controller != null && controller.gangData != null && TurnManager.Instance != null)
            {
                string gangId = controller.gangData.gangName;
                TurnManager.Instance.PerformGangWindowAction(() => GameEvents.SkillUsed("기밀정보유출", gangId, 0));

                charges--;
                Debug.Log($"[AttackSkillItemHandler] {gangId} 대상 기밀정보유출 사용 (남은 개수: {charges})");
            }
        }

        Disarm();
    }

    // UI 버튼(SkillMenu)의 OnClick과 키보드(X) 양쪽에서 호출.
    public void ToggleArmed()
    {
        if (armed)
        {
            Disarm();
            return;
        }

        if (charges <= 0)
        {
            Debug.Log("[AttackSkillItemHandler] 개수를 다 썼습니다.");
            return;
        }

        if (WallItemHandler.Instance != null)
            WallItemHandler.Instance.Disarm();
        if (FreezeItemHandler.Instance != null)
            FreezeItemHandler.Instance.Disarm();
        if (NoiseItemHandler.Instance != null)
            NoiseItemHandler.Instance.Disarm();
        if (PathRedirectHandler.Instance != null)
            PathRedirectHandler.Instance.Disarm();

        armed = true;
        if (atmClickHandler != null)
            atmClickHandler.enabled = false;

        Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width, cursorTexture.height) * 0.5f, CursorMode.Auto);
        Debug.Log("[AttackSkillItemHandler] 조준 모드 - 갱단을 좌클릭하세요 (다시 누르면 취소)");
    }

    // 다른 아이템이 조준 모드를 켤 때 이쪽을 취소시킬 수 있도록 공개해둔다.
    public void Disarm()
    {
        if (!armed)
            return;

        armed = false;
        if (atmClickHandler != null)
            atmClickHandler.enabled = true;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 별도 아트 없이 코드로 만드는 임시 커서(빨간 X). 나중에 실제 아이콘으로 교체 예정.
    private static Texture2D BuildCursorTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color red = new Color(1f, 0.2f, 0.2f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        for (int i = 0; i < size; i++)
        {
            texture.SetPixel(i, i, red);
            texture.SetPixel(Mathf.Clamp(i + 1, 0, size - 1), i, red);
            texture.SetPixel(size - 1 - i, i, red);
            texture.SetPixel(Mathf.Clamp(size - 2 - i, 0, size - 1), i, red);
        }

        texture.Apply();
        return texture;
    }
}
