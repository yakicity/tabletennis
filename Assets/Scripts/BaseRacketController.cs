using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseRacketController : MonoBehaviour
{
    /**
    * ラケットの前後の動きに関するパラメータやいや移動のための変数
    */
    protected float moveSpeed = 2.0f; // ラケットの移動速度
    protected Vector3 moveInput = Vector3.zero; // ラケットが動く速さ. 継承先クラスの入力によって変化する

    /**
    * ラケットの傾きに関するパラメータや変数
    */
    public Vector3 baseRotationVector = new Vector3(-90f, -90f, 180f); // 通常時の基本角度
    protected float drivePitchAngle = -15f; // ドライブは基本から-10度
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
    * スマッシュ判定・UI関連
    */
    protected const float SmashHeightThreshold = 1.32f; // この高さ以上でスマッシュ可能
    public GameObject smashUIText; // Inspector で「スマッシュ!」テキストを設定
    protected GameManager gameManager; // Inspector で GameManager をアタッチ

    /**
    * その他コンポーネントや Unity の設定
    */
    protected Rigidbody rb; // ラケットの Rigidbody
    protected GameObject ball; // ゲーム内にあるボール
    protected Rigidbody ballRb; // ボールの Rigidbody
    protected BallMovement ballMovement; // ボールの軌跡を予測するために用いる
    private LineRenderer lineRenderer; // ボールの軌跡を表示するために用いる
    private float timeScale; // ゲーム内の時間の進む速さ
    protected float verticalSpeed = 1.0f;

    /**
    * ラケットの移動範囲制限
    * PlayerRacketの継承先で書き換えて使う。enemyはFixedUpdateではClampPositionを呼ばず、独自のルールで動くため、EnemyRacketの継承先では設定しない。
    */
    protected float minX = 0f;
    protected float maxX = 0f;
    protected float minZ = 0f;
    protected float maxZ = 0f;

    protected virtual void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        if (gameManager == null)
            Debug.LogError("GameManager の取得に失敗しました！");

        // 各種コンポーネントや Unity上の設定を取得
        rb = GetComponent<Rigidbody>();
        ball = GameObject.Find("Ball");
        ballRb = ball.GetComponent<Rigidbody>();
        ballMovement = ball.GetComponent<BallMovement>();
        lineRenderer = ball.GetComponent<LineRenderer>();
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
    }

    protected virtual void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed * verticalSpeed;
        AdjustPositionToBall(transform.position.x); // ラケットの位置をボールに合わせる
        ClampPosition(); // ラケットの移動範囲を制限
    }

    protected void ClampPosition()
    {
        Vector3 currentPos = rb.position;
        float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
        float clampedZ = Mathf.Clamp(currentPos.z, minZ, maxZ);

        if (currentPos.x != clampedX || currentPos.z != clampedZ)
        {
            rb.position = new Vector3(clampedX, currentPos.y, clampedZ);
        }
    }

    public int[] GetAngleIndices()
    {
        return racketFaceIndex;
    }

    protected virtual void AdjustPositionToBall(float targetX)
    {
        Vector3 pos = transform.position;
        List<Vector3> points = new();
        float? predictedY = ballMovement.SimulateUntilX(ball.transform.position, ballRb.linearVelocity, targetX, points);
        bool ballToPlayer = ballRb.linearVelocity.x < 0;

        if (ballToPlayer)
        {
            pos.y = predictedY ?? pos.y;
            rb.MovePosition(pos);
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
        rb.position = initialPosition;
        rb.rotation = initialRotation;

        // Rigidbodyが設定されていれば、動きを完全に止める
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ラケットの角度インデックスもリセットする
        racketFaceIndex[0] = 0;
        racketFaceIndex[1] = 0;

    }

}