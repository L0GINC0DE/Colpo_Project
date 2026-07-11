using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LSO_PlayerMovement : MonoBehaviour
{
   [SerializeField] private float moveSpeed = 100;
   private Rigidbody2D _rigid;
   private float moveDir;

   private void Awake()
   {
      _rigid = GetComponent<Rigidbody2D>();
   }

   private void Update()
   {
      _rigid.linearVelocityX = moveDir * moveSpeed;
   }

   private void OnMove(InputValue value)
   {
      moveDir = value.Get<Vector2>().x;
   }
}
