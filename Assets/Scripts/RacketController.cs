using Fusion;

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Profiling.Editor;

using UnityEditor.Build.Content;

using UnityEngine;
using UnityEngine.UI;


public class RacketController : NetworkBehaviour
{
    /// <summary>
    /// 操縦戦略（Strategyパターン）を取得
    /// </summary>
    public IControlStrategy GetControlStrategy() => controlStrategy;
    // [SerializeField] private Image boostSliderImage; // ブーストゲージのUI
    private Rigidbody rb;
    private LineRenderer lineRenderer;
    private GameObject ball;

    /**
    * ラケットの前後の動きに関するパラメータや移動のための変数
    */
    private const float moveSpeed = 2.0f;
    private const float MinMoveSpeed = 2.0f;
    private const float MaxMoveSpeed = 4.0f;
    private const float MaxChargeTime = 0.5f;
    private const float RacketMoveDistance = 0.2f;

    /**
    * ネットワーク同期される変数
    */
    [Networked] private Quaternion SyncedRotation { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private TickTimer BoostChargeTimer { get; set; }
    [Networked] private NetworkBool IsBoostMoving { get; set; }

    // --- ローカル変数 ---
    private IControlStrategy controlStrategy; // このラケットの「操縦者」
    private BallController ballController; // ボールの挙動スクリプト
    private float boostChargeRatio = 0f;
    public GameObject gamelauncherobject;
    public GameLauncher gameLauncher;

    /// <summary>
    /// オブジェクトが生成された時に一度だけ呼ばれる
    /// </summary>
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();

        Debug.Log($"Player spawned - HasInputAuthority: {HasInputAuthority}, HasStateAuthority: {Object.HasStateAuthority}, PlayerRef: {Object.InputAuthority}");

        controlStrategy = gameObject.AddComponent<PlayerControlStrategy>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // ボールの取得を継続的に試行
        StartCoroutine(WaitForBall());
    }

