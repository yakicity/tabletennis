using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyMoveRacket : MonoBehaviour
{
    private GameObject ball;
    private Rigidbody rb;
    private Rigidbody ballRb;

    private BallMovement ballMovement;


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
    void Update() { }

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

}
