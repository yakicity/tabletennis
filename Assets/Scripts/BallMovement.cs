using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using System;
using UnityEditor.Callbacks;
using System.Net.NetworkInformation;

[RequireComponent(typeof(Rigidbody))]
public class BallMovement : MonoBehaviour
{
    /**
    * ボールやボールの動きに関するパラメータ
    */
    private const float MassValue = 0.0027f; // 卓球ボールの重さ（kg）
    private const float LinearDampingValue = 0.1f; // 空気抵抗係数（適当な試行値、0.4〜1.0くらい）
    private const float AngularDampingValue = 0.1f; // 回転空気抵抗（摩擦）
    private const float MagnusForceScale = 1e-7f; // マグナス力のスケーリング（適当な試行値、0.001〜0.01くらい）


    /**
    * ボールの回転や飛ぶ方向に関するパラメータ
    */
    private Vector3 baseReturnVelocity = new(4.0f, 2.0f, 0.0f);
    private Vector3 baseServeVelocity = new(2.5f, 2.0f, 0.0f);
    private bool isServe = true; // サーブ中かどうか
    // 左右の傾き1段階あたりに加算されるオフセット
    private Vector3 rollVelocityOffsetPerLevel = new(0.0f, 0.0f, -0.5f);
    /**
    * スマッシュ時のパラメータ
    */
    private const float SmashTargetX = 1.8f;           // スマッシュのターゲットX座標（相手コートの奥）
    private const float SmashTargetY = 0.85f;          // スマッシュのターゲットY座標（台の高さ付近）
    private const float SmashVelocityScaleX = 2.4f;    // X方向（前後）の速度スケール
    private const float SmashVelocityY = 0.5f;         // Y方向（上下）の固定速度
    private const float SmashVelocityScaleZ = 1.0f;    // Z方向（左右）の速度スケール
    private const float SmashTargetZOffsetPerLevel = 1.0f; // ラケット傾き1段階あたりのZ座標オフセット

    /**
    * ボールの軌道を予測するために用いるパラメータや変数
    */
    private const float StandY = 0.820f; // 台の高さ
    private const float Bounciness = 1.0f;// 跳ね返りの強さ
    private const int MaxSteps = 50; // 予測のための計算回数
    private const float TimeStep = 0.01f; // 予測する時間幅

    /**
    * ボールの Rigidbody
    */
    private Rigidbody rb;

    [Header("ゲーム管理")]
    public GameManager gameManager; // GameManagerへの参照

    private const string playerObjectName = "PlayerBat";
    private string playerTag;

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

        playerTag = GameObject.FindWithTag(playerObjectName).tag;
        if (playerTag == null)
        {
            Debug.LogError("PlayerBatタグのついたオブジェクトが見つかりません。");
        }
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

    public void ApplyReturn(Vector3 velocity, Vector3 spin)
    {
        rb.linearVelocity = velocity;
        rb.angularVelocity = spin;
        rb.useGravity = true;
    }

    // ボールの回転・ヒット時の速度補正を計算する
    public (Vector3, Vector3) CalculateBallReturn(GameObject racket, Collision ballCollision, float xSpeed = 0f)
    {
        const float verticalVelocityThreshold = 2.0f;
        const float paramReturnXVelocity = 2.3f;
        const float verticalVelocityMaxCap = 4.0f;
        const float spinDecreaseRate = 0.8f;

        Vector3 currentSpin = rb.angularVelocity; // 現在のボールのスピン
        Vector3 generatedSpin = CalculateGeneratedSpin(ballCollision, racket, xSpeed); // ラケットによって生成されるスピン
        Vector3 baseReturnVelocity = CalculateVelocityFromRacketFace(racket); // ラケットの傾きによって生成される, ボールの速度ベクトル
        Debug.Log("currentSpin: " + currentSpin + ", generatedSpin: " + generatedSpin);

        Vector3 finalSpin; // 最終的なスピン
        Vector3 spinEffect = Vector3.zero;
        // Debug.Log($"currentSpin: {currentSpin}, generateSpin: {generatedSpin}");

        // スピン比較：生成スピンが強ければ上書き、弱ければ合成＋ズレ補正
        if (Mathf.Abs(generatedSpin.z) > Mathf.Abs(currentSpin.z * spinDecreaseRate))
        {
            finalSpin = generatedSpin;
        }
        else
        {
            finalSpin = generatedSpin + currentSpin;
            spinEffect = CalculateSpinEffect(ballCollision, finalSpin);
            // Debug.Log($"finalSpin: {finalSpin}, spinEffect: {spinEffect}");
        }

        // ラケットの速さと傾きによる速度補正を計算する
        Vector3 hitVelocity = CalculateHitVelocity(racket);
        // Debug.Log($"hitVelocity: {hitVelocity}");

        // ボールに最終的な速度とスピンを適用
        Vector3 returnVelocity = baseReturnVelocity + hitVelocity + spinEffect;

        // TODO: スマッシュでボールが浮いた時は, X方向を減少させない. 現状は, ボールが浮いたらその分だけX軸方向を減らす. でもそうすると, スマッシュ時に齟齬が生じる. そのため, currentSpinを見て, currentSpinがそんなになかったら飛んでくようにする.
        if (returnVelocity.y > verticalVelocityThreshold)
            returnVelocity.x = returnVelocity.x * paramReturnXVelocity / Mathf.Min(returnVelocity.y, verticalVelocityMaxCap);
        isServe = false; // サーブ状態を解除
        Debug.Log($"returnVelocity: {returnVelocity}, finalSpin: {finalSpin}");
        return (returnVelocity, finalSpin);
    }

