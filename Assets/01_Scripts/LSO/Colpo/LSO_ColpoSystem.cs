using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _01_Scripts.LSO
{
    public class LSO_ColpoSystem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private LSO_ColpoManager colpoManager;
        private float timer;
        private const float TickInterval = 0.15f;

        public LDY_GangData data;
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
                    
                  double cur =  colpoManager.CurrentUp(GetStep(colpoManager.Current));
                    
                   LSO_MoneyManager.Instance.AddMoney(cur);
                }

                if (colpoManager.Current >= colpoManager.ColpoLimit && !colpoManager.isColpoTime)
                {
                    colpoTimeCoroutine = StartCoroutine(EnterColpoTime());
                }

                if (colpoManager.colpoTimeEnd && !resultAlreadyGiven)
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

            ExitColpoTime(colpoManager.isColpoTime
                ? LSO_ColpoManager.ColpoResultType.Colpo
                : LSO_ColpoManager.ColpoResultType.Normal);
        }

        private IEnumerator EnterColpoTime()
        {
            colpoManager.isColpoTime = true;
            yield return new WaitForSeconds(colpoManager.ColpoTime);
            colpoManager.isColpoTime = false;
            colpoManager.colpoTimeEnd = true;
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
            colpoManager.colpoTimeEnd = false;

            double resultMoney = 0;
            
            switch (colpoResultType.ToString())
            { 
                case "Normal":
                    resultMoney = colpoManager.Current;
                    break;
                case "Fail":
                    resultMoney = colpoManager.Current * 0;
                    break;
                case "Colpo":
                    resultMoney = colpoManager.Current;
                    break;
                default:
                    Debug.LogError("이상한 값: " + colpoResultType.ToString());
                    break;
            }
            
            
            colpoManager.ResetColpo();
            LSO_ColpoManager.OnColpoResult?.Invoke(colpoResultType, resultMoney, gangType);
            
            if (colpoManager.holding)
            {
                colpoManager.HandlePointerUp();
            }

            //Debug.Log(colpoResultType.ToString());
        }
        
        private int GetStep(double value)
        {
            if (value <= 0) return 1;

            int step = 1;
            while (value >= 10)
            {
                value /= 10;
                step *= 10;
            }
            return step;
        }
    }
}