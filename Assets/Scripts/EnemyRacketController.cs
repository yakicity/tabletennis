using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class EnemyRacketController : BaseRacketController
{
    private EnemyAIBase enemyAI;
    protected override void Start()
    {
        base.Start(); // BaseRacketControllerのUpdate()を実行
        enemyAI = GetComponent<EnemyAIBase>();
        if (enemyAI == null)
            Debug.LogError("EnemyAIBase がアタッチされていません！");
    }
    // FixedUpdate
    void FixedUpdate()
    {
        AdjustPositionToBall(transform.position.x); // ラケットの位置をボールに合わせる
    }

    // baseクラスのものは、味方のラケット用のAdjustPositionToBallである。オーバーライドする必要がある。
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
    
    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        BallMovement ballMovement = collision.gameObject.GetComponent<BallMovement>();

        // 敵CPUがラケットの傾きや速度を調整する傾きや速度を調整する
        enemyAI.AdjustRacketBeforeReturn(gameObject, rb);

        // ラケットの傾きや速さ, 現在のボールの速さや回転から, 返球速度やボールの回転速度を計算する
        var returnData = ballMovement.CalculateBallReturn(gameObject, collision);

        // 返球速度
        Vector3 returnVelocity = new Vector3
        {
            x = returnData.Item1.x * -1,
            y = returnData.Item1.y,
            z = enemyAI.CalculateReturnVelocityZ(rb.transform.position.z)
        };

        // 返球するボールの回転速度
        Vector3 returnAnglarVelocity = returnData.Item2;

        // ボールに計算結果を適用する
        ballMovement.ApplyReturn(returnVelocity, returnAnglarVelocity);
    } 
}
