using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class LSO_ColpoUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
      private bool isHolding;
      [SerializeField] private GameObject handle;
      private float colpoLimit;
      private float current;
      [SerializeField] private GameObject playerCam;
      [SerializeField] private float duration = 0.3f;
      [SerializeField] private GameObject hinge;
      
      private float originalPosZ;
      private float originalRotY;

      void Awake()
      {
         // 버튼의 원래 위치
         originalPosZ = playerCam.transform.position.z;
         originalRotY = hinge.transform.eulerAngles.y;
      }
   

      void Update()
      {
         // 버튼을 누르는 동안 계속 실행할 동작
         if (isHolding)
         {
            OnHold();
         }
      }

      // 버튼을 처음 눌렀을 때
      public void OnPointerDown(PointerEventData eventData)
      {
         playerCam.transform.DOMoveZ(playerCam.transform.position.z + 0.25f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
         
         hinge.transform.DORotate(new Vector3(
               playerCam.transform.rotation.eulerAngles.x,
               playerCam.transform.rotation.eulerAngles.y + 1,
               playerCam.transform.rotation.eulerAngles.z) , duration).SetUpdate(true);
         
         isHolding = true;
      }

      // 버튼에서 손을 떼었을 때
      public void OnPointerUp(PointerEventData eventData)
      {
         playerCam.transform.DOMoveZ(originalPosZ, duration).SetEase(Ease.OutQuad).SetUpdate(true);
         
         hinge.transform.DORotate(new Vector3(
            playerCam.transform.rotation.eulerAngles.x,
            originalRotY,
            playerCam.transform.rotation.eulerAngles.z) , duration).SetUpdate(true);
         
         isHolding = false;
      }  

      private void OnDestroy()
      {
         // 오브젝트가 파괴될 때 트윈 잔재 제거 (메모리 관리)
         playerCam.transform.DOKill();
         hinge.transform.DOKill();
      }
      
      //버튼을 누르는 동안에
      private void OnHold()
      {
         //handle.transform.Rotate(new Vector3())
      }
}
