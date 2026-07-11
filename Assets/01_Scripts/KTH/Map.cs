using System;
using UnityEngine;
using DG.Tweening;

public class Map : MonoBehaviour
{
 
    [SerializeField] private GameObject map;
    [SerializeField] private float scaleStep = 0.01f;   // 한 스텝당 커지는 크기 단위
    [SerializeField] private float scaleDuration = 0.3f; // 전체 애니메이션 시간

    private Tween scaleTween;

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
        map.transform.localScale = Vector3.zero; // 0에서 시작

        scaleTween?.Kill();
        scaleTween = map.transform
            .DOScale(Vector3.one, scaleDuration)
            .SetEase(Ease.OutQuad);
    }

    private void OnMinImage()
    {
        scaleTween?.Kill();
        scaleTween = map.transform
            .DOScale(Vector3.zero, scaleDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => map.SetActive(false));
    }
}

