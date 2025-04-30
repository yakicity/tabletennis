using UnityEngine;

public class ServeManager : MonoBehaviour
{
    public Rigidbody ballRb;
    public Transform racketTransform;
    public Rigidbody racketRb;
    public Vector3 serveOffset = new Vector3(-0.1f, 0.1f, 0);
    public Vector3 swingDirection = new Vector3(-1, 1, 0);
    public float swingForce = 1f;   // スイングの移動力
    public float ballForce = 0.001f;   // スイングの移動力
    private bool isPrepareServing = false;
    private Quaternion initialRotation;
    private Vector3 initialPosition;

    void Start()
    {
        if (racketRb == null)
            racketRb = racketTransform.GetComponent<Rigidbody>();

        if (racketRb != null)
        {
            racketRb.mass = 0.18f;
            racketRb.constraints = RigidbodyConstraints.None; // 全自由
        }

        initialRotation = racketTransform.rotation;
        initialPosition = racketTransform.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PrepareServe();
        }
        if (Input.GetKeyDown(KeyCode.Space) && isPrepareServing)
        {
            StartServe();
        }
    }

    public void PrepareServe()
    {
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.isKinematic = true;

        racketRb.isKinematic = true;
        racketTransform.rotation = initialRotation;
        racketTransform.position = initialPosition;
        racketRb.isKinematic = false;

        ballRb.transform.position = racketTransform.position + serveOffset;
        isPrepareServing = true;
    }

    public void StartServe()
    {
        ballRb.isKinematic = false;
        isPrepareServing = false;

        // スイング方向
        Vector3 swingDirection_norm = swingDirection.normalized;

        // 移動させる
        racketRb.AddForce(swingDirection_norm * swingForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            Debug.Log("ラケットでボールにヒット！！");

            Vector3 forceDir = collision.contacts[0].normal;
            ballRb.AddForce(-forceDir.normalized * ballForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            Debug.Log("ラケットのヒット終了");
        }
    }
}