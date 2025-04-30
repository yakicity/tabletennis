using UnityEngine;

public class MoveRacket : MonoBehaviour
{
    private Rigidbody rb;
    public float moveSpeed = 3f; // 移動スピード

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.A))
        {
            move += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            move += new Vector3(1, 0, 0);
        }
        if (Input.GetKey(KeyCode.W))
        {
            move += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            move += new Vector3(0, 0, -1);
        }

        rb.linearVelocity = move.normalized * moveSpeed;
    }
}
