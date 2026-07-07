using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// [테스트 전용] 맵이 넓어서 카메라로 둘러볼 수 있게 만든 간단한 컨트롤러.
// TestDriver가 S/Space/B/N 키를 이미 쓰고 있어서, 겹치지 않게 방향키/마우스만 사용한다.
//   방향키       : 카메라 이동
//   마우스 우클릭 드래그 : 카메라 이동
//   마우스 스크롤 : 확대/축소 (orthographic size 조절)
// 카메라는 GraphMapSetup의 노드 좌표 범위를 벗어나지 못하도록 매 프레임 클램프된다.
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

    // GraphMapSetup의 노드 좌표 최소/최대값으로 맵 경계를 한 번만 계산해 캐싱한다.
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
