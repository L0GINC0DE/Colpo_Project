using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 30f;
    [SerializeField] private float mapPadding = 2f; // 맵 가장자리 노드가 화면 끝에 딱 붙지 않도록 주는 여유 공간

    private Camera cam;
    private Vector3 dragOrigin;
    private bool dragging;

    private bool boundsReady;
    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleKeyboardMove();
        HandleZoom();
        HandleDrag();
        ClampToMapBounds();
    }

    private void EnsureBounds()
    {
        if (boundsReady)
            return;

        if (GraphMapSetup.Instance == null)
            return;

        List<MapNode> nodeList = GraphMapSetup.Instance.NodeList;
        if (nodeList == null || nodeList.Count == 0)
            return;

        minX = maxX = nodeList[0].position.x;
        minY = maxY = nodeList[0].position.y;

        foreach (MapNode node in nodeList)
        {
            minX = Mathf.Min(minX, node.position.x);
            maxX = Mathf.Max(maxX, node.position.x);
            minY = Mathf.Min(minY, node.position.y);
            maxY = Mathf.Max(maxY, node.position.y);
        }

        minX -= mapPadding;
        maxX += mapPadding;
        minY -= mapPadding;
        maxY += mapPadding;

        boundsReady = true;
    }

    // 카메라가 보는 화면 범위(orthographicSize 기준)가 맵 경계 밖으로 나가지 않도록 위치를 잘라낸다.
    private void ClampToMapBounds()
    {
        EnsureBounds();
        if (!boundsReady)
            return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        Vector3 pos = transform.position;
        pos.x = ClampAxis(pos.x, minX, maxX, halfWidth);
        pos.y = ClampAxis(pos.y, minY, maxY, halfHeight);
        transform.position = pos;
    }

    // 맵이 화면보다 작은 축(너무 확대/축소한 경우)은 중앙에 고정한다.
    private float ClampAxis(float value, float min, float max, float half)
    {
        float lo = min + half;
        float hi = max - half;
        if (lo > hi)
            return (min + max) * 0.5f;
        return Mathf.Clamp(value, lo, hi);
    }

    private void HandleKeyboardMove()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector2 dir = Vector2.zero;
        if (keyboard.upArrowKey.isPressed) dir.y += 1f;
        if (keyboard.downArrowKey.isPressed) dir.y -= 1f;
        if (keyboard.leftArrowKey.isPressed) dir.x -= 1f;
        if (keyboard.rightArrowKey.isPressed) dir.x += 1f;

        if (dir != Vector2.zero)
            transform.position += (Vector3)(dir.normalized * moveSpeed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !cam.orthographic)
            return;

        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) > 0.01f)
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scrollY * zoomSpeed * 0.01f, minZoom, maxZoom);
    }

    private void HandleDrag()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            dragOrigin = ScreenToWorld(mouse.position.ReadValue());
            dragging = true;
        }
        else if (mouse.rightButton.wasReleasedThisFrame)
        {
            dragging = false;
        }

        if (dragging)
        {
            Vector3 currentWorld = ScreenToWorld(mouse.position.ReadValue());
            transform.position += dragOrigin - currentWorld;
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        return cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -transform.position.z));
    }
}
