using UnityEngine;

// SkillMenu 패널 전체 관리. 위쪽 << 버튼(OnClick)에 ToggleCollapsed를 연결하면
// 유니티 인스펙터 접듯이 버튼 목록을 숨기고 패널을 작게 줄인다.
public class LDY_SkillMenuController : MonoBehaviour
{
    [SerializeField] private RectTransform content; // 접었다 펼 대상(버튼들이 있는 BackGround).
    [SerializeField] private RectTransform collapseArrow; // 접히면 좌우로 뒤집어서 방향을 보여줌.
    [SerializeField] private float collapsedHeight = 60f;

    private RectTransform panelRect;
    private float expandedHeight;
    private bool collapsed;

    private void Awake()
    {
        panelRect = (RectTransform)transform;
        expandedHeight = panelRect.sizeDelta.y;

        if (content == null)
            content = transform.Find("BackGround") as RectTransform;
    }

    // << 버튼의 OnClick에 연결.
    public void ToggleCollapsed()
    {
        collapsed = !collapsed;

        if (content != null)
            content.gameObject.SetActive(!collapsed);

        Vector2 size = panelRect.sizeDelta;
        size.y = collapsed ? collapsedHeight : expandedHeight;
        panelRect.sizeDelta = size;

        if (collapseArrow != null)
        {
            Vector3 scale = collapseArrow.localScale;
            scale.x = collapsed ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            collapseArrow.localScale = scale;
        }
    }
}
