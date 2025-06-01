using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class RacketController : MonoBehaviour
{

    /**
    * どれくらい長押しされたかをゲージで可視化するために用いる
    */
    public GameObject boostSlider; // 長押しゲージの画像

    /**
    * ラケットの前後の動きに関するパラメータやいや移動のための変数
    */
    private static float moveSpeed = 3.0f; // ラケットの移動速度
    private static float minMoveSpeed = 2.0f; // ラケットが威力を溜めた時の最小のスピード
    private static float maxMoveSpeed = 4.0f; // ラケットが威力を溜めた時の最大のスピード
    private static float maxChargeTime = 1.0f; // 1秒ためたら maxSpeed になる
    private static float racketMoveDistance = 0.2f; // ラケットが威力を溜めた後に移動する距離
    private Vector3 moveInput = Vector3.zero; // ラケットが動く速さ. 入力によって変化する

    /**
    * ラケットの傾きに関するパラメータや変数
    */
    private Vector3 normalRotationVector = new Vector3(-90f, -90f, 180f); // 通常のラケットの向き
    private Vector3 driveRotationVector = new Vector3(-100f, -90f, 180f); // 少し下向きに傾ける
    private Vector3 cutRotationVector = new Vector3(-70f, -90f, 180f); // 少し上向きに傾ける
    private Vector3 rightRotationVector = new Vector3(-90f, -90f, 210f); // 少し右向きに傾ける
    private Vector3 leftRotationVector = new Vector3(-90f, -90f, 150f); // 少し左向きに傾ける
    private Quaternion normalRotation;  // 通常のラケットの向き
    private Quaternion driveRotation;  // ドライブのラケットの向き
    private Quaternion cutRotation;     // カットスピンのラケットの向き
    private Quaternion rightRotation;     // 右向きのラケット
    private Quaternion leftRotation;     // 左向きラケット

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
    private float lastTapTime = 0f; // 最後にCボタンが押された時間. 2回連続タップやどれくらいの時間長押しされているかを計測するために用いる
    private float doubleTapThreshold = 0.2f; // Cボタン2回連続タップと判定するための時間
    private float boostSpeed; // 長押しによってラケットに適用される速さ
    private bool isBoostCharging = false; // 2回連続押されたか
    private bool isBoostMoving = false; // 長押しによてラケットの速さが変化中かどうか
    private float boostChargeRatio; // 最大と比較してどれくらい長押ししたかという割合


    /**
    * その他コンポーネントや Unity の設定
    */
    private Rigidbody rb; // ラケットの Rigidbody
    private GameObject ball; // ゲーム内にあるボール
    private Rigidbody ballRb; // ボールの Rigidbody
    private BallMovement ballMovement; // ボールの軌跡を予測するために用いる
    private LineRenderer lineRenderer; // ボールの軌跡を表示するために用いる
    private Image boostSliderImage;
    private float timeScale; // ゲーム内の時間の進む速さ (0 ~ 1)

    void Start()
    {
        // ラケットの傾きに関する変数の初期化
        normalRotation = Quaternion.Euler(normalRotationVector);
        driveRotation = Quaternion.Euler(driveRotationVector);
        cutRotation = Quaternion.Euler(cutRotationVector);
        rightRotation = Quaternion.Euler(rightRotationVector);
        leftRotation = Quaternion.Euler(leftRotationVector);

        // 各種コンポーネントや Unity上の設定を取得
        rb = GetComponent<Rigidbody>();
        ball = GameObject.Find("Ball");
        ballRb = ball.GetComponent<Rigidbody>();
        ballMovement = ball.GetComponent<BallMovement>();
        lineRenderer = ball.GetComponent<LineRenderer>();
        boostSliderImage = boostSlider.GetComponent<Image>();
        timeScale = Time.timeScale;

        // 衝突判定を連続的にする
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

    }

    void Update()
    {
        HandleBoostInput(); // Cボタンの2回連続タップと長押し入力を取得
        UpdateBoostUI(); // 長押し時の UI 更新
        HandleInput(); // 前後左右いどのための入力を取得
    }

    void FixedUpdate()
    {
        // キー入力がある時だけ速度を与え、ない時は止める
        if (!isBoostMoving)
            rb.linearVelocity = moveInput * moveSpeed;
        AdjustPositionToBall(transform.position.x); // ラケットの位置をボールに合わせる
        UpdateRotationDiscrete(); // ラケットの向きを離散的に更新
    }

    void HandleBoostInput(){
        float now = Time.time;
        float tapDuration = now - lastTapTime; // 前回Cボタンが押されてからどれくらいの時間が経過しているか

        // --- Cボタン押下 ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            // ダブルタップ判定
            if (lastTapTime > 0f && tapDuration / timeScale <= doubleTapThreshold)
            {
                isBoostCharging = true;
                // Debug.Log("Double Tap Detected!");
            }
            else
                isBoostCharging = false;

            lastTapTime = now;
        }

        // --- Cボタン長押し中 ---
        if (Input.GetKey(KeyCode.C) && isBoostCharging)
        {
            boostChargeRatio = Mathf.Clamp01(tapDuration / maxChargeTime);
        }

        // --- Cボタン離した時 ---
        if (Input.GetKeyUp(KeyCode.C))
        {
            if (isBoostCharging)
            {
                float boostSpeed = Mathf.Lerp(minMoveSpeed, maxMoveSpeed, boostChargeRatio);
                StartCoroutine(MoveRacketCoroutine(boostSpeed));
                Debug.Log($"Boost Applied: {boostSpeed}");
            }

            boostChargeRatio = 0f;
            isBoostCharging = false;
        }
    }
    void HandleInput()
    {
        moveInput = Vector3.zero;
        if (!isBoostCharging) // ため中は完全停止！
        {
            if (Input.GetKey(KeyCode.W)) moveInput.x += 1;
            if (Input.GetKey(KeyCode.S)) moveInput.x -= 1;
            if (Input.GetKey(KeyCode.A)) moveInput.z += 1;
            if (Input.GetKey(KeyCode.D)) moveInput.z -= 1;
        }
    }
    void UpdateBoostUI()
    {
        boostSliderImage.fillAmount = boostChargeRatio;
    }

    // cボタンを二度押しして長押しした後, ラケットを貯めた分の速さだけ動かす
    IEnumerator MoveRacketCoroutine(float moveSpeed)
    {
        Vector3 initialPos = transform.position;
        isBoostMoving = true;

        rb.linearVelocity = new Vector3(moveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        while (Mathf.Abs(initialPos.x - transform.position.x) < racketMoveDistance)
        {
            yield return new WaitForFixedUpdate(); // 1フレーム分ラケットを動かす
        }

        rb.linearVelocity = Vector3.zero;
        isBoostMoving = false;
    }

    void UpdateRotationDiscrete()
    {
        if (Input.GetKey(KeyCode.UpArrow)) transform.rotation = driveRotation;
        else if (Input.GetKey(KeyCode.DownArrow)) transform.rotation = cutRotation;
        else if (Input.GetKey(KeyCode.RightArrow)) transform.rotation = rightRotation;
        else if (Input.GetKey(KeyCode.LeftArrow)) transform.rotation = leftRotation;
        else transform.rotation = normalRotation;
    }

    void AdjustPositionToBall(float targetX)
    {
        Vector3 pos = transform.position;
        List<Vector3> points = new();

        float? predictedY = ballMovement.SimulateUntilX(ball.transform.position, ballRb.linearVelocity, targetX, points);

        bool ballToPlayer = ballRb.linearVelocity.x < 0;
        if (ballToPlayer)
        {
            // ラケットのy座標を, 弾の軌道から予測したy座標に設定する. predictedYがnullだったらラケットの位置はそのまま.
            pos.y = predictedY ?? pos.y;
            rb.MovePosition(pos);
        }

        // 弾がプレイヤー方向に来ている場合, 弾の軌跡を表示する
        UpdateLineRender(points, ballToPlayer);
    }

    void UpdateLineRender(List<Vector3> points, bool ballToPlayer)
    {
        if (points == null || lineRenderer == null)
            return;

        if (ballToPlayer){
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            lineRenderer.enabled = true;
        }
        else
            lineRenderer.enabled = false;
    }

}