    // ラケットの傾きと動かす速さによって生成される, 回転速度を計算する
    Vector3 CalculateGeneratedSpin(Collision ballCollision, GameObject racket, float xSpeed = 0f)
    {
        const float rubberPower = 20f; // ラバーによる回転量の増加率
        Vector3 normal = ballCollision.contacts[0].normal;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();
        Vector3 racketVelocity = racket.CompareTag(playerTag) ? racketRb.linearVelocity : new Vector3(xSpeed, 0f, 0f);

        // ラケットの接線成分 (擦っている方向)
        Vector3 tangentialVel = Vector3.ProjectOnPlane(racketVelocity, normal);
        // 回転軸（スピン方向） = 接線 × 法線
        Vector3 spinDir = Vector3.Cross(tangentialVel, normal).normalized;
        // 回転の強さ = 接線速度の大きさ × スピン係数
        float spinAmount = tangentialVel.magnitude * rubberPower * Mathf.Abs(racketVelocity.x);
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
            return isServe ? baseServeVelocity : baseReturnVelocity;
        }
        // 1. ラケットから現在の状態インデックスを取得
        int[] angleIndices = racketController.GetAngleIndices();
        int leftRightIndex = angleIndices[1];

        // 2. 基本となる返球速度から計算を開始
        Vector3 finalVelocity = isServe ? baseServeVelocity : baseReturnVelocity;
        Debug.Log("finalVelocity (初期値): " + finalVelocity);

        // 4. 左右の状態に応じて速度を調整
        // (例) 右に2段階なら、rollVelocityOffsetPerLevel * 2 が加算される
        finalVelocity += rollVelocityOffsetPerLevel * leftRightIndex;

