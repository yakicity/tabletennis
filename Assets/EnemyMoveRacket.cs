using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class EnemyMoveRacket : BaseRacketController
{
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

}
