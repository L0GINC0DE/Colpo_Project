using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _01_Scripts.LSO
{
    public class LSO_ColpoManager : MonoBehaviour
    {
        public static LSO_ColpoManager Instance;
        
        /// <summary>
        /// 콜포 결과를 알려주는 이벤트
        /// </summary>
        /// <param name="ColpoResultType">콜포 결과</param>
        /// <param name="Money">턴 돈 양</param>
        /// <param name="GangType">갱단 종류</param>
        public static Action<string, int, string> OnColpoResult;
        
        public int ColpoMax{get; private set;}
        public int ColpoMin{get; private set;}
        public int ColpoLimit{get; private set;}
        
        public int ColpoTime{get; private set;}
        
        public int ColpoValue{get; private set;}

        public bool isColpoTime;

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

        public int ColpoMaxUp(int value)
        {
            ColpoMax += value;
            
            return ColpoMax;
        }

        public int ResetColpo()
        {
            ColpoLimit = Random.Range(ColpoMin, ColpoMax);
            
            return ColpoLimit;
        }
        
        //public int 
    }
}