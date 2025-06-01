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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float linearDampingValue = 0f; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
    private float angularDampingValue = 0f; // 回転空気抵抗（摩擦）
    private float massValue = 0.0027f; // 卓球ボールの重さ（kg）
    private float magnusForceScale = 1e-7f; // マグナス力のスケーリング（適当な試行値、0.001〜0.01くらい）
    private float rubberPower = 20f;
    private static float netHeight = 0.94f;
    private float targetHeight = netHeight + 0.2f; // ネットより少し高い位置を狙う
    private const float NormalTuningVelocityY = 1.0f;
    private const float LoopTuningVelocityY = 6f;
    private const float TuningVelocityX = 1.5f;
    private const float TunignAngle = 1.0f;
    private const float TuningSpinPower = 1.0f;

    private GameObject stand;
    private float standY; // ボールが跳ね返る高さ
    private const float bounciness = 1.0f;// 跳ね返りの強さ
    private int maxSteps = 50; // 予測のための計算回数
    private const float timeStep = 0.02f; // 予測する時間幅
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // 重力を無効にする

        rb.mass = massValue; // 卓球ボールの重さ（kg）
        rb.linearDamping = linearDampingValue; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
        rb.angularDamping = angularDampingValue; // 回転空気抵抗（摩擦）

        // すり抜け対策
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        stand = GameObject.Find("Stand");
        if (!stand)
            Debug.Log("stand not found");
        standY = stand.transform.position.y;
    }

    void FixedUpdate()
    {
        ApplyMagnusEffect();
    }

    // Update is called once per frame

    void Update()
    {
        // スピンの向き（赤線）を可視化
        Debug.DrawRay(transform.position, rb.angularVelocity.normalized * 0.5f, Color.red);

        // 現在の速度の方向（青線）も表示してみると面白い
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f, Color.blue);
        // Debug.Log("AngularVelocity" + rb.angularVelocity);
    }
    void OnCollisionEnter(Collision collision)
    {
        // ラケットがボールに触れたら重力を付与
        rb.useGravity = true;

        if (collision.gameObject.CompareTag("PlayerBat")){
            
        }
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

    void HandleBatCollision(Collision collision)
    {
        Vector3 spin = rb.angularVelocity; // ボールの回転速度
        Debug.Log("spin: "+ spin);
        Vector3 normal = collision.contacts[0].normal; // ラケットの法線
        GameObject racket = collision.gameObject; 
        Rigidbody racketRb = racket.GetComponent<Rigidbody>(); 

        //TODO: 今はラケットとボールの当たり方によって変えているけど, ラケットの角度によってhorizontalForceを出しちゃってもいい気がする. そうすれば, 横に行っても横回転がかからないような気がするよね
        // ボールの回転による影響
        Vector3 spinDir = Vector3.Cross(spin, normal).normalized; // 回転によりボールがずれる方向
        float spinMagnitude = spin.magnitude;
        Vector3 horizontalForce = spinDir * spinMagnitude;

        // ラケットの動きによるボールの速さ
        // Vector3 racketImpact = Vector3.Project(racketRb.linearVelocity, normal); // ラケットの動きによりボールがずれる方向
        // Debug.Log($"racketImpact: {racketImpact}");

        // ラケットの速さと傾きによるボールの回転

        Debug.Log($"rb.LinearVelocity:{rb.linearVelocity}");
        // rb.linearVelocity = horizontalForce + racketImpact;
        rb.linearVelocity += horizontalForce;
        Debug.Log($"spin: {spin}, normal: {normal}, spinDir: {spinDir}, SpinMagnitude: {spinMagnitude},horizontalForce: {horizontalForce} rb.LinearVelocity: {rb.linearVelocity}");
    }

    void HandleBatSpinCollision(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;
        GameObject racket = collision.gameObject;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();
        Vector3 racketVelocity = racketRb.linearVelocity;

        // ラケットの接線方向の速度（擦ってる方向）
        Vector3 tangentialVel = Vector3.ProjectOnPlane(racketVelocity, normal);
        // 回転軸（スピン方向） = 接線 × 法線
        Vector3 spinDir = Vector3.Cross(tangentialVel, normal).normalized;
        // 回転の強さ = 接線速度の大きさ × スピン係数
        float spinAmount = tangentialVel.magnitude * rubberPower;
        // 回転ベクトルとしてセット
        rb.angularVelocity = new Vector3(rb.angularVelocity.x,rb.angularVelocity.y, spinDir.z * spinAmount);

        Debug.Log($"[Spin] tangentialVel: {tangentialVel}, spinDir: {spinDir}, spinAmount: {spinAmount}, rb.angularVelocity: {rb.angularVelocity}");
    }
    void AdjustTrajectory(Collision collision)
    {
        GameObject racket = collision.gameObject;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();

        // y方向の速さは現在のボールの位置とネットの関係によって決まり, x方向の速さはラケットを動かす速さと傾きによって決まる
        float ballHeight = gameObject.transform.position.y;
        float yDifference = targetHeight - ballHeight; // 現在のボールの位置と狙う位置の高さの差
        float TuningVelocityY = yDifference < 0f ? NormalTuningVelocityY : LoopTuningVelocityY; // ネットよりボールが低い場合にはループ気味に打つ
        float velocityY = yDifference * TuningVelocityY; // ネットに向かってボールを打つ


        // ラケットの傾き: ラケットのrotation.xが-90° (地面に対して垂直)に近いほどボールが飛び, rotation.xが0°や-180°(地面に対して水平)に近づくほどボールが飛ばない
        // float racketRotationX = collision.gameObject.transform.eulerAngles.x;
        float racketRotationX = Mathf.DeltaAngle(0f, collision.gameObject.transform.eulerAngles.x) ;
        float angleFactor = 1f - Mathf.Abs(racketRotationX + 90f) / 90f + TunignAngle; 
        // ラケットの速さ: ラケットの動きが速いほどボールが飛び, ラケットの動きが遅いほどボールが飛ばない
        // float speedFactor = racketRb.linearVelocity.magnitude;
        float speedFactor = 2.5f;
        float velocityX = angleFactor * speedFactor * TuningVelocityX;
        if (collision.gameObject.CompareTag("EnemyBat"))
            velocityX *= -1;

        rb.linearVelocity = rb.linearVelocity * 0.05f + new Vector3(velocityX, velocityY, 0f);
        Debug.Log($"[Flight] ballY: {ballHeight}, velocityY: {velocityY}, racketAngleX: {racketRotationX}, angleFactor:{angleFactor}, speedFactor: {speedFactor}, finalVelocity: {rb.linearVelocity}");
    }

    Vector3 CalculateNewVelocity(Vector3 paddleVel, Vector3 normal)
    {
        return Vector3.zero;
    }

    Vector3 CalculateNewSpin(Vector3 paddleVel, Vector3 normal)
    {
        return Vector3.zero;
    }


    void ApplyMagnusEffect()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 spin = rb.angularVelocity;

        // Magnus 力 = 回転ベクトル × 速度ベクトル（スケーリングあり）
        Vector3 magnusForce = Vector3.Cross(spin, velocity) * magnusForceScale;
        rb.AddForce(magnusForce, ForceMode.Force);

    }

    private void ApplySpinEffect()
    {
        // スピンによって跳ね返りを変化させる処理
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentSpin = rb.angularVelocity;

        // 例：X軸のスピンが大きければ、Y方向に影響を与える（ふわっと浮く or 落ちる）
        float liftFactor = - Mathf.Clamp(currentSpin.x * 0.5f, -10f, 10f); // 負なら落ちる、正なら浮く
        Debug.Log("currentSpin.x: " + currentSpin.x);
        Debug.Log("liftFactor: " + liftFactor);
        // Y方向に lift を追加（＝跳ね返り方向がスピンにより上下に曲がる）
        rb.linearVelocity = new Vector3(currentVelocity.x, currentVelocity.y + liftFactor, currentVelocity.z);
        // （お好みで）Z方向にスピンで曲がる処理なども追加可能
    }

    // ボールの数秒先の軌道を予測する関数
    public float? SimulateUntilX(Vector3 pos, Vector3 vel, float targetX, List<Vector3> trajectoryPoints = null)
    {
        for (int i = 0; i < maxSteps; i++)
        {
            // 次の位置を計算
            vel += Physics.gravity * timeStep;
            Vector3 nextPos = pos + vel * timeStep;

            // バウンド判定
            if (pos.y > standY && nextPos.y <= standY)
            {
                pos.y = standY;
                vel.y = -vel.y * bounciness;
                nextPos = pos + vel * timeStep;
            }
            // 弾の軌跡を表示するため, 計算した弾の位置をリストに追加する
            trajectoryPoints?.Add(pos);

            // 目標Xを通過したら、高さを記録して終了
            if ((pos.x - targetX) * (nextPos.x - targetX) <= 0f)
            {
                float lerpedY = Mathf.Lerp(pos.y, nextPos.y, Mathf.InverseLerp(nextPos.x, pos.x, targetX));
                return Mathf.Max(standY, lerpedY);
            }
            pos = nextPos;
        }
        return null;
    }
}
