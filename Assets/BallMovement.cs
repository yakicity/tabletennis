using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using System;
using UnityEditor.Callbacks;
using System.Net.NetworkInformation;

public class BallMovement : MonoBehaviour
{
    /**
    * ボールやボールの動きに関するパラメータ
    */
    private const float MassValue = 0.0027f; // 卓球ボールの重さ（kg）
    private const float LinearDampingValue = 0f; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
    private const float AngularDampingValue = 0f; // 回転空気抵抗（摩擦）
    private const float MagnusForceScale = 1e-7f; // マグナス力のスケーリング（適当な試行値、0.001〜0.01くらい）


    /**
    * ボールの回転や飛ぶ方向に関するパラメータ
    */
    private const float RubberPower = 20f; // ラバーによる回転量の増加率
    private const float NormalTuningVelocityY = 1.0f; // ボールがネットより高い時のY方向速度調整
    private const float LoopTuningVelocityY = 6f; // ボールがネットより低い時のY方向速度調整
    private const float TuningVelocityX = 1.5f; // ボールのX方向速度の調整
    private const float TunignAngle = 1.0f; // ラケット傾きによる飛距離補正
    private const float NetHeight = 0.94f; // ネットの高さ （Y座標）
    private float targetHeight = NetHeight + 0.2f; // ボールが狙う高さ (ネットより少し高め)
    private const float RacketMinSpeed = 2.5f; // ラケットの最低限の速さ


    /**
    * ボールの軌道を予測するために用いるパラメータや変数
    */
    private const float StandY = 0.785f; // 台の高さ
    private const float Bounciness = 1.0f;// 跳ね返りの強さ
    private const int MaxSteps = 50; // 予測のための計算回数
    private const float TimeStep = 0.02f; // 予測する時間幅

    /**
    * ボールの Rigidbody
    */
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // ボールやボールの動きに関するパラメータ設定
        rb.useGravity = false; // 最初は重力を無効にする
        rb.mass = MassValue; // 卓球ボールの重さ（kg）
        rb.linearDamping = LinearDampingValue; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
        rb.angularDamping = AngularDampingValue; // 回転空気抵抗（摩擦）

