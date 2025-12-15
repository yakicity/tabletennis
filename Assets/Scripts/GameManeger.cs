using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // --- Publicな変数 ---
    [Header("リセット対象オブジェクト")]
    public BaseRacketController playerRacket;
    public BaseRacketController enemyRacket;
    public BallMovement ball;

    [Header("UI関連")]
    public GameObject scoreTextPlayerObject; // スコアを管理するスクリプト
    public GameObject scoreTextEnemyObject;  // スコアを管理するスクリプト

    private TMP_Text scoreTextPlayer; // プレイヤーのスコアを表示するテキスト
    private TMP_Text scoreTextEnemy;  // 相手のスコアを表示するテキスト
    private int scoreNumPlayer = 0; // スコア変数
    private int scoreNumEnemy = 0; // 相手のスコア変数

    // ▼▼▼ ラリーの状態を管理するenum（ステートマシン） ▼▼▼
    private enum RallyState
    {
        Serve,          // サーブ前
        PlayerJustHit,  // プレイヤーが打った直後
        EnemyJustHit,   // 相手が打った直後
        BouncedOnPlayerCourt, // プレイヤーコートで1バウンド後
        BouncedOnEnemyCourt   // 相手コートで1バウンド後
    }
    private RallyState currentState = RallyState.Serve; // 現在の状態

    // --- 状態管理 ---
    public enum LastHitter { None, Player, Enemy } // publicにしてBallMovementからアクセス可能に
    private LastHitter lastHitter = LastHitter.None;
    private int playerCourtBounceCount = 0;
    private int enemyCourtBounceCount = 0;
    private Vector3 lastBouncePosition; // 最後にバウンドした位置
    private const float MIN_BOUNCE_DISTANCE_SQR = 0.01f; // 2回目バウンドとして判定するための最低距離(の2乗)。0.1f * 0.1f
    private bool isRallyFinished = false; // ラリーが終了したかどうか

    // 各オブジェクトの初期位置と回転を保存する変数
    private Vector3 playerInitialPosition;
    private Quaternion playerInitialRotation;
    private Vector3 enemyInitialPosition;
    private Quaternion enemyInitialRotation;
    private Vector3 ballInitialPosition;
    private Quaternion ballInitialRotation;


    void Start()
    {
        StoreInitialTransforms();
        scoreTextPlayer = scoreTextPlayerObject.GetComponent<TMP_Text>();
        scoreTextEnemy = scoreTextEnemyObject.GetComponent<TMP_Text>();
        UpdateScoreText();
        currentState = RallyState.Serve; // 初期状態はサーブ
    }

    void Update()
    {
        UpdateScoreText(); // スコアテキストを更新
        if (ball == null) return;

        // ボールがアウトになった場合（床に落ちた）
        if (ball.transform.position.y < 0f)
        {
            // 状態に応じて得点者を判断
            switch (currentState)
            {
                case RallyState.PlayerJustHit: // プレイヤーが打ってアウト
                    Debug.Log("プレイヤーが打ってアウト");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    break;
                case RallyState.BouncedOnPlayerCourt: // プレイヤーコートでバウンド後、プレイヤーが返せずアウト
                    Debug.Log("プレイヤーが返せずアウト");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    break;
                case RallyState.EnemyJustHit: // 相手が打ってアウト
                    Debug.Log("相手が打ってアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
                case RallyState.BouncedOnEnemyCourt: // 相手コートでバウンド後、相手が返せずアウト
                    Debug.Log("相手が返せずアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
            }
        }
        if (isRallyFinished)
        {
            EnterServeMode(); // ラリーが終了したらサーブモードに戻す
        }
    }

    // ボールから呼び出される公開メソッド

    public void OnRacketHit(bool wasHitByPlayer)
    {
        switch (currentState)
        {
            case RallyState.Serve:
            case RallyState.BouncedOnPlayerCourt:
            case RallyState.BouncedOnEnemyCourt:
                // Debug.Log($"状態変化: {currentState}");
                currentState = wasHitByPlayer ? RallyState.PlayerJustHit : RallyState.EnemyJustHit;
                // Debug.Log($"状態変化: {currentState}");
                break;

            case RallyState.PlayerJustHit:
                // プレイヤーが打った直後の状態
                if (wasHitByPlayer == false)
                {
                    Debug.Log("相手がノーバウンドで打った");
                    AwardPointToPlayer(); // 相手が打ったボールが直接相手コートに -> Pの得点
                    isRallyFinished = true;
                }
                break;
            case RallyState.EnemyJustHit:
                // 相手が打った直後の状態
                if (wasHitByPlayer == true)
                {
                    Debug.Log("プレイヤーがノーバウンドで打った");
                    AwardPointToEnemy(); // 自分が打ったボールが直接自分コートに -> Eの得点
                    isRallyFinished = true;
                }
                break;
        }
    }

    // ボールから呼び出される公開メソッド
    public void OnCourtBounce(Vector3 bouncePosition)
    {
        bool isPlayerSide = bouncePosition.x < 0.543;
        bool isEnemySide = bouncePosition.x > 0.651;

        switch (currentState)
        {
            // 相手が打った後、初めてのバウンド
            case RallyState.EnemyJustHit:
                if (isPlayerSide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.BouncedOnPlayerCourt; // 正規の1バウンド
                }
                else
                {
                    Debug.Log("相手が打ったボールが直接相手コートに");
                    AwardPointToPlayer(); // 相手が打ったボールが直接相手コートに -> Pの得点
                    isRallyFinished = true;
                }
                break;

            // プレイヤーが打った後、初めてのバウンド
            case RallyState.PlayerJustHit:
                if (isEnemySide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.BouncedOnEnemyCourt; // 正規の1バウンド
                }
                else
                {
                    AwardPointToEnemy(); // 自分が打ったボールが直接自分コートに -> Eの得点
                    isRallyFinished = true;
                    Debug.Log("自分が打ったボールが直接自分コートに");
                }
                break;

            // プレイヤーコートで1バウンドした後、さらにバウンド
            case RallyState.BouncedOnPlayerCourt:
                // lastBouncePositionとbouncePositionの距離が一定以下ならカウントしない

                // プレイヤー陣地で、かつ前回のバウンド位置から十分な距離があるかチェック
                if (isPlayerSide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToEnemy(); // プレイヤーコートで2バウンド -> Eの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: プレイヤーコートでバウンド-----");
                }
                break;

            // 相手コートで1バウンドした後、さらにバウンド
            case RallyState.BouncedOnEnemyCourt:
                // 相手陣地で、かつ前回のバウンド位置から十分な距離があるかチェック
                if (isEnemySide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToPlayer(); // 相手コートで2バウンド -> Pの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: 相手コートでバウンド-----");
                }
                break;
        }
    }

    public void EnterServeMode()
    {
        Debug.Log("サーブモードに切り替えます。");
        playerRacket.ResetState(playerInitialPosition, playerInitialRotation);
        enemyRacket.ResetState(enemyInitialPosition, enemyInitialRotation);
        ball.ResetState(ballInitialPosition, ballInitialRotation);

        // 状態をリセット
        currentState = RallyState.Serve;
        isRallyFinished = false;
    }
    private void AwardPointToPlayer()
    {
        scoreNumPlayer++;
        UpdateScoreText();
        Debug.Log("プレイヤーの得点！");
    }

    private void AwardPointToEnemy()
    {
        scoreNumEnemy++;
        UpdateScoreText();
        Debug.Log("相手の得点！");
    }

    private void UpdateScoreText()
    {
        scoreTextPlayer.text = scoreNumPlayer.ToString();
        scoreTextEnemy.text = scoreNumEnemy.ToString();
    }

    /// <summary>
    /// 各オブジェクトの初期状態を保存します。
    /// </summary>
    private void StoreInitialTransforms()
    {
        playerInitialPosition = playerRacket.transform.position;
        playerInitialRotation = playerRacket.transform.rotation;

        enemyInitialPosition = enemyRacket.transform.position;
        enemyInitialRotation = enemyRacket.transform.rotation;

        ballInitialPosition = ball.transform.position;
        ballInitialRotation = ball.transform.rotation;
    }
}