using UnityEngine;

public class DebuffCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Ground")
        {
            Destroy(gameObject);
        }
        else if(collision.gameObject.name == "Player")
        {
            Destroy(gameObject);
        }
    }
}
