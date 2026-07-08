using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class LSO_ColpoUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
      private bool isHolding;
      [SerializeField] private GameObject handle;

      private float colpoTime = 0.7f;
      private float colpoLimit = 1000;//시스템에서 가져올꺼(임시)
      private float current;//시스템에서 가져올꺼(임시)
      private float timer;
      private bool is80;
      
      [SerializeField] private GameObject playerCam;
      [SerializeField] private float duration = 0.3f;
      [SerializeField] private GameObject hinge;
      
      private bool isColpoTime;//이것도 시스템에서 가져올꺼(임시)
      
      private float originalPosZ;
      private float originalRotY;
      
      public Material fullscreenMaterial;
      private float originalRedTransparent;

      

      void Awake()
      {
         // 버튼의 원래 위치
         originalPosZ = playerCam.transform.position.z;
         originalRotY = hinge.transform.eulerAngles.y;
      }
      
      void Start()
      {
         originalRedTransparent = fullscreenMaterial.GetFloat("_Transparent");
         fullscreenMaterial.SetFloat("_Transparent", 0);
         Debug.Log(originalRedTransparent);
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
         playerCam.transform.DOKill();
         hinge.transform.DOKill();
         
         playerCam.transform.DOMoveZ(playerCam.transform.position.z + 0.25f, duration).SetEase(Ease.OutQuad).SetUpdate(true);
         
         hinge.transform.DORotate(new Vector3(
               hinge.transform.rotation.eulerAngles.x,
               hinge.transform.rotation.eulerAngles.y + 11.25f,
               hinge.transform.rotation.eulerAngles.z) , duration).SetUpdate(true);
         
         isHolding = true;
      }

      // 버튼에서 손을 떼었을 때
      public void OnPointerUp(PointerEventData eventData)
      {
         playerCam.transform.DOKill();
         hinge.transform.DOKill();
         
         playerCam.transform.DOMoveZ(originalPosZ, duration).SetEase(Ease.OutQuad).SetUpdate(true);
         
         hinge.transform.DORotate(new Vector3(
            hinge.transform.rotation.eulerAngles.x,
            originalRotY,
            hinge.transform.rotation.eulerAngles.z) , duration).SetUpdate(true);
         
         isHolding = false;

         if (isColpoTime)
         {
            ExitColpoTime();
         }
      }  

      private void OnDestroy()
      {
         // 오브젝트가 파괴될 때 트윈 잔재 제거 (메모리 관리)
         playerCam.transform.DOKill();
         hinge.transform.DOKill();
         DOTween.Kill(fullscreenMaterial);
      }
      
      //버튼을 누르는 동안에
      private void OnHold()
      {
         //임시 스크립트 시스템 완성되면 연동할꺼
         timer += Time.deltaTime;
         if (timer >= 0.01f)
         {
            timer = 0f; // 타이머 리셋
            current += 1;
            if (current < colpoLimit)
            {
               Debug.Log(current);
            }
         }

         if (current % 100 == 0)//나중에 천만으로 바꿀꺼임
         
         {
            //빨간 이펙트
            RedEffect();   
         }

         if ((colpoLimit / 10 * 8) <= current && !is80)
         {
            is80 = true;
            //심장 소리
         }

         if (current >= colpoLimit && !isColpoTime)
         {
            StartCoroutine(EnterColpoTime());
         }
         
         handle.transform.Rotate(Vector3.up, 90 * Time.deltaTime, Space.Self);
      }

      private IEnumerator EnterColpoTime()
      {
         isColpoTime = true;
         yield return new WaitForSeconds(colpoTime);
         isColpoTime = false;
      }

      private void ExitColpoTime()
      {
         isColpoTime = false;
         Debug.Log("Colpo!");
         current = 0;
         is80 = false;
      }

      private void RedEffect()
      {
         DOTween.Kill(fullscreenMaterial);
         fullscreenMaterial.DOFloat(originalRedTransparent, "_Transparent", 0.2f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo);
      }

      private void OnDisable()
      {
         fullscreenMaterial.SetFloat("_Transparent", originalRedTransparent);
      }
}
