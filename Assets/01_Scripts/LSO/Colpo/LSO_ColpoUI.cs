using _01_Scripts.LSO;
using DG.Tweening;
using UnityEngine;

public class LSO_ColpoUI : MonoBehaviour
{
    private static readonly int Transparent = Shader.PropertyToID("_Transparent");
    [SerializeField] private GameObject handle;

    private float ColpoLimit => colpoManager.ColpoLimit;
    private float Current => colpoManager.Current;
    private bool Holding => colpoManager.holding;
    private bool is80;

    private LSO_ColpoManager colpoManager;

    [SerializeField] private GameObject playerCam;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private GameObject hinge;

    private float originalPosZ;
    private float originalRotY;

    public Material fullscreenMaterial;
    private float originalRedTransparent;

    void Awake()
    {
        originalPosZ = playerCam.transform.position.z;
        originalRotY = hinge.transform.eulerAngles.y;
    }

    void Start()
    {
        colpoManager = LSO_ColpoManager.Instance;
        originalRedTransparent = fullscreenMaterial.GetFloat(Transparent);
        fullscreenMaterial.SetFloat(Transparent, 0);
    }

    // 더 이상 자기가 직접 입력을 받지 않고, Manager 이벤트를 구독
    private void OnEnable()
    {
        LSO_ColpoManager.OnColpoPointerDown += PlayOpenAnimation;
        LSO_ColpoManager.OnColpoPointerUp += PlayCloseAnimation;
        LSO_ColpoManager.OnColpoResult += ResultEffect;
    }

    private void OnDisable()
    {
        LSO_ColpoManager.OnColpoPointerDown -= PlayOpenAnimation;
        LSO_ColpoManager.OnColpoPointerUp -= PlayCloseAnimation;
        LSO_ColpoManager.OnColpoResult -= ResultEffect;

        fullscreenMaterial.SetFloat(Transparent, originalRedTransparent);
    }

    void Update()
    {
        if (Holding)
        {
            OnHold();
        }
    }

    private void PlayOpenAnimation()
    {
        playerCam.transform.DOKill();
        hinge.transform.DOKill();

        playerCam.transform.DOMoveZ(playerCam.transform.position.z + 0.25f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        hinge.transform.DORotate(new Vector3(
            hinge.transform.rotation.eulerAngles.x,
            hinge.transform.rotation.eulerAngles.y + 11.25f,
            hinge.transform.rotation.eulerAngles.z), duration).SetUpdate(true);
    }

    private void PlayCloseAnimation()
    {
        playerCam.transform.DOKill();
        hinge.transform.DOKill();

        playerCam.transform.DOMoveZ(originalPosZ, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        hinge.transform.DORotate(new Vector3(
            hinge.transform.rotation.eulerAngles.x,
            originalRotY,
            hinge.transform.rotation.eulerAngles.z), duration).SetUpdate(true);
        is80 = false;
    }

    private void OnDestroy()
    {
        if (playerCam != null)
            playerCam.transform.DOKill();
        if (hinge != null)
            hinge.transform.DOKill();
        DOTween.Kill(fullscreenMaterial);
    }

    private void OnHold()
    {
        if (Current % 100 == 0 && Current != 0)
        {
            RedEffect();
        }

        if (ColpoLimit * 0.8f <= Current && !is80)
        {
            is80 = true;
            //Debug.LogError("80%");
        }

        handle.transform.Rotate(Vector3.up, 90 * Time.deltaTime, Space.Self);
    }

    private void RedEffect()
    {
        DOTween.Kill(fullscreenMaterial);
        fullscreenMaterial.DOFloat(originalRedTransparent, "_Transparent", 0.2f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
    }

    private void ResultEffect(LSO_ColpoManager.ColpoResultType result, int current, GangType gangType)
    {
        DOTween.Kill(fullscreenMaterial);
        fullscreenMaterial.SetFloat(Transparent, 0f);

        switch (result.ToString())
        { 
            case "Normal":
                Debug.Log("노말");
                break;
            case "Fail":
                Debug.Log("실패");
                break;
            case "Colpo":
                Debug.Log("콜포!");
                break;
            default:
                Debug.LogError("이상한 값: " + result.ToString());
                break;
        }
    }
}
