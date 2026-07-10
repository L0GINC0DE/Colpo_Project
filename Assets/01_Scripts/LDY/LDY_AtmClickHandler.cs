using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class LDY_AtmClickHandler : MonoBehaviour
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
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        LDY_GangController controller = hit.collider.GetComponentInParent<LDY_GangController>();
        if (controller == null || controller.gangData == null)
            return;

        if (LDY_GangManager.Instance == null || LDY_TurnManager.Instance == null)
        {
            Debug.LogWarning("[LDY_AtmClickHandler] LDY_GangManager/LDY_TurnManager.Instance가 아직 없습니다 - 씬 구성을 확인하세요.");
            return;
        }

        string gangName = controller.gangData.gangName;
        Debug.Log($"[LDY_AtmClickHandler] {gangName} ATM 털기 - StealFrom({stealAmount}) (턴 소모)");

        // 갱단창 안의 유료 행동(해킹/털기)이므로 PerformGangWindowAction을 거쳐서 턴을 소모한다.
        LDY_TurnManager.Instance.PerformGangWindowAction(() => LDY_GangManager.Instance.StealFrom(gangName, stealAmount));
    }
}
