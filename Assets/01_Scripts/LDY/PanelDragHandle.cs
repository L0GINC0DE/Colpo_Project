using UnityEngine;
using UnityEngine.EventSystems;

// 버튼이 아닌 배경(BackGround) 위에 붙여서, 여기를 마우스로 잡고 끌면 부모 패널
// (SkillMenu) 전체가 따라 움직인다. 버튼 위에 붙이면 클릭이랑 드래그가 서로 씹힌다.
// 패널이 화면(캔버스) 밖으로 나가지 않도록 위치를 캔버스 범위 안으로 잘라낸다.
public class PanelDragHandle : MonoBehaviour, IDragHandler
{
    [SerializeField] private RectTransform panelToMove; // 비워두면 부모 RectTransform을 씀.

    private Canvas canvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (panelToMove == null)
            panelToMove = transform.parent as RectTransform;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (panelToMove == null)
            return;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 pos = panelToMove.anchoredPosition + eventData.delta / scale;

        // 패널(anchor 0.5,0.5 기준)이 캔버스 밖으로 나가지 않도록 이동 범위를 제한한다.
        if (canvasRect != null)
        {
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 panelSize = panelToMove.rect.size;
            float maxX = Mathf.Max(0f, (canvasSize.x - panelSize.x) * 0.5f);
            float maxY = Mathf.Max(0f, (canvasSize.y - panelSize.y) * 0.5f);
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        }

        panelToMove.anchoredPosition = pos;
    }
}
