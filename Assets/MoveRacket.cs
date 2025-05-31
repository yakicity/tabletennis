using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class MoveRacket : MonoBehaviour
{
    private float moveSpeed = 3.0f;    // ラケットの移動速度
    private GameObject ball;

    private Vector3 initialPosition;

    private Vector3 normalRotationVector = new Vector3(-90f, -90f, 180f); // 通常のラケットの向き
    private Vector3 driveRotationVector = new Vector3(-100f, -90f, 180f); // 少し下向きに傾ける
    private Vector3 cutRotationVector = new Vector3(-70f, -90f, 180f); // 少し上向きに傾ける
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

    // 衝突(離散)
    private Vector3 returnDirectionNormal = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private Vector3 returnDirectionDrive = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private Vector3 returnDirectionCut = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private Vector3 returnDirectionRight = new Vector3(-0.3f, 0.2f, 0.2f).normalized;
    private Vector3 returnDirectionLeft = new Vector3(-0.3f, 0.2f, -0.2f).normalized;
    private float returnSpeed = 3f;
    private float lastTapTime = 0f;
    private bool isDoubleTap = false;
    private float doubleTapThreshold = 0.2f;
    private float pressStartTime = 0f;
    private bool isHolding = false;
    private float holdDuration = 0f;
    private float boostSpeed;
    private float timeScale;

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
        // rb.isKinematic =true; // ラケットの物理演算を無効にする

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
        
        timeScale = Time.timeScale;
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
        // UpdateRotation();   // ラケットの向きを更新
        UpdateRotationDiscrete(); // ラケットの向きを離散的に更新

    }
    void HandleInput()
    {
        moveInput = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.C))
        {
            float now = Time.time;
            Debug.Log(Time.time);

            // ダブルタップ判定
            if (lastTapTime > 0f && (now - lastTapTime)  / timeScale <= doubleTapThreshold)
            {
                isDoubleTap = true;
                Debug.Log("Double Tap Detected!");
            }
            else
            {
                isDoubleTap = false;
            }

            lastTapTime = now;

            // 押し始め時間記録
            pressStartTime = now;
            isHolding = true;
        }
        // --- 離した時 ---
        if (Input.GetKeyUp(KeyCode.C))
        {
            if (isHolding)
            {
                holdDuration = Time.time - pressStartTime;
                Debug.Log($"W Hold Duration: {holdDuration}");

                if (isDoubleTap)
                {
                    boostSpeed = Mathf.Clamp(holdDuration * 5f, 1f, 10f);
                    rb.linearVelocity = new Vector3(boostSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
                    Debug.Log($"Boost Applied: {boostSpeed}");
                }
            }
            isDoubleTap = false;
            isHolding = false;
        }
        if (!isDoubleTap) // ため中は完全停止！
        {
            if (Input.GetKey(KeyCode.W)) moveInput.x += 1;
            if (Input.GetKey(KeyCode.S)) moveInput.x -= 1;
            if (Input.GetKey(KeyCode.A)) moveInput.z += 1;
            if (Input.GetKey(KeyCode.D)) moveInput.z -= 1;
        }
        // else
        // {
        //     // ため中は Debug 表示（確認用）
        //     Debug.Log("In Double Tap (Charging), movement disabled.");
        // }
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

    void UpdateRotationDiscrete()
    {
        if (Input.GetKey(KeyCode.UpArrow)) transform.rotation = driveRotation;
        else if (Input.GetKey(KeyCode.DownArrow)) transform.rotation = cutRotation;
        else if (Input.GetKey(KeyCode.RightArrow)) transform.rotation = rightRotation;
        else if (Input.GetKey(KeyCode.LeftArrow)) transform.rotation = leftRotation;
        else transform.rotation = normalRotation;
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

    void OnCollisionEnter(Collision collision){
        // ballMovement.ApplyDriveSpin();
        // ballMovement.ApplyCutSpin();
    }
}
