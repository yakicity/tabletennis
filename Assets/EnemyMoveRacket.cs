using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyMoveRacket : MonoBehaviour
{
    private GameObject ball;
    private Rigidbody rb;
    private Rigidbody ballRb;

    private Vector3 returnDirection = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private float returnSpeed = 5f;
    private float moveSpeed = 3.5f;    // ラケットの移動速度
    private BallMovement ballMovement;
    private float xTarget =  -1.70f;
    private float zTarget;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 衝突判定を連続的にする
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        ball = GameObject.Find("Ball");
        ballRb = ball.GetComponent<Rigidbody>();
        ballMovement = ball.GetComponent<BallMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        zTarget = ball.transform.position.z;
    }

    void FixedUpdate()
    {
        AdjustPositionToBall(transform.position.x); // ラケットの位置をボールに合わせる
    }


    void AdjustPositionToBall(float targetX)
    {
        if (ball == null || ballRb == null || ballMovement == null)
            return;

        Vector3 pos = transform.position;
        pos.z = ball.transform.position.z;

        float? predictedY = ballMovement.SimulateUntilX(ball.transform.position, ballRb.linearVelocity, targetX);

        if (ballRb.linearVelocity.x > 0)
        {
            pos.y = predictedY ?? pos.y;  // ラケットのy座標を, 弾の軌道から予測したy座標に設定する. predictedYがnullだったらラケットの位置はそのまま.
            rb.MovePosition(pos);
        }
    }

    // // return directionを決定する
    // Vector3 CalculateReturnDirection(Vector3 currentBallPosition)
    // {
    //     Vector3 targetPosition = new Vector3(xTarget, currentBallPosition.y, zTarget); // Xはラケットの現在位置、Yはボールの高さを使う
    //     Debug.Log("targetPosition: " + targetPosition);
    //     // ボールから目標地点へのベクトルを計算し、正規化する
    //     Vector3 direction = (targetPosition - currentBallPosition).normalized;
    //     Debug.Log("direction: " + (targetPosition - currentBallPosition));
    //     // X方向は常にプレイヤーから相手コート方向へ、Y方向は少し上向きになるように調整
    //     // direction.x = Mathf.Abs(direction.x); // 常に相手コート方向（-X）
    //     direction.y = Mathf.Max(direction.y, 0.2f); // 最低でも少し上向きに
    //     return direction.normalized; // 再度正規化
    // }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (ballRb != null || ballMovement == null)
                return;

            ballMovement.ApplyDriveSpin(); // ドライブ回転をかける
            ballRb.linearVelocity = returnDirection * returnSpeed; // 速さを与えて山なりにボールを返す
            Debug.Log("AIが返球しました");
        }
    }
}