        // ボールのすり抜け対策
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        ApplyMagnusEffect(); // マグナス力の適用
    }

    void Update()
    {
        // スピンの向き（赤線）を可視化
        Debug.DrawRay(transform.position, rb.angularVelocity.normalized * 0.5f, Color.red);

        // 現在の速度の方向（青線）も表示してみると面白い
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f, Color.blue);
        // Debug.Log("AngularVelocity" + rb.angularVelocity);
    }
    void ApplyMagnusEffect()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 spin = rb.angularVelocity;

        // Magnus 力 = 回転ベクトル × 速度ベクトル（スケーリングあり）
        Vector3 magnusForce = Vector3.Cross(spin, velocity) * MagnusForceScale;
        rb.AddForce(magnusForce, ForceMode.Force);
    }

    // ボールの回転による「ずれ」を適用する処理
    void HandleBatCollision(Collision collision)
    {
        Vector3 spin = rb.angularVelocity; // 現在のボールの回転
        Vector3 normal = collision.contacts[0].normal; // 接触点の砲戦

        // 回転によル上下方向の力 (ずれる方向)
        Vector3 spinDir = Vector3.Cross(spin, normal).normalized; // ずれる方向の単位ベクトル
        float spinMagnitude = spin.magnitude; // 回転量の大きさ
        Vector3 horizontalForce = spinDir * spinMagnitude; // 実際に適用する力ベクトル

        // TODO: ラケットの動きによるボールの速さ
        // Vector3 racketImpact = Vector3.Project(racketRb.linearVelocity, normal); // ラケットの動きによりボールがずれる方向
        // Debug.Log($"racketImpact: {racketImpact}");


        // 回転によるずれをボールの速度に加算
        rb.linearVelocity += horizontalForce;
        // rb.linearVelocity = horizontalForce + racketImpact;

        Debug.Log($"spin: {spin}, normal: {normal}, spinDir: {spinDir}, SpinMagnitude: {spinMagnitude},horizontalForce: {horizontalForce} rb.LinearVelocity: {rb.linearVelocity}");
    }

    // ラケットの擦りによってボールに回転を与える処理
    void HandleBatSpinCollision(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;
        GameObject racket = collision.gameObject;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();
        Vector3 racketVelocity = racketRb.linearVelocity;

        // ラケットの接線成分 (擦っている方向)
        Vector3 tangentialVel = Vector3.ProjectOnPlane(racketVelocity, normal);
        // 回転軸（スピン方向） = 接線 × 法線
        Vector3 spinDir = Vector3.Cross(tangentialVel, normal).normalized;
        // 回転の強さ = 接線速度の大きさ × スピン係数
        float spinAmount = tangentialVel.magnitude * RubberPower;
        // 回転をZ軸 (上下回転)にだけ反映
        rb.angularVelocity = new Vector3(rb.angularVelocity.x,rb.angularVelocity.y, spinDir.z * spinAmount);

        Debug.Log($"[Spin] tangentialVel: {tangentialVel}, spinDir: {spinDir}, spinAmount: {spinAmount}, rb.angularVelocity: {rb.angularVelocity}");
    }
    void AdjustTrajectory(Collision collision)
    {
        GameObject racket = collision.gameObject;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();

        // Y方向速度 (ネットとの位置関係から決定)
        float ballHeight = gameObject.transform.position.y;
        float yDifference = targetHeight - ballHeight; // 現在のボールの位置と狙う位置の高さの差
        float tuningVelocityY = yDifference < 0f ? NormalTuningVelocityY : LoopTuningVelocityY; // ネットよりボールが低い場合にはループ気味に打つ
        float velocityY = yDifference * tuningVelocityY; 

        // X方向速度 (ラケットの傾きと速度から決定)
        float racketRotationX = Mathf.DeltaAngle(0f, collision.gameObject.transform.eulerAngles.x) ;
        float angleFactor = 1f - Mathf.Abs(racketRotationX + 90f) / 90f + TunignAngle; 

        // ラケットの速さ: ラケットの動きが速いほどボールが飛び, ラケットの動きが遅いほどボールが飛ばない
        // float speedFactor = racketRb.linearVelocity.magnitude;
        float actualRacketSpeed = Mathf.Abs(racketRb.linearVelocity.x); 
        float speedFactor = Mathf.Max(actualRacketSpeed, RacketMinSpeed); // ラケットが2.5fより速く動いていたらそれを適用, それ以下だったら2.5fの速さをボールに与える
        float velocityX = angleFactor * speedFactor * TuningVelocityX;

        if (racket.CompareTag("EnemyBat")) // Enemy が打つ時はX方向速度が逆になる
            velocityX *= -1;

        rb.linearVelocity = new Vector3(velocityX, velocityY, 0f); // ボールに速さを適用

        Debug.Log($"[Flight] ballY: {ballHeight}, velocityY: {velocityY}, racketAngleX: {racketRotationX}, angleFactor:{angleFactor}, speedFactor: {speedFactor}, finalVelocity: {rb.linearVelocity}");
    }

    // ボールの数秒先の軌道を予測する関数
    public float? SimulateUntilX(Vector3 startPos, Vector3 startVel, float targetX, List<Vector3> trajectoryPoints = null)
    {
        Vector3 currentPos = startPos;
        Vector3 currentVel = startVel;
        for (int i = 0; i < MaxSteps; i++)
        {
            // 次の位置を計算
            currentVel += Physics.gravity * TimeStep;
            Vector3 nextPos = currentPos + currentVel * TimeStep;

            // バウンド判定
            if (IsBounceTriggered(currentPos, nextPos))
            {
                currentPos.y = StandY;
                currentVel.y = -currentVel.y * Bounciness;
                nextPos = currentPos + currentVel * TimeStep;
            }
            // 弾の軌跡のデータを追加
            trajectoryPoints?.Add(currentPos);

            // X方向に targetX を通過した場合
            if (HasCrossedTargetX(currentPos.x, nextPos.x, targetX))
            {
                float lerpedY = Mathf.Lerp(currentPos.y, nextPos.y, Mathf.InverseLerp(nextPos.x, currentPos.x, targetX));
                return Mathf.Max(StandY, lerpedY);
            }
            currentPos = nextPos;
        }
        return null;
    }

    // バウンド判定
    private bool IsBounceTriggered(Vector3 currentPos, Vector3 nextPos){
        return currentPos.y > StandY && nextPos.y <= StandY;
    }

    // X方向の目標到達判定
    private bool HasCrossedTargetX(float currentX, float nextX, float targetX){
        return (currentX - targetX) * (nextX - targetX) <= 0f;
    }

    void OnCollisionEnter(Collision collision)
    {
        // ラケットがボールに触れたら重力を付与
        rb.useGravity = true;

        // デバッグ用に敵のラケットが当たった時だけ
        if (collision.gameObject.CompareTag("EnemyBat")){
            Debug.Log(collision.gameObject.name);
            HandleBatCollision(collision);
            // AdjustTrajectory(collision);
        }
        if (collision.gameObject.CompareTag("PlayerBat")){
            Debug.Log(collision.gameObject.name);
            HandleBatSpinCollision(collision);
            AdjustTrajectory(collision);
        }
    }

}
