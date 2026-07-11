using UnityEngine;
using UnityEngine.TestTools;

public class DebuffHackingMovement : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 3f;

    private Rigidbody rb;
    private float inputX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // 입력은 Update에서 매 프레임 받아야 놓치지 않음
        inputX = Input.GetAxisRaw("Horizontal"); // A: -1, D: 1
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        Vector3 nextPos = rb.position + Vector3.right * inputX * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }

}
