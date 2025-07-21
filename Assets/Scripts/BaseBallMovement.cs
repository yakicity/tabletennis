using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using System;
using UnityEditor.Callbacks;
using System.Net.NetworkInformation;

public class BaseBallMovement : MonoBehaviour
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
    private const float TuningVelocityX = 0.5f; // ボールがネットより高い時のX方向速度の調整
    private const float TuningVelocityY = 1.0f;
    private const float TuningSpinEffect = 0.1f; // ボールがネットより低い時のX方向速度の調整
    private const float TunignAngle = 1.0f; // ラケット傾きによる飛距離補正
    private const float RacketMinSpeed = 2.0f; // ラケットの最低限の速さ
    private const float SpinDecreaseRate = 0.8f;
    private Vector3 defaultReturn = new(4.0f, 2.0f, 0.0f);
    private Vector3 baseReturnVelocity = new(4.0f, 2.0f, 0.0f);
    // 左右の傾き1段階あたりに加算されるオフセット
    private Vector3 rollVelocityOffsetPerLevel = new(0.0f, 0.0f, -0.5f);

    /**
    * ボールの軌道を予測するために用いるパラメータや変数
    */
    private const float StandY = 0.785f; // 台の高さ
    private const float Bounciness = 1.0f;// 跳ね返りの強さ
    private const int MaxSteps = 50; // 予測のための計算回数
    private const float TimeStep = 0.01f; // 予測する時間幅

    /**
    * ボールの Rigidbody
    */
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // ボールやボールの動きに関するパラメータ設定
    }

    public void InitializeParameter(Rigidbody ballRb){
        ballRb.useGravity = false; // 最初は重力を無効にする
        ballRb.mass = MassValue; // 卓球ボールの重さ（kg）
        ballRb.linearDamping = LinearDampingValue; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
        ballRb.angularDamping = AngularDampingValue; // 回転空気抵抗（摩擦）

        // ボールのすり抜け対策
        ballRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ballRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        ApplyMagnusEffect(rb); // マグナス力の適用
    }

    void Update()
    {
        // スピンの向き（赤線）を可視化
        Debug.DrawRay(transform.position, rb.angularVelocity.normalized * 0.5f, Color.red);

        // 現在の速度の方向（青線）も表示してみると面白い
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f, Color.blue);
        // Debug.Log("AngularVelocity" + rb.angularVelocity);
    }

    public void ApplyMagnusEffect(Rigidbody ballRb)
    {
        Vector3 velocity = ballRb.linearVelocity;
        Vector3 spin = ballRb.angularVelocity;

        // Magnus 力 = 回転ベクトル × 速度ベクトル（スケーリングあり）
        Vector3 magnusForce = Vector3.Cross(spin, velocity) * MagnusForceScale;
        ballRb.AddForce(magnusForce, ForceMode.Force);
    }

    public void ApplyReturn(Vector3 velocity, Vector3 spin, Rigidbody ballRb)
    {
        ballRb.linearVelocity = velocity;
        ballRb.angularVelocity = spin;
        ballRb.useGravity = true;
    }

    // ボールの回転・ヒット時の速度補正を計算する
    public (Vector3, Vector3) CalculateBallReturn(GameObject racket, Collision ballCollision)
    {
        Vector3 currentSpin = rb.angularVelocity; // 現在のボールのスピン
        Vector3 generatedSpin = CalculateGeneratedSpin(ballCollision, racket); // ラケットによって生成されるスピン
        Vector3 baseReturnVelocity = CalculateVelocityFromRacketFace(racket); // ラケットの傾きによって生成される, ボールの速度ベクトル

        Vector3 finalSpin; // 最終的なスピン
        Vector3 spinEffect = Vector3.zero;
        // Debug.Log($"currentSpin: {currentSpin}, generateSpin: {generatedSpin}");

        // スピン比較：生成スピンが強ければ上書き、弱ければ合成＋ズレ補正
        if (Mathf.Abs(generatedSpin.z) > Mathf.Abs(currentSpin.z * SpinDecreaseRate))
        {
            finalSpin = generatedSpin;
        }
        else
        {
            finalSpin = generatedSpin + currentSpin;
            spinEffect = CalculateSpinEffect(ballCollision, finalSpin);
            Debug.Log($"finalSpin: {finalSpin}, spinEffect: {spinEffect}");
        }

        // ラケットの速さと傾きによる速度補正を計算する
        Vector3 hitVelocity = CalculateHitVelocity(racket);
        // Debug.Log($"hitVelocity: {hitVelocity}");

        // ボールに最終的な速度とスピンを適用
        Vector3 returnVelocity = baseReturnVelocity + hitVelocity + spinEffect;
        Debug.Log($"returnVelocity: {returnVelocity}, finalSpin: {finalSpin}");
        return (returnVelocity, finalSpin);
    }

    // ラケットの傾きと動かす速さによって生成される, 回転速度を計算する
    Vector3 CalculateGeneratedSpin(Collision ballCollision, GameObject racket)
    {
        Vector3 normal = ballCollision.contacts[0].normal;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();
        Vector3 racketVelocity = racketRb.linearVelocity;

        // ラケットの接線成分 (擦っている方向)
        Vector3 tangentialVel = Vector3.ProjectOnPlane(racketVelocity, normal);
        // 回転軸（スピン方向） = 接線 × 法線
        Vector3 spinDir = Vector3.Cross(tangentialVel, normal).normalized;
        // 回転の強さ = 接線速度の大きさ × スピン係数
        float spinAmount = tangentialVel.magnitude * RubberPower;
        // 回転をZ軸 (上下回転)にだけ反映
        return new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, spinDir.z * spinAmount);
    }

    // ラケットの傾きによって、ボールを打ち返すデフォルトの速度ベクトルを計算する
    Vector3 CalculateVelocityFromRacketFace(GameObject racket)
    {
        PlayerRacketController racketController = racket.GetComponent<PlayerRacketController>();
        // ラケットコントローラーが取得できなければ、デフォルト値を返す
        if (racketController == null)
        {
            Debug.LogWarning("衝突オブジェクトにRacketControllerが見つかりません。");
            return baseReturnVelocity;
        }
        // 1. ラケットから現在の状態インデックスを取得
        int[] angleIndices = racketController.GetAngleIndices();
        int leftRightIndex = angleIndices[1];

        // 2. 基本となる返球速度から計算を開始
        Vector3 finalVelocity = baseReturnVelocity;

        // 4. 左右の状態に応じて速度を調整
        // (例) 右に2段階なら、rollVelocityOffsetPerLevel * 2 が加算される
        finalVelocity += rollVelocityOffsetPerLevel * leftRightIndex;

        // 5. 敵が打った場合は、進行方向(X,Z)を反転させる
        return finalVelocity;

        // // ラケットのrotationをオイラー角（XYZ角度）に変換
        // Vector3 eulerAngles = racket.transform.rotation.eulerAngles;
        // Vector3 returnVelocitybyRacketFace = defaultReturn; // デフォルトの返球方向

        // // デバッグ用に現在の角度を表示（問題解決に役立ちます）
        // Debug.Log($"Racket Euler Angles: {eulerAngles}");

        // // --- ラケットの向きによる分岐 ---
        // // Y軸の回転角度で向きを判定するのが一般的です。
        // // 角度には誤差があるため、完全な一致(==)ではなく、範囲で比較するのが安全です。
        // Debug.Log($"Racket Y Angle: {eulerAngles.y}");
        // // 例：Y軸の角度が約45度なら「デフォルト」
        // if (Mathf.Abs(eulerAngles.y - 90.0f) < 5.0f)
        // {
        //     Debug.Log("判定: デフォルト");
        //     // デフォルトの返球方向（右利きの場合のフォアハンドストレートのようなイメージ）
        //     returnVelocitybyRacketFace = defaultReturn;
        // }
        // // 例：Y軸の角度が約60度なら「左向き」
        // else if (Mathf.Abs(eulerAngles.y - 70.0f) < 5.0f)
        // {
        //     Debug.Log("判定: 左向き");
        //     // 左方向への返球（クロス方向）
        //     // Xの値をマイナスにする、Zの値を変えるなどで調整
        //     returnVelocitybyRacketFace = new Vector3(4.0f, 2.0f, 1.0f);
        // }
        // // 例：Y軸の角度が約30度なら「右向き」
        // else if (Mathf.Abs(eulerAngles.y - 110.0f) < 5.0f)
        // {
        //     Debug.Log("判定: 右向き");
        //     // 右方向への返球（逆クロス方向）
        //     returnVelocitybyRacketFace = new Vector3(4.0f, 2.0f, -1.0f);
        // }
        // // 例：Y軸の角度が約60度なら「左向き」
        // else if (Mathf.Abs(eulerAngles.y - 50.0f) < 5.0f)
        // {
        //     Debug.Log("判定: 2段階目左向き");
        //     // 左方向への返球（クロス方向）
        //     // Xの値をマイナスにする、Zの値を変えるなどで調整
        //     returnVelocitybyRacketFace = new Vector3(4.0f, 2.0f, 1.5f);
        // }
        // // 例：Y軸の角度が約30度なら「右向き」
        // else if (Mathf.Abs(eulerAngles.y - 130.0f) < 5.0f)
        // {
        //     Debug.Log("判定: 2段階目右向き");
        //     // 右方向への返球（逆クロス方向）
        //     returnVelocitybyRacketFace = new Vector3(4.0f, 2.0f, -1.5f);
        // }
        // else
        // {
        //     Debug.Log("判定: どれにも当てはまらない（デフォルトを返します）");
        //     // いずれの条件にも一致しない場合のデフォルト値
        //     returnVelocitybyRacketFace = defaultReturn;
        // }

        // if (is_enemy)
        // {
        //     // Enemy が打つ時はX方向速度が逆になる
        //     returnVelocitybyRacketFace.x *= -1;
        // }
        // return returnVelocitybyRacketFace;
    }

    // ボールの回転によってずれる方向を計算する
    Vector3 CalculateSpinEffect(Collision ballCollision, Vector3 spin)
    {
        // 接触点の法線
        Vector3 normal = ballCollision.contacts[0].normal;
        // ずれる方向の単位ベクトル
        Vector3 spinDir = Vector3.Cross(spin, normal).normalized;
        // 回転量の大きさ
        float spinMagnitude = spin.magnitude;
        // 実際に適用するベクトル(y方向のみ適用)
        return new Vector3(0f, spinDir.y * spinMagnitude * TuningSpinEffect, 0f);
    }
    Vector3 CalculateHitVelocity(GameObject racket)
    {
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();

        // Y方向速度 (ラケットの角度から決定)
        float angleFactor = racket.transform.forward.x * TunignAngle; // -1~1の範囲。ラケットが上向きだと-1に近づき, ラケットが下向きだと1に近づく
        float velocityY = -angleFactor * TuningVelocityY; // ラケットが上向だと上方向に飛び, 下向きだとした方向に飛ぶ

        // X方向速度 (ラケットの傾きと速度から決定)
        // ラケットの速さ: ラケットの動きが速いほどボールが飛び, ラケットの動きが遅いほどボールが飛ばない
        float actualRacketSpeed = Mathf.Abs(racketRb.linearVelocity.x);
        float speedFactor = Mathf.Max(actualRacketSpeed, RacketMinSpeed); // ラケットが RacketMinSpeed より速く動いていたらそれを適用, それ以下だったら RacketMinSPeed の速さをボールに与える
        // velocityX = (ラケットの速さ - ラケットの最低速度 : 0f ~ 2f) * (ラケットの角度: 0f ~ 1f) * (パラメータ : 0.5f)
        float velocityX = (speedFactor - RacketMinSpeed) * (1 - Mathf.Abs(angleFactor)) * TuningVelocityX;

        // Enemy が打つ時はX方向速度が逆になる
        if (racket.CompareTag("EnemyBat"))
            velocityX *= -1;

        return new Vector3(velocityX, velocityY, 0f);
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
    private bool IsBounceTriggered(Vector3 currentPos, Vector3 nextPos)
    {
        return currentPos.y > StandY && nextPos.y <= StandY;
    }

    // X方向の目標到達判定
    private bool HasCrossedTargetX(float currentX, float nextX, float targetX)
    {
        return (currentX - targetX) * (nextX - targetX) <= 0f;
    }

    void OnCollisionEnter(Collision collision)
    {
        // ラケットがボールに触れたら重力を付与
        rb.useGravity = true;
    }

    /// <summary>
    /// ボールの位置、回転、物理状態を初期化
    /// </summary>
    public void ResetState(Vector3 initialPosition, Quaternion initialRotation)
    {
        // 位置と回転を初期状態に戻す
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Rigidbodyが設定されていれば、動きを止めて重力も無効化する
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }
    }


}
