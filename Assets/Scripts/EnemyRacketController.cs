using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
public enum EnemyAILevel
{
    Level1,
    Level2
}
public class EnemyRacketController : BaseRacketController
{
    [SerializeField] private EnemyAILevel aiLevel = EnemyAILevel.Level1;

    private EnemyAIBase enemyAI;
    protected override void Start()
    {
        base.Start(); // BaseRacketControllerのUpdate()を実行
        switch (aiLevel)
        {
            case EnemyAILevel.Level1:
                enemyAI = gameObject.AddComponent<EnemyAILevel1>();
                break;
            case EnemyAILevel.Level2:
                enemyAI = gameObject.AddComponent<EnemyAILevel2>();
                break;
            default:
                Debug.LogError("Unknown AI Level");
                break;
        }
        if (enemyAI == null)
            Debug.LogError("EnemyAIBase の取得に失敗しました！");
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
        Debug.Log($"Enemy Return Velocity: {returnVelocity}, Angular Velocity: {returnAnglarVelocity}");

        // ボールに計算結果を適用する
        ballMovement.ApplyReturn(returnVelocity, returnAnglarVelocity);
    } 
}
