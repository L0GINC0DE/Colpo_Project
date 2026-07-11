using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _01_Scripts.LSO
{
    public class LSO_ColpoSystem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private LSO_ColpoManager colpoManager;
        private float timer;
        private const float TickInterval = 0.01f;

        public LDY_GangConfig data;
        private LDY_GangType gangType;

        private bool resultAlreadyGiven;
        private Coroutine colpoTimeCoroutine; // ← 코루틴 참조 추적

        private void Start()
        {
            gangType = data.type;
            colpoManager = LSO_ColpoManager.Instance;
        }

        private void Update()
        {
            if (colpoManager.holding)
            {
                timer += Time.deltaTime;
                if (timer >= TickInterval)
                {
                    timer = 0f;
                    colpoManager.CurrentUp();
                }

                if (colpoManager.Current >= colpoManager.ColpoLimit && !colpoManager.isColpoTime)
                {
                    colpoTimeCoroutine = StartCoroutine(EnterColpoTime());
                }

                if (colpoManager.ColpoTimeEnd && !resultAlreadyGiven)
                {
                    ExitColpoTime(LSO_ColpoManager.ColpoResultType.Fail);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            resultAlreadyGiven = false;
            colpoManager.HandlePointerDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            colpoManager.HandlePointerUp();

            if (resultAlreadyGiven)
            {
                return;
            }

            if (colpoManager.isColpoTime)
            {
                ExitColpoTime(LSO_ColpoManager.ColpoResultType.Colpo);
            }
            else
            {
                ExitColpoTime(LSO_ColpoManager.ColpoResultType.Normal);
            }
        }

        private IEnumerator EnterColpoTime()
        {
            colpoManager.isColpoTime = true;
            yield return new WaitForSeconds(colpoManager.ColpoTime);
            colpoManager.isColpoTime = false;
            colpoManager.ColpoTimeEnd = true;
        }

        private void ExitColpoTime(LSO_ColpoManager.ColpoResultType colpoResultType)
        {
            // 아직 안 끝난 코루틴이 있다면 여기서 확실히 멈춤 (다음 판에 지연 발동되는 것 방지)
            if (colpoTimeCoroutine != null)
            {
                StopCoroutine(colpoTimeCoroutine);
                colpoTimeCoroutine = null;
            }

            resultAlreadyGiven = true;

            colpoManager.isColpoTime = false;
            colpoManager.ColpoTimeEnd = false;

            LSO_ColpoManager.OnColpoResult?.Invoke(colpoResultType, colpoManager.Current, gangType);
            colpoManager.ResetColpo();

            if (colpoManager.holding)
            {
                colpoManager.HandlePointerUp();
            }

            //Debug.Log(colpoResultType.ToString());
        }
    }
}