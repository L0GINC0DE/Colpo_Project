using _01_Scripts.LSO;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class LSO_ColpoEffect : MonoBehaviour
{
    private static readonly int Transparent = Shader.PropertyToID("_Transparent");
    [SerializeField] private GameObject handle;

    private double ColpoLimit => colpoManager.ColpoLimit;
    private double Current => colpoManager.Current;
    private bool Holding => colpoManager.holding;
    private bool is80;

    private LSO_ColpoManager colpoManager;

    [SerializeField] private GameObject playerCam;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private GameObject hinge;
    [SerializeField] private Light roomLight;
    [SerializeField] private CinemachineImpulseSource impulseSource;

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

        // 매번 명확한 기준 회전값으로 리셋 (드리프트 방지)
        hinge.transform.rotation = Quaternion.Euler(0f, originalRotY, 0f);

        playerCam.transform.DOMoveZ(originalPosZ + 0.25f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        hinge.transform.DORotate(new Vector3(0f, originalRotY + 22.25f, 0f), duration * 5).SetUpdate(true);
    }


    private void PlayCloseAnimation()
    {
        playerCam.transform.DOKill();
        

        playerCam.transform.DOMoveZ(originalPosZ, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        /*hinge.transform.DORotate(new Vector3(
            hinge.transform.rotation.eulerAngles.x,
            originalRotY,
            hinge.transform.rotation.eulerAngles.z), duration).SetUpdate(true);*/
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
        }

        if (colpoManager.isColpoTime)
        {
            roomLight.intensity = 10;
        }
        else if (!colpoManager.isColpoTime)
        {
            roomLight.intensity = 0;
        }
        
        roomLight.intensity = colpoManager.isColpoTime ? 10 : 0;

        handle.transform.Rotate(Vector3.up, 90 * Time.deltaTime, Space.Self);
    }

    private void RedEffect()
    {
        DOTween.Kill(fullscreenMaterial);
        fullscreenMaterial.DOFloat(originalRedTransparent, "_Transparent", 0.2f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
    }

    private void ResultEffect(LSO_ColpoManager.ColpoResultType result, double current, LDY_GangType gangType)
    {
        DOTween.Kill(fullscreenMaterial);
        fullscreenMaterial.SetFloat(Transparent, 0f);

        switch (result.ToString())
        { 
            case "Normal":
                NormalEffect();
                break;
            case "Fail":
                FailEffect();
                break;
            case "Colpo":
                ColpoEffect();
                break;
            default:
                Debug.LogError("이상한 값: " + result.ToString());
                break;
        }
    }

    private void ColpoEffect()
{
    hinge.transform.DOKill();
    impulseSource.GenerateImpulse();
    hinge.transform.DORotate(new Vector3(0f, 90f, 0f), duration);
}

private void NormalEffect()
{
    hinge.transform.DOKill();
    hinge.transform.DORotate(new Vector3(0f, originalRotY, 0f), duration).SetUpdate(true);
}

private void FailEffect()
{
    hinge.transform.DOKill();
    hinge.transform.DORotate(new Vector3(0f, originalRotY, 0f), duration / 2)
        .SetEase(Ease.OutQuad).OnComplete(impulseSource.GenerateImpulse).SetUpdate(true);
}
}
