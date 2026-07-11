using UnityEngine;
using UnityEngine.InputSystem;

// 노이즈 아이템. D로 조준 모드를 켜면(커서가 회색 지그재그 모양으로 바뀜) 다음 좌클릭에
// 맞은 갱단의 시너지/부조화 상성 효과를 noiseDuration턴 동안 무효화한다.
[RequireComponent(typeof(Camera))]
public class LDY_NoiseItemHandler : MonoBehaviour
{
    public static LDY_NoiseItemHandler Instance { get; private set; }

    [SerializeField] private int noiseCharges = 3;
    [SerializeField] private int noiseDuration = 2;

    private int initialNoiseCharges;
    private Camera cam;
    private LDY_AtmClickHandler atmClickHandler;
    private Texture2D cursorTexture;
    private bool armed;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        atmClickHandler = GetComponent<LDY_AtmClickHandler>();
        initialNoiseCharges = noiseCharges;
        cursorTexture = BuildCursorTexture();
    }

    // 게임 리스타트: 개수를 되돌리고 조준 모드도 취소한다.
    public void ResetCharges()
    {
        noiseCharges = initialNoiseCharges;
        Disarm();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.dKey.wasPressedThisFrame)
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
                LDY_GangManager.Instance.Noise(controller.gangData.gangName, noiseDuration);
                noiseCharges--;
                Debug.Log($"[LDY_NoiseItemHandler] {controller.gangData.gangName} 노이즈 ({noiseDuration}턴, 남은 개수: {noiseCharges})");
            }
        }

        Disarm();
    }

    // UI 버튼(SkillMenu)의 OnClick과 키보드(D) 양쪽에서 호출.
    public void ToggleArmed()
    {
        if (armed)
        {
            Disarm();
            return;
        }

        if (noiseCharges <= 0)
        {
            Debug.Log("[LDY_NoiseItemHandler] 노이즈 개수를 다 썼습니다.");
            return;
        }

        if (LDY_FreezeItemHandler.Instance != null)
            LDY_FreezeItemHandler.Instance.Disarm();
        if (LDY_WallItemHandler.Instance != null)
            LDY_WallItemHandler.Instance.Disarm();
        if (LDY_PathRedirectHandler.Instance != null)
            LDY_PathRedirectHandler.Instance.Disarm();
        if (LDY_AttackSkillItemHandler.Instance != null)
            LDY_AttackSkillItemHandler.Instance.Disarm();

        armed = true;
        if (atmClickHandler != null)
            atmClickHandler.enabled = false;

        Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width, cursorTexture.height) * 0.5f, CursorMode.Auto);
        Debug.Log("[LDY_NoiseItemHandler] 노이즈 조준 모드 - 갱단을 좌클릭하세요 (다시 D를 누르면 취소)");
    }

    // 다른 아이템(S/A/F)이 조준 모드를 켤 때 이쪽을 취소시킬 수 있도록 공개해둔다.
    public void Disarm()
    {
        if (!armed)
            return;

        armed = false;
        if (atmClickHandler != null)
            atmClickHandler.enabled = true;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 별도 아트 없이 코드로 만드는 임시 커서(회색 지그재그 - 잡음 느낌). 나중에 실제 아이콘으로 교체 예정.
    private static Texture2D BuildCursorTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color gray = new Color(0.6f, 0.6f, 0.6f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        int mid = size / 2;
        for (int x = 0; x < size; x++)
        {
            int y = (x / 4) % 2 == 0 ? mid - 4 : mid + 4;
            texture.SetPixel(x, y, gray);
            texture.SetPixel(x, Mathf.Clamp(y + 1, 0, size - 1), gray);
        }

        texture.Apply();
        return texture;
    }
}
