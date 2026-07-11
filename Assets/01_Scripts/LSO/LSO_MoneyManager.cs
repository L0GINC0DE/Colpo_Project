using _01_Scripts.LSO;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public class LSO_MoneyManager : MonoBehaviour
{
   public static LSO_MoneyManager Instance;
   
   [SerializeField] private TextMeshProUGUI moneyText;
   
   public double Money{get; private set;}

   private void Awake()
   {
      if (Instance == null)
      {
         Instance = this;
      }
   }

   private void Start()
   {
      moneyText.text = Money.ToString();
   }

   private void OnEnable()
   {
      LSO_ColpoManager.OnColpoResult += AddMoney;
   }

   private void OnDisable()
   {
      LSO_ColpoManager.OnColpoResult -= AddMoney;
   }


   public bool UseMoney(int value)
   {
      if (value >= Money)
      {
         Money -= value;
         return true;
      }
      
      return  false;
   }

   public void AddMoney(double value)
   {
      Money += value;
      moneyText.text = Money.ToString();
   }

   public void AddMoney(LSO_ColpoManager.ColpoResultType result, double value, LDY_GangType gangType)
   {
      Debug.Log($"{gangType} 갱으로 부터  {result}을/를 해 {value}달러만큼 털어왔습니다");

      if (value == 0)
      {
         Money = value;
      }
     
      moneyText.text = Money.ToString();
   }
}