    private void TryFindBall()
    {
        if (ball == null)
        {
            // 複数の方法でボールを探す
            ball = GameObject.FindWithTag("Ball");
            if (ball == null)
            {
                ball = GameObject.Find("Ball");
            }

            if (ball != null)
            {
                Debug.Log($"Ball found and set for {gameObject.name}");
                SetBall(ball);
            }
        }
    }
    IEnumerator WaitForBall()
    {
        Debug.Log("Waiting for ball to be available...");
        float timeout = 10f; // 10秒でタイムアウト
        float elapsedTime = 0f;

        while (ball == null && elapsedTime < timeout)
        {
            // 複数の方法でボールを探す

            // 方法1: GameLauncherから取得
            if (gamelauncherobject == null)
            {
                gamelauncherobject = GameObject.Find("GameLauncher");
                if (gamelauncherobject != null)
                {
                    gameLauncher = gamelauncherobject.GetComponent<GameLauncher>();
                }
            }

            if (gameLauncher != null && gameLauncher.ballObj != null)
            {
                ball = gameLauncher.ballObj.gameObject;
                Debug.Log("Ball found via GameLauncher");
            }

            // 方法2: タグで検索
            if (ball == null)
            {
                ball = GameObject.FindWithTag("Ball");
                if (ball != null)
                {
                    Debug.Log("Ball found via FindWithTag");
                }
            }

            // 方法3: 名前で検索
            if (ball == null)
            {
                ball = GameObject.Find("Ball");
                if (ball != null)
                {
                    Debug.Log("Ball found via Find");
                }
            }

            if (ball != null)
            {
                SetBall(ball);
                Debug.Log($"Ball successfully set for {gameObject.name}");
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (ball == null)
        {
            Debug.LogWarning($"Ball not found within timeout for {gameObject.name}");
        }
    }

    /// <summary>
    /// 物理演算のフレームごとに呼ばれる
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // ボールがまだ見つかっていない場合は継続的に探す
        if (ball == null)
        {
            TryFindBall();
        }

        // 操作権限があるオブジェクトだけが「指示」を読み取り、状態を更新する
        if (GetInput<RacketInput>(out var input))
        {
            // ブースト移動中でなければ、指示通りに移動
            if (!IsBoostMoving)
            {
                rb.linearVelocity = input.MoveDirection * moveSpeed;
            }

            // 指示通りにラケットを回転させ、他のクライアントに同期する
            transform.rotation = input.TargetRotation;
            SyncedRotation = input.TargetRotation;

            // ブースト処理
            HandleBoost(input);
        }
        else
        {
            // 操作権限がないクライアントは、同期された回転を適用するだけ
            transform.rotation = SyncedRotation;
        }

        // ボールの軌道予測と位置調整（HasStateAuthorityを持つクライアントのみ）
        if (ball == null)
        {
            return; // ログを減らすためDebug.Logを削除
        }
        if (Object.HasInputAuthority)
        {
            AdjustPositionToBall(transform.position.x);
        }
    }

    // private void Update()
    // {
    //     // UIの更新は毎フレーム行う
    //     UpdateBoostUI();
    // }

    /// <summary>
    /// ネットワーク対応したブースト処理
    /// </summary>
    private void HandleBoost(RacketInput input)
    {
        NetworkButtons buttons = input.Buttons;
        NetworkButtons pressed = buttons.GetPressed(PreviousButtons);
        NetworkButtons released = buttons.GetReleased(PreviousButtons);
        PreviousButtons = buttons;

        // ブーストボタンが2回連続で押されたか判定
        if (pressed.IsSet(0)) // 0番目のボタンをブーストに割り当て
        {
            if (BoostChargeTimer.ExpiredOrNotRunning(Runner))
            {
                // 1回目のタップ
                BoostChargeTimer = TickTimer.CreateFromSeconds(Runner, 0.2f); // ダブルタップの猶予時間
            }
            else
            {
                // ダブルタップ成功。チャージ開始
                BoostChargeTimer = TickTimer.CreateFromSeconds(Runner, MaxChargeTime);
            }
        }

        // ボタンが押され続けていて、チャージ中ならゲージを溜める
        if (buttons.IsSet(0) && !BoostChargeTimer.ExpiredOrNotRunning(Runner))
        {
            boostChargeRatio = 1.0f - (BoostChargeTimer.RemainingTime(Runner) ?? 0f) / MaxChargeTime;
        }

        // ボタンが離されたらブースト発動
        if (released.IsSet(0))
        {
            if (boostChargeRatio > 0.1f) // 一定以上溜まっていたら発動
            {
                float boostSpeed = Mathf.Lerp(MinMoveSpeed, MaxMoveSpeed, boostChargeRatio);
                StartCoroutine(MoveRacketCoroutine(boostSpeed));
            }
            boostChargeRatio = 0f;
            BoostChargeTimer = TickTimer.None;
        }
    }

    // // private void UpdateBoostUI()
    // // {
    // //     if (boostSliderImage != null)
    // //     {
    // //         boostSliderImage.fillAmount = boostChargeRatio;
    // //     }
    // // }

    // Coroutineはネットワークオブジェクトで動かす場合、特別な考慮が必要な場合があるが、
    // 操作権限を持つクライアントでのみ実行されるため、この場合は問題ない
    IEnumerator<WaitForSeconds> MoveRacketCoroutine(float racketMoveSpeed)
    {
        Vector3 initialPos = transform.position;
        IsBoostMoving = true;

        // プレイヤー側（xがマイナス方向）に動くと仮定
        rb.linearVelocity = new Vector3(-racketMoveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        while (Mathf.Abs(initialPos.x - transform.position.x) < RacketMoveDistance)
        {
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }

        rb.linearVelocity = Vector3.zero;
        IsBoostMoving = false;
    }

    /// <summary>
    /// ボールの軌道を予測し、ラケットの高さを自動で合わせる
    /// </summary>
    public void AdjustPositionToBall(float targetX)
    {
        Vector3 pos = transform.position;
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        List<Vector3> points = new();

        // 予測に使用する値をログ出力
        Vector3 networkPos = ballController.NetworkPosition;
        Vector3 networkVel = ballController.NetworkVelocity;

        float? predictedY = ballController.SimulateUntilX(networkPos, networkVel, targetX, points);

        bool ballToPlayer = ballController.NetworkVelocity.x < 0;
        bool ballToLeft = ballController.NetworkVelocity.x < 0;
        bool isLeftRacket = pos.x < 0;

        if (HasInputAuthority && (ballToLeft == isLeftRacket))
        {
            // サーバー・非サーバー問わず、ネットワーク値を使用
            pos.y = predictedY ?? pos.y;
            rb.MovePosition(pos);

            if (predictedY.HasValue)
            {
                Debug.Log($"=== 軌道予測デバッグ ===");
                Debug.Log($"HasStateAuthority: {Object.HasStateAuthority}, HasInputAuthority: {HasInputAuthority}");
                Debug.Log($"Network Position: {networkPos}, Network Velocity: {networkVel}");
                Debug.Log($"Using: Network prediction (consistent across all clients)");
                Debug.Log($"Actual Ball Y: {ballRb.position.y}");
                Debug.Log($"Network Prediction Diff: {Mathf.Abs(predictedY.Value - ballRb.position.y)}");
                Debug.Log($"=========================");
            }
        }

        UpdateLineRender(points, ballToPlayer);
    }

    private void UpdateLineRender(List<Vector3> points, bool ballToPlayer)
    {
        if (points == null || lineRenderer == null) return;

        bool alwaysOn = GameLauncher.CurrentMode == GameMode.AutoHostOrClient;

        if (ballToPlayer || alwaysOn)
        {
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
    public void SetBall(GameObject b)
    {
        if (b == null)
        {
            Debug.LogWarning($"Attempting to set null ball for {gameObject.name}");
            return;
        }

        ball = b;
        ballController = b.GetComponent<BallController>();
        lineRenderer = b.GetComponent<LineRenderer>();

        if (ballController == null)
        {
            Debug.LogWarning($"BallController not found on ball object for {gameObject.name}");
        }
        if (lineRenderer == null)
        {
            Debug.LogWarning($"LineRenderer not found on ball object for {gameObject.name}");
        }

        Debug.Log($"Ball components set successfully for {gameObject.name}");
    }
}