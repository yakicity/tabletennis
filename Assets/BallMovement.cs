using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

public class BallMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float linearDampingValue = 0f; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
    private float angularDampingValue = 0f; // 回転空気抵抗（摩擦）
    private float massValue = 0.0027f; // 卓球ボールの重さ（kg）
    private float magnusForceScale = 0.001f; // マグナス力のスケーリング（適当な試行値、0.001〜0.01くらい）

    private float groundY = 0.815f; // 地面の高さ（通常は0）
    private float bounciness = 1.0f;// 跳ね返りの強さ
    private int maxSteps = 500; // 予測のための計算回数
    private float timeStep = 0.02f; // 予測する時間幅
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

    }

    void FixedUpdate()
    {
        //ApplyMagnusEffect();
    }

    // Update is called once per frame

    void Update()
    {
        // スピンの向き（赤線）を可視化
        Debug.DrawRay(transform.position, rb.angularVelocity.normalized * 0.5f, Color.red);

        // 現在の速度の方向（青線）も表示してみると面白い
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f, Color.blue);
    }
    void OnCollisionEnter(Collision collision)
    {
        rb.useGravity = true;
        // rb.angularVelocity *= 0.8f;
        // スピンの影響をバウンドに反映
        if (collision.gameObject.CompareTag("wall"))
        {
            // ApplySpinEffect();
        }

        // if (collision.relativeVelocity.magnitude > 1f)
        // {
        //     Vector3 reflect = Vector3.Reflect(rb.velocity, collision.contacts[0].normal);
        //     rb.velocity = reflect * 1.05f; // 少し跳ね返り強化
        // }
    }
    void ApplyMagnusEffect()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 spin = rb.angularVelocity;

        // Magnus 力 = 回転ベクトル × 速度ベクトル（スケーリングあり）
        Vector3 magnusForce = Vector3.Cross(spin, velocity) * magnusForceScale;
        rb.AddForce(magnusForce, ForceMode.Force);
    }
    public void ApplyCutSpin()
    {
        // 下回転（Z軸まわりの反時計回転）
        rb.angularVelocity = new Vector3(0f, 0f, 1f);
    }

    public void ApplyDriveSpin()
    {
        // 上回転（Z軸まわりの時計回転）
        rb.angularVelocity = new Vector3(0f, 0f, -1f);
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
            if (pos.y > groundY && nextPos.y <= groundY)
            {
                pos.y = groundY;
                vel.y = -vel.y * bounciness;
                nextPos = pos + vel * timeStep;
            }
            // 弾の軌跡を表示するため, 計算した弾の位置をリストに追加する
            trajectoryPoints?.Add(pos);

            // 目標Xを通過したら、高さを記録して終了
            if ((pos.x - targetX) * (nextPos.x - targetX) <= 0f)
            {
                float lerpedY = Mathf.Lerp(pos.y, nextPos.y, Mathf.InverseLerp(nextPos.x, pos.x, targetX));
                return Mathf.Max(groundY, lerpedY);
            }
            pos = nextPos;
        }
        return null;
    }
}
