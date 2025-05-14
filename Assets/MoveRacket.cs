using UnityEngine;
using UnityEngine.Rendering;

public class MoveRacket : MonoBehaviour
{
    private float moveSpeed = 3.0f;    // ラケットの移動速度
    private float adjustDistance = 2.0f;  // ボールとの距離調整のための距離
    private GameObject ball;

    private Vector3 normalRotationVector = new Vector3(-90f, -90f, 180f); // 通常のラケットの向き

    private Vector3 driveRotationVector = new Vector3(-110f, -90f, 180f); // 少し上向きに傾ける
    private Vector3 cutRotationVector = new Vector3(-50f, -90f, 180f); // 少し下向きに傾ける
    private Vector3 rightRotationVector = new Vector3(-90f, -90f, 210f); // 少し右向きに傾ける
    private Vector3 leftRotationVector = new Vector3(-90f, -90f, 150f); // 少し左向きに傾ける

    // 衝突時
    private Vector3 racketVelocityAtCollision = new Vector3(3f, 0f, 0f); // 衝突時のラケットの速度
    private float reflectScale = 0.4f; // 反射の強さ
    private float racketImpactScale = 1.0f; // ラケットの勢いで押し出す強さ

    private Quaternion normalRotation;  // 通常のラケットの向き
    private Quaternion driveRotation;  // ドライブのラケットの向き

    private Quaternion cutRotation;     // カットスピンのラケットの向き
    private Quaternion rightRotation;     // 右向きのラケット

    private Quaternion leftRotation;     // 左向きラケット

    Rigidbody rb;
    Rigidbody ballRb;
    private Vector3 moveInput = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        normalRotation = Quaternion.Euler(normalRotationVector);
        driveRotation = Quaternion.Euler(driveRotationVector);
        cutRotation = Quaternion.Euler(cutRotationVector);
        rightRotation = Quaternion.Euler(rightRotationVector);
        leftRotation = Quaternion.Euler(leftRotationVector);

        // 衝突判定を連続的にする
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        ball = GameObject.Find("Ball");
        ballRb = ball.GetComponent<Rigidbody>();
        if (!ball) Debug.Log("ball is null");
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput(); // キー入力を取得
        UpdateRotation();   // ラケットの向きを更新
    }
    void FixedUpdate()
    {
        // キー入力がある時だけ速度を与え、ない時は止める
        rb.linearVelocity = moveInput * moveSpeed;
        AdjustPositionToBall(); // ラケットの位置をボールに合わせる
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
        if (Input.GetKey(KeyCode.UpArrow)) transform.rotation = driveRotation;
        if (Input.GetKey(KeyCode.DownArrow)) transform.rotation = cutRotation;
        if (Input.GetKey(KeyCode.RightArrow)) transform.rotation = rightRotation;
        if (Input.GetKey(KeyCode.LeftArrow)) transform.rotation = leftRotation;
    }

    void AdjustPositionToBall()
    {
        // ラケットとボールの距離が
        float dist = Vector3.Distance(transform.position, ball.transform.position);
        if (dist < adjustDistance && ballRb.linearVelocity.x < 0)
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
            Rigidbody racketRb = GetComponent<Rigidbody>();

            if (ballRb != null)
            {
                Vector3 racketVelocity = racketRb.linearVelocity; // ラケットの動き
                Vector3 normal = collision.contacts[0].normal; // 接触面の法線
                Debug.Log(normal);
                Vector3 incomingVelocity = ballRb.linearVelocity;   // ボールの動き

                // 「ラケットの速度方向」と「法線」の加味
                Vector3 finalVelocity = Vector3.Reflect(incomingVelocity, - normal) * reflectScale// 物理的反射
                                                                                                // + racketVelocity * 2f; // ラケットの勢いで押し出す
                                    + racketVelocityAtCollision * racketImpactScale; // 衝突時ラケットの勢い(固定)で押し出す
                // Debug.Log(finalVelocity);

                ballRb.linearVelocity = finalVelocity;
                // Debug.Log(racketVelocity);

                BallMovement ballMovement = collision.gameObject.GetComponent<BallMovement>();
                if (ballMovement != null)
                {
                    if (transform.rotation == cutRotation)
                    {
                        Debug.Log("カットスピンの条件: ");
                        // ballMovement.ApplyCutSpin();
                    }
                    else
                    {
                        Debug.Log("ドライブ: ");
                        // ballMovement.ApplyDriveSpin();
                    }
                }
            }
        }
    }
}