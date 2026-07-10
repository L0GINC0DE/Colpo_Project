using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;


public class DLJ_OnMapClick : MonoBehaviour
{
    [SerializeField] private GameObject camera;
    [SerializeField] private Camera raycastCamera;

    private readonly Vector3 targetPosition = new Vector3(4.73f, 6.8f, -26.66f);
    private readonly Vector3 targetRotation = new Vector3(-4.27f, -88.08f, 0f);
    private int lastClickFrame = -1;

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Camera clickCamera = raycastCamera != null ? raycastCamera : Camera.main;
        if (clickCamera == null)
            return;

        Ray ray = clickCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.transform == transform || hit.transform.IsChildOf(transform))
            MoveCamera();
    }

    private void OnMouseDown()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        if (lastClickFrame == Time.frameCount || camera == null)
            return;

        lastClickFrame = Time.frameCount;
        camera.transform.DOMove(targetPosition, 0.5f);
        camera.transform.DORotate(targetRotation, 0.5f);
    }
}
