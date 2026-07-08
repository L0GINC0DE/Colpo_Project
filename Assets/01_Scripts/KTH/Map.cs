using System;
using UnityEngine;
using DG.Tweening;

public class Map : MonoBehaviour
{
 
    [SerializeField] private GameObject map;
    [SerializeField] private RectTransform mapRect;   // map의 RectTransform

    [SerializeField] private float dropDuration = 0.5f;

    private float startY;   // 처음(화면 밖) Y 위치
    private Tween moveTween;

    private void Awake()
    {
        startY = mapRect.anchoredPosition.y; // 화면 밖에 배치된 초기 위치 저장
    }

    private void OnMouseDown()
    {
        OnMaxImage();
    }

    public void OnClick()
    {
        OnMinImage();
    }

    private void OnMaxImage()
    {
        map.SetActive(true);

        // 혹시 이전 위치가 남아있을 수 있으니 시작 위치로 초기화
        Vector2 pos = mapRect.anchoredPosition;
        pos.y = startY;
        mapRect.anchoredPosition = pos;

        moveTween?.Kill();
        moveTween = mapRect
            .DOAnchorPosY(0f, dropDuration)   // Y가 0이 될 때까지 부드럽게 내려감
            .SetEase(Ease.OutBack, 1.2f);     // 살짝 튕기는 반동 포함
    }

    private void OnMinImage()
    {
        moveTween?.Kill();
        moveTween = mapRect
            .DOAnchorPosY(startY, dropDuration)  // 다시 원래(화면 밖) 위치로 올라감
            .SetEase(Ease.InBack)
            .OnComplete(() => map.SetActive(false));
    }
}

