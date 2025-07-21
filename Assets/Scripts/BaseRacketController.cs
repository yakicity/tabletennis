using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseRacketController : MonoBehaviour
{
    /**
    * どれくらい長押しされたかをゲージで可視化するために用いる
    */
    public GameObject boostSlider; // 長押しゲージの画像

    /**
    * ラケットの前後の動きに関するパラメータやいや移動のための変数
    */
    protected float moveSpeed = 2.0f; // ラケットの移動速度
    private const float MinMoveSpeed = 2.0f; // ラケットが威力を溜めた時の最小のスピード
    private const float MaxMoveSpeed = 4.0f; // ラケットが威力を溜めた時の最大のスピード
    private const float MaxChargeTime = 0.5f; // 0.5秒ためたら maxSpeed になる
    private const float RacketMoveDistance = 0.2f; // ラケットが威力を溜めた後に移動する距離
    protected Vector3 moveInput = Vector3.zero; // ラケットが動く速さ. 継承先クラスの入力によって変化する

    /**
    * ラケットの傾きに関するパラメータや変数
    */
    protected Vector3 baseRotationVector = new Vector3(-90f, -90f, 180f); // 通常時の基本角度
    protected float drivePitchAngle = -10f; // ドライブは基本から-10度
    protected float cutPitchAngle = 20f;   // カットは基本から+20度
    protected float rollAnglePerLevel = 20f; // 1段階あたり20度傾く

    protected int[] racketFaceIndex = new int[2]; // ラケットの向きのインデックス. 0: drive cut,  1: right left
    // right left = 0: 通常, 1: firstLevelRight, 2: secondLevelRight, -1: firstLevelLeft, -2: secondLevelLeft
    // drive cut = 0: 通常, 1: drive, -1: cut

    /**
    * ボールを返す方向
    */
    private Vector3 returnDirectionNormal = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private Vector3 returnDirectionDrive = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private Vector3 returnDirectionCut = new Vector3(-0.3f, 0.2f, 0.0f).normalized;
    private Vector3 returnDirectionRight = new Vector3(-0.3f, 0.2f, 0.2f).normalized;
    private Vector3 returnDirectionLeft = new Vector3(-0.3f, 0.2f, -0.2f).normalized;

    /**
    * ラケットの長押しに関するパラメータ
    */
    private float lastTapTime = 0f; // 最後にCボタンが押された時間
    private float doubleTapThreshold = 0.2f; // Cボタン2回連続タップと判定するための時間
    private float boostSpeed; // 長押しによってラケットに適用される速さ
    protected bool isBoostCharging = false; // 2回連続押されたか
    protected bool isBoostMoving = false; // 長押しによてラケットの速さが変化中かどうか
    private float boostChargeRatio; // 最大と比較してどれくらい長押ししたかという割合

    /**
    * その他コンポーネントや Unity の設定
    */
    protected Rigidbody rb; // ラケットの Rigidbody
    protected GameObject ball; // ゲーム内にあるボール
    protected Rigidbody ballRb; // ボールの Rigidbody
    protected BaseBallMovement ballMovement; // ボールの軌跡を予測するために用いる
    private LineRenderer lineRenderer; // ボールの軌跡を表示するために用いる
    private Image boostSliderImage;
    private float timeScale; // ゲーム内の時間の進む速さ

    protected virtual void Start()
    {
        // 各種コンポーネントや Unity上の設定を取得
        rb = GetComponent<Rigidbody>();
        ball = GameObject.Find("Ball");
        ballRb = ball.GetComponent<Rigidbody>();
        ballMovement = ball.GetComponent<BaseBallMovement>();
        lineRenderer = ball.GetComponent<LineRenderer>();
        boostSliderImage = boostSlider.GetComponent<Image>();
        timeScale = Time.timeScale;

        // 衝突判定を連続的にする
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 右左の向きのインデックス
        racketFaceIndex[0] = 0; // 通常のラケットの向き
        racketFaceIndex[1] = 0; // 通常のラケットの向き
    }

    protected virtual void Update()
    {
        HandleBoostInput(); // Cボタンの2回連続タップと長押し入力を取得
        UpdateBoostUI(); // 長押し時の UI 更新
        // UpdateRotationDiscrete(); // ラケットの向きを離散的に更新
        // HandleInput(); // 移動入力の受付は継承先クラスで行う
    }

    protected virtual void FixedUpdate()
    {
        // キー入力がある時だけ速度を与え、ない時は止める
        if (!isBoostMoving)
            rb.linearVelocity = moveInput * moveSpeed;
        AdjustPositionToBall(transform.position.x, gameObject); // ラケットの位置をボールに合わせる
    }

    private void HandleBoostInput()
    {
        float now = Time.time;
        float tapDuration = now - lastTapTime; // 前回Cボタンが押されてからどれくらいの時間が経過しているか

        // --- Cボタン押下 ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            // ダブルタップ判定
            if (lastTapTime > 0f && tapDuration / timeScale <= doubleTapThreshold)
            {
                isBoostCharging = true;
            }
            else
                isBoostCharging = false;

            lastTapTime = now;
        }

        // --- Cボタン長押し中 ---
        if (Input.GetKey(KeyCode.C) && isBoostCharging)
        {
            boostChargeRatio = Mathf.Clamp01(tapDuration / MaxChargeTime);
        }

        // --- Cボタン離した時 ---
        if (Input.GetKeyUp(KeyCode.C))
        {
            if (isBoostCharging)
            {
                float boostSpeed = Mathf.Lerp(MinMoveSpeed, MaxMoveSpeed, boostChargeRatio);
                StartCoroutine(MoveRacketCoroutine(boostSpeed));
                Debug.Log($"Boost Applied: {boostSpeed}");
            }
            boostChargeRatio = 0f;
            isBoostCharging = false;
        }
    }

    private void UpdateBoostUI()
    {
        boostSliderImage.fillAmount = boostChargeRatio;
    }

    // cボタンを二度押しして長押しした後, ラケットを貯めた分の速さだけ動かす
    IEnumerator MoveRacketCoroutine(float racketMoveSpeed)
    {
        Vector3 initialPos = transform.position;
        isBoostMoving = true;

        rb.linearVelocity = new Vector3(racketMoveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        while (Mathf.Abs(initialPos.x - transform.position.x) < RacketMoveDistance)
        {
            yield return new WaitForFixedUpdate(); // 1フレーム分ラケットを動かす
        }

        rb.linearVelocity = Vector3.zero;
        isBoostMoving = false;
    }

    public int[] GetAngleIndices()
    {
        return racketFaceIndex;
    }

    public void AdjustPositionToBall(float targetX, GameObject racket)
    {
        Vector3 pos = racket.transform.position;
        Rigidbody racketRb = racket.GetComponent<Rigidbody>();
        List<Vector3> points = new();
        float? predictedY = ballMovement.SimulateUntilX(ball.transform.position, ballRb.linearVelocity, targetX, points);
        bool ballToPlayer = ballRb.linearVelocity.x < 0;

        if (ballToPlayer)
        {
            pos.y = predictedY ?? pos.y;
            racketRb.MovePosition(pos);
        }

        UpdateLineRender(points, ballToPlayer);
    }

    private void UpdateLineRender(List<Vector3> points, bool ballToPlayer)
    {
        if (points == null || lineRenderer == null) return;

        if (ballToPlayer)
        {
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            lineRenderer.enabled = true;
        }
        else
            lineRenderer.enabled = false;
    }

    /// <summary>
    /// ラケットの位置、回転、物理状態を初期化
    /// </summary>
    public void ResetState(Vector3 initialPosition, Quaternion initialRotation)
    {
        // 位置と回転を初期状態に戻す
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Rigidbodyが設定されていれば、動きを完全に止める
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ラケットの角度インデックスもリセットする
        racketFaceIndex[0] = 0;
        racketFaceIndex[1] = 0;

        // その他、必要に応じてリセットしたい変数をここに追加
        isBoostCharging = false;
        isBoostMoving = false;
    }
}