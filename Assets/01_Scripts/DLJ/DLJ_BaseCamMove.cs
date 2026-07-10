using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class DLJ_BaseCamMove : MonoBehaviour
{
    [SerializeField] private GameObject camera;

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //camera.transform.DOMove(left ? new Vector3(15.4f, 4.5f, -27.7f) : new Vector3(0.99f, 1, -15.19f), 0.3f).SetEase(Ease.OutCirc);
            //camera.transform.DORotate(left ? new Vector3(4.8f, -90.12f, 0) :  Vector3.zero, 0.3f).SetEase(Ease.OutCirc);
            Back();
        }
    }

    private void Back()
    {
        camera.transform.DOMove(new Vector3(15.4f, 4.5f, -27.7f), 0.5f);
        camera.transform.DORotate(new Vector3(4.8f, -62.33f, 0), 0.5f);
    }
}
