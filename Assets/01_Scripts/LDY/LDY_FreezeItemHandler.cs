using UnityEngine;
using UnityEngine.InputSystem;

// 얼리기 아이템. S로 조준 모드를 켜면(커서가 얼음 모양으로 바뀜)
// 다음 좌클릭에 맞은 갱단을 freezeDuration턴 얼린다.
[RequireComponent(typeof(Camera))]
public class LDY_FreezeItemHandler : MonoBehaviour
{
    public static LDY_FreezeItemHandler Instance { get; private set; }

    [SerializeField] private int freezeCharges = 3;
    [SerializeField] private int freezeDuration = 1;

    // 얼음 오버레이에 쓸 머티리얼(icePrefab이 비어있을 때만 사용). 이것도 비워두면
    // GangController가 기본 흰색 Quad로 대신한다.
    [SerializeField] private Material iceMaterial;
    public Material IceMaterial => iceMaterial;

    // 직접 만든 얼음 프리팹. 지정돼 있으면 iceMaterial보다 우선해서 그대로 Instantiate하고,
    // 코드에선 그 프리팹 머티리얼의 _size만 애니메이션한다.
    [SerializeField] private GameObject icePrefab;
    public GameObject IcePrefab => icePrefab;

    private int initialFreezeCharges;
    private Camera cam;
    private LDY_AtmClickHandler atmClickHandler;
    private Texture2D cursorTexture;
    private bool armed;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        atmClickHandler = GetComponent<LDY_AtmClickHandler>();
        initialFreezeCharges = freezeCharges;
        cursorTexture = BuildCursorTexture();
    }

    // 게임 리스타트: 개수를 되돌리고 조준 모드도 취소한다.
    public void ResetCharges()
    {
        freezeCharges = initialFreezeCharges;
        Disarm();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.sKey.wasPressedThisFrame)
            ToggleArmed();

        if (!armed)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            LDY_GangController controller = hit.collider.GetComponentInParent<LDY_GangController>();
            if (controller != null && controller.gangData != null)
            {
                LDY_GangManager.Instance.Freeze(controller.gangData.gangName, freezeDuration);
                freezeCharges--;
                Debug.Log($"[LDY_FreezeItemHandler] {controller.gangData.gangName} 얼림 ({freezeDuration}턴, 남은 개수: {freezeCharges})");
            }
        }

        Disarm();
    }

    // UI 버튼(SkillMenu)의 OnClick과 키보드(S) 양쪽에서 호출.
    public void ToggleArmed()
    {
        if (armed)
        {
            Disarm();
            return;
        }

        if (freezeCharges <= 0)
        {
            Debug.Log("[LDY_FreezeItemHandler] 얼리기 개수를 다 썼습니다.");
            return;
        }

        if (LDY_WallItemHandler.Instance != null)
            LDY_WallItemHandler.Instance.Disarm();
        if (LDY_PathRedirectHandler.Instance != null)
            LDY_PathRedirectHandler.Instance.Disarm();
        if (LDY_NoiseItemHandler.Instance != null)
            LDY_NoiseItemHandler.Instance.Disarm();
        if (LDY_AttackSkillItemHandler.Instance != null)
            LDY_AttackSkillItemHandler.Instance.Disarm();

        armed = true;
        if (atmClickHandler != null)
            atmClickHandler.enabled = false;

        Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width, cursorTexture.height) * 0.5f, CursorMode.Auto);
        Debug.Log("[LDY_FreezeItemHandler] 얼리기 조준 모드 - 갱단을 좌클릭하세요 (다시 S를 누르면 취소)");
    }

    // 다른 아이템(A: 벽)이 조준 모드를 켤 때 이쪽을 취소시킬 수 있도록 공개해둔다.
    public void Disarm()
    {
        if (!armed)
            return;

        armed = false;
        if (atmClickHandler != null)
            atmClickHandler.enabled = true;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 별도 아트 없이 코드로 만드는 임시 커서(십자 모양). 나중에 실제 아이콘으로 교체 예정.
    private static Texture2D BuildCursorTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color ice = new Color(0.4f, 0.8f, 1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        int center = size / 2;
        for (int i = 0; i < size; i++)
        {
            texture.SetPixel(i, center, ice);
            texture.SetPixel(i, Mathf.Clamp(center - 1, 0, size - 1), ice);
            texture.SetPixel(center, i, ice);
            texture.SetPixel(Mathf.Clamp(center - 1, 0, size - 1), i, ice);
        }

        texture.Apply();
        return texture;
    }
}
