using UnityEngine;
using UnityEngine.Rendering;

public class MoveRacket : MonoBehaviour
{
    float MoveSpeed = 15000.0f;
    float adjustDistance = 2.0f;
    // ラケットの初期位置
    // Vector3 initialPos = new Vector3(-1.0f, 1.1f, -1.4f);
    Rigidbody rb;
    [SerializeField] private GameObject ball;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // y座標をボールと合わせる
        Transform racketTransform = this.transform;
        Vector3 pos = racketTransform.position;

        // ラケットとボールの距離が
        float dist = Vector3.Distance(racketTransform.position, ball.transform.position);
        if (dist < adjustDistance) {
            pos.y = ball.transform.position.y;

            // 後で消すかも
            pos.z = ball.transform.position.z;

            racketTransform.position = pos;
        }


        if (Input.GetKeyDown(KeyCode.A))
        {
            rb.AddForce(0.0f, 0.0f, MoveSpeed); // 加える力のベクトルをVectorで入れる
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            rb.linearVelocity = new Vector3(0, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            rb.AddForce(MoveSpeed, 0.0f, 0.0f); // 加える力のベクトルをVectorで入れる
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            rb.linearVelocity = new Vector3(0, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            rb.AddForce(-MoveSpeed, 0.0f, 0.0f); // 加える力のベクトルをVectorで入れる
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            rb.linearVelocity = new Vector3(0, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            rb.AddForce(0.0f, 0.0f, -MoveSpeed); // 加える力のベクトルをVectorで入れる
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            rb.linearVelocity = new Vector3(0, 0, 0);
        }
    }

}
