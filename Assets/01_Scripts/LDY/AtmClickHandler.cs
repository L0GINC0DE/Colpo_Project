using UnityEngine;
using UnityEngine.InputSystem;

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
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
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
