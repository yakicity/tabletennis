using Fusion;
using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// ラケットのインパクト情報をまとめるデータ構造
/// </summary>
public class RacketImpactData
{
    public Vector3 RacketVelocity { get; set; }
    public Quaternion RacketRotation { get; set; }
    public Vector3 ContactNormal { get; set; }
    public string RacketTag { get; set; }
}

/// <summary>
/// ネットワーク同期対応の卓球ボール制御クラス
/// 物理演算・返球計算・マグナス効果・軌道予測などを担当
/// </summary>
public class BallController : NetworkBehaviour
{
    /**
    * ボールやボールの動きに関するパラメータ
    */
    private const float MassValue = 0.0027f; // 卓球ボールの重さ（kg）
    private const float LinearDampingValue = 0f; // 空気抵抗係数
    private const float AngularDampingValue = 0f; // 回転空気抵抗（摩擦）
    private const float MagnusForceScale = 1e-7f; // マグナス力のスケーリング

    /**
     * ボールの回転や飛ぶ方向に関するパラメータ
     */
    private const float RubberPower = 20f; // ラバーによる回転量の増加率
    private const float TuningVelocityX = 0.5f;
    private const float TuningVelocityY = 1.0f;
    private const float TuningSpinEffect = 0.1f;
    private const float TunignAngle = 1.0f;
    private const float RacketMinSpeed = 2.0f;
    private const float SpinDecreaseRate = 0.8f;
    private Vector3 baseReturnVelocity = new(4.0f, 2.0f, 0.0f);
    private Vector3 rollVelocityOffsetPerLevel = new(0.0f, 0.0f, -0.5f);
    /**
 * ボールの軌道を予測するために用いるパラメータ
 */
    private const float StandY = 0.785f; // 台の高さ
    private const float Bounciness = 1.0f;// 跳ね返りの強さ
    private const int MaxSteps = 50; // 予測のための計算回数
    private const float TimeStep = 0.01f; // 予測する時間幅

    private Rigidbody rb;

    // ネットワーク同期される変数
    [Networked] public Vector3 NetworkPosition { get; set; }
    [Networked] public Vector3 NetworkVelocity { get; set; }
    [Networked] public Vector3 NetworkAngularVelocity { get; set; }
    [Networked] public bool NetworkUseGravity { get; set; }

    // 高頻度同期用のカウンター
    private int syncCounter = 0;

