using UnityEngine;
using UnityEngine.InputSystem;

// [테스트/플레이어 조작] 갱단을 직접 터는 게 아니라, 그 갱단이 관리하는 ATM기를 털어서
// 돈을 훔친다는 설정. 지금은 별도 ATM 비주얼 없이 갱단 마커를 그 자리로 취급해서
// 우클릭하면 GangManager.StealFrom을 호출한다.
// StealFrom이 성공하면 그 갱단의 추적(pursuing)이 시작되고,
// TurnManager.AdvanceTurn()이 호출될 때마다 성향에 맞게 플레이어 기지 쪽으로 이동한다.
//
// 주의: MapCameraController도 우클릭(드래그)으로 카메라를 움직인다.
// 같은 우클릭을 두 스크립트가 각자 처리하므로, 마커 위에서 클릭하면 훔치기와
// 드래그 시작이 동시에 발생한다 (드래그 없이 짧게 클릭하면 카메라는 실질적으로 안 움직인다).
[RequireComponent(typeof(Camera))]
public class AtmClickHandler : MonoBehaviour
{
    [SerializeField] private int stealAmount = 300;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        GangController controller = hit.collider.GetComponentInParent<GangController>();
        if (controller == null || controller.gangData == null)
            return;

        Debug.Log($"[AtmClickHandler] {controller.gangData.gangName} ATM 털기 - StealFrom({stealAmount})");
        GangManager.Instance.StealFrom(controller.gangData.gangName, stealAmount);
    }
}
