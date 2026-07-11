using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _01_Scripts.LSO
{
    public class LSO_ColpoManager : MonoBehaviour
    {
        public static LSO_ColpoManager Instance;
        
        // 누르기/떼기 이벤트
        public static event Action OnColpoPointerDown;
        public static event Action OnColpoPointerUp;
        
        /// <summary>
        /// 콜포 결과를 알려주는 이벤트
        /// </summary>
        public static Action<ColpoResultType, int, LDY_GangType> OnColpoResult;

        public int ColpoMax { get; private set; } = 10;
        private int ColpoMin { get; set; } = 0;
        public int ColpoLimit{get; private set;}

        public float ColpoTime { get; private set; } = 0.7f;
        
        public int Current{get; private set;}

        public bool isColpoTime;
        public bool holding;
        public bool colpoTimeEnd;

        public enum ColpoResultType
        {
            Colpo,
            Normal,
            Fail
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
        
        public void HandlePointerDown()
        {
            ResetColpo();
            holding = true;
            OnColpoPointerDown?.Invoke();
        }

        public void HandlePointerUp()
        {
            holding = false;
            OnColpoPointerUp?.Invoke();
        }

        public int ColpoMaxUp(int value)
        {
            ColpoMax += value;
            
            return ColpoMax;
        }

        public int ResetColpo()
        {
            ColpoLimit = Random.Range(ColpoMin, ColpoMax + 1) * 100;
            Current = 0;
            
            return ColpoLimit;
        }

        public int CurrentUp(int value = 1)
        {
            Current += value;
            return Current;
        }
    }
}