using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.Rendering;

public class MoveRacket : MonoBehaviour
{
    private float moveSpeed = 3.5f;    // ラケットの移動速度
    private GameObject ball;

    private Vector3 initialPosition;

    private Vector3 normalRotationVector = new Vector3(-90f, -90f, 180f); // 通常のラケットの向き
    private Vector3 driveRotationVector = new Vector3(-110f, -90f, 180f); // 少し上向きに傾ける
    private Vector3 cutRotationVector = new Vector3(-50f, -90f, 180f); // 少し下向きに傾ける
    private Vector3 rightRotationVector = new Vector3(-90f, -90f, 210f); // 少し右向きに傾ける
    private Vector3 leftRotationVector = new Vector3(-90f, -90f, 150f); // 少し左向きに傾ける
    private Quaternion normalRotation;  // 通常のラケットの向き
    private Quaternion driveRotation;  // ドライブのラケットの向き

    private Quaternion cutRotation;     // カットスピンのラケットの向き
    private Quaternion rightRotation;     // 右向きのラケット

    private Quaternion leftRotation;     // 左向きラケット

    // 衝突時
    private Vector3 racketVelocityAtCollision = new Vector3(3f, 0f, 0f); // 衝突時のラケットの速度
    private float reflectScale = 0.4f; // 反射の強さ
    private float racketImpactScale = 1.0f; // ラケットの勢いで押し出す強さ

    private Vector3 moveInput = Vector3.zero;
    Rigidbody rb;
    Rigidbody ballRb;
    BallMovement ballMovement;
    LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        initialPosition = transform.position;

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
        ballMovement = ball.GetComponent<BallMovement>();
        lineRenderer = ball.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput(); // キー入力を取得
    }
    void FixedUpdate()
    {
        // キー入力がある時だけ速度を与え、ない時は止める
        rb.linearVelocity = moveInput * moveSpeed;
        AdjustPositionToBall(transform.position.x); // ラケットの位置をボールに合わせる
        UpdateRotation();   // ラケットの向きを更新

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
        if (Input.GetKey(KeyCode.UpArrow)) transform.rotation *= Quaternion.Euler(5f, 0f, 0f);
        if (Input.GetKey(KeyCode.DownArrow)) transform.rotation *= Quaternion.Euler(-5f, 0f, 0f);
        if (Input.GetKey(KeyCode.RightArrow)) transform.rotation *= Quaternion.Euler(0f, 0f, 5f);
        if (Input.GetKey(KeyCode.LeftArrow)) transform.rotation *= Quaternion.Euler(0f, 0f, -5f);
        if (Input.GetKey(KeyCode.C)) transform.rotation = normalRotation;
    }

    void AdjustPositionToBall(float targetX)
    {
        Vector3 pos = transform.position;
        List<Vector3> points = new();

        float? predictedY = ballMovement.SimulateUntilX(ball.transform.position, ballRb.linearVelocity, targetX, points);

        bool ballToPlayer = ballRb.linearVelocity.x < 0;
        if (ballToPlayer)
        {
            // ラケットのy座標を, 弾の軌道から予測したy座標に設定する. predictedYがnullだったらラケットの位置はそのまま.
            pos.y = predictedY ?? pos.y;
            rb.MovePosition(pos);
        }

        // 弾がプレイヤー方向に来ている場合, 弾の軌跡を表示する
        UpdateLineRender(points, ballToPlayer);
    }

    void UpdateLineRender(List<Vector3> points, bool ballToPlayer)
    {
        if (points == null || lineRenderer == null)
            return;
        
        if (ballToPlayer){
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            lineRenderer.enabled = true;
        }
        else
            lineRenderer.enabled = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {

            if (ballRb != null)
            {
                Vector3 racketVelocity = rb.linearVelocity; // ラケットの動き
                Vector3 normal = collision.contacts[0].normal; // 接触面の法線
                Debug.Log(normal);
                Vector3 incomingVelocity = ballRb.linearVelocity;   // ボールの動き

                // 「ラケットの速度方向」と「法線」の加味
                Vector3 finalVelocity = Vector3.Reflect(incomingVelocity, -normal) * reflectScale// 物理的反射
                                                                                                 // + racketVelocity * 2f; // ラケットの勢いで押し出す
                                    + racketVelocityAtCollision * racketImpactScale; // 衝突時ラケットの勢い(固定)で押し出す
                // Debug.Log(finalVelocity);

                ballRb.linearVelocity = finalVelocity;
                // Debug.Log(racketVelocity);

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
