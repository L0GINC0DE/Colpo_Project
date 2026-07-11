using System.Runtime.CompilerServices;
using UnityEngine;

public class MoneySystem : MonoBehaviour
{
    [SerializeField]private int curMoney;

    public static MoneySystem instance;
    private void Awake()
    {
        instance = this;
    }

    public void Add(int money)=>curMoney += money;
    
    public void Subtraction(int money)=> curMoney -= money;
   
}