    /// <summary>
    /// オブジェクトがネットワーク上で生成された時に呼ばれる
    /// </summary>
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbodyが見つかりません: NetworkBallController");
            return;
        }
        // ボールやボールの動きに関するパラメータ設定
        rb.useGravity = false; // 最初は重力を無効にする
        rb.mass = MassValue; // 卓球ボールの重さ（kg）
        rb.linearDamping = LinearDampingValue; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
        rb.angularDamping = AngularDampingValue; // 回転空気抵抗（摩擦）
        // ボールのすり抜け対策
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// ネットワーク同期される物理演算フレーム
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // StateAuthorityが物理演算を実行し、ネットワーク変数を更新
            ApplyMagnusEffect(); // マグナス力の適用

            // Rigidbodyの状態をネットワーク変数に同期
            NetworkPosition = transform.position;
            NetworkVelocity = rb.linearVelocity;
            NetworkAngularVelocity = rb.angularVelocity;
            NetworkUseGravity = rb.useGravity;

            // 高頻度でRPC同期も実行（3フレームに1回）
            syncCounter++;
            if (syncCounter >= 3)
            {
                syncCounter = 0;
                RPC_SyncBallState(transform.position, rb.linearVelocity, rb.angularVelocity);
            }
        }
        else
        {
            // 他のクライアントはネットワーク変数からRigidbodyを更新
            // より強力な同期で遅延を最小化

            // 位置の差が大きい場合は即座に同期
            float positionDistance = Vector3.Distance(transform.position, NetworkPosition);
            if (positionDistance > 0.2f)
            {
                // 大きなずれは即座に修正
                transform.position = NetworkPosition;
                rb.linearVelocity = NetworkVelocity;
                rb.angularVelocity = NetworkAngularVelocity;
                Debug.Log($"Ball position corrected: distance was {positionDistance}");
            }
            else
            {
                // 小さなずれは補間で滑らかに
                float lerpSpeed = 15f * Runner.DeltaTime;
                transform.position = Vector3.Lerp(transform.position, NetworkPosition, lerpSpeed);
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, NetworkVelocity, lerpSpeed);
                rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, NetworkAngularVelocity, lerpSpeed);
            }

            rb.useGravity = NetworkUseGravity;

            // デバッグログ（必要に応じて）
            if (positionDistance > 0.1f)
            {
                Debug.Log($"Ball position sync: Local={transform.position}, Network={NetworkPosition}, Distance={positionDistance}");
            }
        }
    }
    /// <summary>
    /// 衝突イベント
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority) return;

        // ラケットとの衝突
        if (collision.gameObject.CompareTag("PlayerBat") || collision.gameObject.CompareTag("EnemyBat"))
        {
            var racketObject = collision.gameObject;
            var racketController = racketObject.GetComponent<RacketController>();
            RacketImpactData impactData;

            // 操縦戦略（Strategyパターン）でAI/Playerを分岐
            var strategy = racketController != null ? racketController.GetControlStrategy() : null;
            if (strategy is AIControlStrategy aiStrategy)
            {
                impactData = aiStrategy.GetVirtualImpactData(collision);
            }
            else
            {
                impactData = new RacketImpactData
                {
                    RacketVelocity = racketObject.GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero,
                    RacketRotation = racketObject.transform.rotation,
                    ContactNormal = collision.contacts[0].normal,
                    RacketTag = racketObject.tag
                };
            }

            var (velocity, spin) = CalculateBallReturn(impactData);
            ApplyReturn(velocity, spin);
        }
        else
        {
            // ラケット以外（台やネット）
            if (rb != null) rb.useGravity = true;
        }
    }

    /// <summary>
    /// マグナス力（ボールの回転による揚力）を計算し、適用する
    /// </summary>
    private void ApplyMagnusEffect()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 spin = rb.angularVelocity;
        Vector3 magnusForce = Vector3.Cross(spin, velocity) * MagnusForceScale;
        rb.AddForce(magnusForce, ForceMode.Force);
    }

    /// <summary>
    /// 計算された速度と回転をボールのRigidbodyに適用する
    /// </summary>
    public void ApplyReturn(Vector3 velocity, Vector3 spin)
    {
        if (Object.HasStateAuthority)
        {
            rb.linearVelocity = velocity;
            rb.angularVelocity = spin;
            rb.useGravity = true;

            // 即座にネットワーク変数も更新
            NetworkVelocity = velocity;
            NetworkAngularVelocity = spin;
            NetworkUseGravity = true;

            Debug.Log($"Ball velocity applied: {velocity}, spin: {spin}");
        }
    }

    /// <summary>
    /// ボールの回転・ヒット時の速度補正を計算する
    /// </summary>
    /// <summary>
    /// 返球計算（インパクト情報から速度・回転を算出）
    /// </summary>
    private (Vector3, Vector3) CalculateBallReturn(RacketImpactData impactData)
    {
        Vector3 generatedSpin = CalculateGeneratedSpin(impactData);
        Vector3 calculatedBaseVelocity = CalculateVelocityFromRacket(impactData);
        Vector3 hitVelocity = CalculateHitVelocity(impactData);

        Vector3 currentSpin = rb.angularVelocity;
        Vector3 finalSpin;
        Vector3 spinEffect = Vector3.zero;

        if (Mathf.Abs(generatedSpin.z) > Mathf.Abs(currentSpin.z * SpinDecreaseRate))
        {
            finalSpin = generatedSpin;
        }
        else
        {
            finalSpin = generatedSpin + currentSpin;
            spinEffect = CalculateSpinEffect(impactData, finalSpin);
        }

        Vector3 returnVelocity = calculatedBaseVelocity + hitVelocity + spinEffect;

        // 敵ラケットの場合はX/Z反転
        if (impactData.RacketTag == "EnemyBat")
        {
            returnVelocity.x *= -1;
            returnVelocity.z *= -1;
        }

        return (returnVelocity, finalSpin);
    }

    /// <summary>
    /// ラケットの傾きと動かす速さによって生成される、回転速度を計算する
    /// </summary>
    private Vector3 CalculateGeneratedSpin(RacketImpactData impactData)
    {
        Vector3 normal = impactData.ContactNormal;
        Vector3 racketVelocity = impactData.RacketVelocity;
        Vector3 tangentialVel = Vector3.ProjectOnPlane(racketVelocity, normal);
        Vector3 spinDir = Vector3.Cross(tangentialVel, normal).normalized;
        float spinAmount = tangentialVel.magnitude * RubberPower;
        return new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, spinDir.z * spinAmount);
    }

    /// <summary>
    /// 【リファクタリングの最重要点】
    /// ラケットの特定のスクリプトに依存せず、物理的な情報（角度）から返球方向を計算する
    /// </summary>
    private Vector3 CalculateVelocityFromRacket(RacketImpactData impactData)
    {
        // 角度やインデックスに応じて返球速度を調整
        int leftRightIndex = 0; // 必要に応じてimpactDataから取得
        Vector3 finalVelocity = baseReturnVelocity + rollVelocityOffsetPerLevel * leftRightIndex;
        return finalVelocity;
    }


    /// <summary>
    /// ボールの回転によってずれる方向を計算する
    /// </summary>
    private Vector3 CalculateSpinEffect(RacketImpactData impactData, Vector3 spin)
    {
        Vector3 normal = impactData.ContactNormal;
        Vector3 spinDir = Vector3.Cross(spin, normal).normalized;
        float spinMagnitude = spin.magnitude;
        return new Vector3(0f, spinDir.y * spinMagnitude * TuningSpinEffect, 0f);
    }

    /// <summary>
    /// ラケット自体の速度と角度から、ボールに加える追加速度を計算する
    /// </summary>
    private Vector3 CalculateHitVelocity(RacketImpactData impactData)
    {
        float angleFactor = (impactData.RacketRotation * Vector3.right).x * TunignAngle;
        float velocityY = -angleFactor * TuningVelocityY;
        float actualRacketSpeed = Mathf.Abs(impactData.RacketVelocity.x);
        float speedFactor = Mathf.Max(actualRacketSpeed, RacketMinSpeed);
        float velocityX = (speedFactor - RacketMinSpeed) * (1 - Mathf.Abs(angleFactor)) * TuningVelocityX;
        return new Vector3(velocityX, velocityY, 0f);
    }

    /// <summary>
    /// ボールの位置、回転、物理状態を初期化（サーバーから呼ばれることを想定）
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ResetState(Vector3 initialPosition, Quaternion initialRotation)
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }

        // ネットワーク変数も初期化
        NetworkPosition = initialPosition;
        NetworkVelocity = Vector3.zero;
        NetworkAngularVelocity = Vector3.zero;
        NetworkUseGravity = false;
    }

    // ボールの数秒先の軌道を予測する関数
    public float? SimulateUntilX(Vector3 startPos, Vector3 startVel, float targetX, List<Vector3> trajectoryPoints = null)
    {
        Vector3 currentPos = startPos;
        Vector3 currentVel = startVel;
        Debug.Log("Simulating trajectory from start position: " + startPos + " with velocity: " + startVel);
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
            // Debug.Log($"Step {i}: Current Position: {currentPos}, Next Position: {nextPos}, targetX: {targetX}");
            if (HasCrossedTargetX(currentPos.x, nextPos.x, targetX))
            {
                // Debug.Log($"Trajectory crossed target X: {targetX} at step {i}");
                float lerpedY = Mathf.Lerp(currentPos.y, nextPos.y, Mathf.InverseLerp(nextPos.x, currentPos.x, targetX));
                return Mathf.Max(StandY, lerpedY);
            }
            currentPos = nextPos;
        }
        return null;
    }

    // バウンド判定
    private bool IsBounceTriggered(Vector3 currentPos, Vector3 nextPos)
    {
        return currentPos.y > StandY && nextPos.y <= StandY;
    }

    // X方向の目標到達判定
    private bool HasCrossedTargetX(float currentX, float nextX, float targetX)
    {
        return (currentX - targetX) * (nextX - targetX) <= 0f;
    }

    /// <summary>
    /// 高頻度でボール状態を同期するRPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncBallState(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
    {
        if (!Object.HasStateAuthority)
        {
            // クライアント側で受信した場合、即座に適用
            transform.position = position;
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}