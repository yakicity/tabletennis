using UnityEngine;
using UnityEngine.Rendering;

public class MoveRacket : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.0f;    // ラケットの移動速度
    [SerializeField] private float adjustDistance = 0.03f;  // ボールとの距離調整のための距離
    [SerializeField] private GameObject ball;

    [SerializeField] private Vector3 normalRotationVector = new Vector3(-80f, -90f, 180f); // 通常のラケットの向き
    [SerializeField] private Vector3 cutRotationVector = new Vector3(-50f, -90f, 180f); // 少し下向きに傾ける

    // 衝突時
    [SerializeField] private Vector3 racketVelocityAtCollision = new Vector3(3f, 0f, 0f); // 衝突時のラケットの速度
    [SerializeField] private float reflectScale = 0.3f; // 反射の強さ
    [SerializeField] private float racketImpactScale = 1.5f; // ラケットの勢いで押し出す強さ

    private Quaternion normalRotation;  // 通常のラケットの向き
    private Quaternion cutRotation;     // カットスピンのラケットの向き
    Rigidbody rb;
    private Vector3 moveInput = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        normalRotation = Quaternion.Euler(normalRotationVector);
        cutRotation = Quaternion.Euler(cutRotationVector);

        // 衝突判定を連続的にする
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput(); // キー入力を取得
        AdjustPositionToBall(); // ラケットの位置をボールに合わせる
        UpdateRotation();   // ラケットの向きを更新
    }
    void FixedUpdate()
    {
        // キー入力がある時だけ速度を与え、ない時は止める
        rb.linearVelocity = moveInput * moveSpeed;
    }
    void HandleInput()
    {
        moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) moveInput.z += 1;
        if (Input.GetKey(KeyCode.D)) moveInput.z -= 1;
        if (Input.GetKey(KeyCode.W)) moveInput.x += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.x -= 1;
    }
    // ラケットの向きを更新
    // カットスピンの時はラケットを少し下向きに傾ける
    void UpdateRotation()
    {
        transform.rotation = Input.GetKey(KeyCode.C) ? cutRotation : normalRotation;
    }

    void AdjustPositionToBall()
    {
        // ラケットとボールの距離が
        float dist = Vector3.Distance(transform.position, ball.transform.position);
        if (dist < adjustDistance)
        {
            Vector3 pos = transform.position;
            pos.y = ball.transform.position.y;
            pos.z = ball.transform.position.z;
            transform.position = pos;
        }
        // Debug.Log("Racket Position: " + racketTransform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            Rigidbody racketRb = GetComponent<Rigidbody>();

            if (ballRb != null)
            {
                Vector3 racketVelocity = racketRb.linearVelocity; // ラケットの動き
                Vector3 normal = collision.contacts[0].normal; // 接触面の法線
                Vector3 incomingVelocity = ballRb.linearVelocity;   // ボールの動き

                // 「ラケットの速度方向」と「法線」の加味
                Vector3 finalVelocity = Vector3.Reflect(incomingVelocity, normal) * reflectScale// 物理的反射
                                    // + racketVelocity * 2f; // ラケットの勢いで押し出す
                                    + racketVelocityAtCollision *  racketImpactScale; // 衝突時ラケットの勢い(固定)で押し出す
                // Debug.Log(finalVelocity);

                ballRb.linearVelocity = finalVelocity;
                // Debug.Log(racketVelocity);

                BallMovement ballMovement = collision.gameObject.GetComponent<BallMovement>();
                if (ballMovement != null)
                {
                    if (transform.rotation == cutRotation)
                    {
                        Debug.Log("カットスピンの条件: ");
                        ballMovement.ApplyCutSpin();
                    }
                    else
                    {
                        Debug.Log("ドライブ: ");
                        ballMovement.ApplyDriveSpin();
                    }
                }
            }
        }
    }
}