        // 5. 敵が打った場合は、進行方向(X,Z)を反転させる
        return finalVelocity;
    }

    // ボールの回転によってずれる方向を計算する
    Vector3 CalculateSpinEffect(Collision ballCollision, Vector3 spin)
    {
        const float tuningSpinEffect = 0.1f;
        // 接触点の法線
        Vector3 normal = ballCollision.contacts[0].normal;
        // ずれる方向の単位ベクトル
        Vector3 spinDir = Vector3.Cross(spin, normal).normalized;
        // 回転量の大きさ
        float spinMagnitude = spin.magnitude;
        float isPlayerHitBall = rb.linearVelocity.x > 0 ? -1f : 1f;
        // 実際に適用するベクトル(y方向のみ適用)
        return new Vector3(0f, isPlayerHitBall * spinDir.y * spinMagnitude * tuningSpinEffect, 0f);
    }
    Vector3 CalculateHitVelocity(GameObject racket)
    {
        const float tuningVelocityX = 0.5f;
        const float tuningVelocityY = 1.0f;
        const float tuningAngle = 0.75f; // ラケット傾きによる飛距離補正
        const float racketMinSpeed = 2.0f; // ラケットの最低限の速さ
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();

        // Y方向速度 (ラケットの角度から決定)
        float racketForwardX = racket.CompareTag(playerTag) ? racket.transform.forward.x : -racket.transform.forward.x;
        float angleFactor = racketForwardX * tuningAngle; // -1~1の範囲。ラケットが上向きだと-1に近づき, ラケットが下向きだと1に近づく
        Debug.Log("transform.forward.x: " + racket.transform.forward.x);
        float velocityY = -angleFactor * tuningVelocityY; // ラケットが上向だと上方向に飛び, 下向きだとした方向に飛ぶ

        // X方向速度 (ラケットの傾きと速度から決定)
        // ラケットの速さ: ラケットの動きが速いほどボールが飛び, ラケットの動きが遅いほどボールが飛ばない
        float actualRacketSpeed = Mathf.Abs(racketRb.linearVelocity.x);
        float speedFactor = Mathf.Max(actualRacketSpeed, racketMinSpeed); // ラケットが RacketMinSpeed より速く動いていたらそれを適用, それ以下だったら RacketMinSPeed の速さをボールに与える
        // velocityX = (ラケットの速さ - ラケットの最低速度 : 0f ~ 2f) * (ラケットの角度: 0f ~ 1f) * (パラメータ : 0.5f)
        float velocityX = (speedFactor - racketMinSpeed) * (1 - Mathf.Abs(angleFactor)) * tuningVelocityX;

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


    /// <summary>
    /// スマッシュ時の返球速度を計算する（直線的にターゲットへ向かう）
    /// </summary>
    /// <param name="hitPosition">ボールの現在位置</param>
    /// <param name="racketRollIndex">ラケットの左右傾きインデックス（-2〜2）</param>
    /// <returns>返球速度ベクトル</returns>
    public Vector3 CalculateSmashVelocity(Vector3 hitPosition, int racketRollIndex)
    {
        float targetZ = CalculateSmashTargetZ(hitPosition, racketRollIndex);

        // 各軸の速度を計算
        float velocityX = CalculateReturnSmashVelocityX(SmashTargetX, hitPosition.x);
        float velocityY = CalculateReturnSmashVelocityY(SmashTargetY, hitPosition.y);
        float velocityZ = CalculateReturnSmashVelocityZ(targetZ, hitPosition.z);
        Debug.Log($"Smash Velocity: ({velocityX}, {velocityY}, {velocityZ})");
        return new Vector3(velocityX, velocityY, velocityZ);
    }

    /// <summary>
    /// ラケットの傾きに応じたスマッシュのターゲットZ座標を取得
    /// </summary>
    private float CalculateSmashTargetZ(Vector3 hitPosition, int racketRollIndex)
    {
        // 基本は現在のZ座標、ラケット傾きで左右に調整
        float baseZ = hitPosition.z;
        return baseZ - racketRollIndex * SmashTargetZOffsetPerLevel;
    }

    /// <summary>
    /// X方向（前後）のスマッシュ速度を計算
    /// </summary>
    private float CalculateReturnSmashVelocityX(float targetX, float hitPositionX)
    {
        float deltaX = targetX - hitPositionX;
        return deltaX * SmashVelocityScaleX;
    }

    /// <summary>
    /// Y方向（上下）のスマッシュ速度を計算
    /// </summary>
    private float CalculateReturnSmashVelocityY(float targetY, float hitPositionY)
    {
        return SmashVelocityY;
    }

    /// <summary>
    /// Z方向（左右）のスマッシュ速度を計算
    /// </summary>
    private float CalculateReturnSmashVelocityZ(float targetZ, float hitPositionZ)
    {
        float deltaZ = targetZ - hitPositionZ;
        return deltaZ * SmashVelocityScaleZ;
    }

    void OnCollisionEnter(Collision collision)
    {
        // ラケットがボールに触れたら重力を付与
        rb.useGravity = true;

        // ▼▼▼ 衝突相手のタグに応じてGameManagerに通知 ▼▼▼

        // プレイヤーのラケットに当たった場合
        if (collision.gameObject.CompareTag("PlayerBat"))
        {
            // プレイヤーが打ったことを通知
            gameManager.OnRacketHit(true);
        }
        // 相手のラケットに当たった場合
        else if (collision.gameObject.CompareTag("EnemyBat"))
        {
            // 相手が打ったことを通知
            gameManager.OnRacketHit(false);
        }
        // コートに当たった場合
        else if (collision.gameObject.CompareTag("table"))
        {
            Vector3 bouncePosition = collision.contacts[0].point;
            gameManager.OnCourtBounce(bouncePosition);
        }

    }

    /// <summary>
    /// ボールの位置、回転、物理状態を初期化
    /// </summary>
    public void ResetState(Vector3 initialPosition, Quaternion initialRotation)
    {
        if (rb != null)
        {
            rb.position = initialPosition;
            rb.rotation = initialRotation;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }
        else
        {
            // Rigidbodyがない場合のみ transform を使う
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
        isServe = true; // サーブ状態にリセット
    }


}
