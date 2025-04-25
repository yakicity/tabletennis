using UnityEngine;

public class MoveRacket : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.AddForce(-20000.0f, 0.0f, 0.0f); // 加える力のベクトルをVectorで入れる
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(0, 0, 0);
        }
    }

}
