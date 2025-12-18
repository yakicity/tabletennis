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
        BeforeServe,          // サーブ前
        PlayerServeJustHit, // プレイヤーがサーブ打った直後
        PlayerServeBounceOnPlayerCourt, // プレイヤーサーブがプレイヤーコートで1バウンド後
        PlayerServeBounceOnEnemyCourt,   // プレイヤーサーブが相手コートで1バウンド後
        EnemyJustHit,   // 相手が打った直後
        BouncedOnPlayerCourt, // プレイヤーコートで1バウンド後
        PlayerJustHit,  // プレイヤーが打った直後
        BouncedOnEnemyCourt,   // 相手コートで1バウンド後
        EnemyServeJustHit, // 敵がサーブ打った直後
        EnemyServeBounceOnEnemyCourt,   // 敵サーブが相手コートで1バウンド後
        EnemyServeBounceOnPlayerCourt, // 敵サーブがプレイヤーコートで1バウンド後
    }
    private RallyState currentState = RallyState.BeforeServe; // 現在の状態

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
        currentState = RallyState.BeforeServe; // 初期状態はサーブ
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
                case RallyState.BeforeServe:
                    // サーブ前にアウトなら何も起こらない
                    Debug.Log("サーブ前にアウト");
                    break;
                case RallyState.PlayerServeJustHit: // プレイヤーがサーブを撃った直後にアウト
                    Debug.Log("プレイヤーのサーブがアウト");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    break;
                case RallyState.PlayerServeBounceOnPlayerCourt: // プレイヤーサーブがプレイヤーコートでバウンド後、アウト
                    Debug.Log("プレイヤーのサーブが相手コートに到達せずアウト");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    break;
                case RallyState.PlayerServeBounceOnEnemyCourt: // プレイヤーサーブが相手コートでバウンド後、アウト
                    Debug.Log("相手がサーブを返せずアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
                case RallyState.EnemyJustHit: // 相手が打ってアウト
                    Debug.Log("相手が打ってアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
                case RallyState.BouncedOnPlayerCourt: // プレイヤーコートでバウンド後、プレイヤーが返せずアウト
                    Debug.Log("プレイヤーが返せずアウト");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    break;
                case RallyState.PlayerJustHit: // プレイヤーが打ってアウト
                    Debug.Log("プレイヤーが打ってアウト");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    break;
                case RallyState.BouncedOnEnemyCourt: // 相手コートでバウンド後、相手が返せずアウト
                    Debug.Log("相手が返せずアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
                case RallyState.EnemyServeJustHit: // 相手がサーブを撃った直後にアウト
                    Debug.Log("相手のサーブがアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
                case RallyState.EnemyServeBounceOnEnemyCourt: // 相手サーブが相手コートでバウンド後、アウト
                    Debug.Log("相手のサーブがプレイヤーコートに到達せずアウト");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    break;
                case RallyState.EnemyServeBounceOnPlayerCourt: // 相手サーブがプレイヤーコートでバウンド後、アウト
                    Debug.Log("プレイヤーがサーブを返せずアウト");
                    AwardPointToEnemy();
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
            case RallyState.BeforeServe:
                currentState = wasHitByPlayer ? RallyState.PlayerServeJustHit : RallyState.EnemyServeJustHit;
                break;
            case RallyState.PlayerServeJustHit:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーがサーブを連続で打った");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手がプレイヤーのサーブをコートに入ってないのに打ち返した。これはプレイヤーのサーブミス。");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                break;
            case RallyState.PlayerServeBounceOnPlayerCourt:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーがサーブを連続で打った");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手がノーバウンドでプレイヤーのサーブを打った");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                break;
            case RallyState.PlayerServeBounceOnEnemyCourt:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーがサーブを連続で打った");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("プレイヤーのサーブを相手が打ち返した");
                    currentState = RallyState.EnemyJustHit;
                }
                break;
            case RallyState.EnemyJustHit:
                if (wasHitByPlayer)
                {
                    Debug.Log("相手がプレイヤーのコートに入れる前にプレイヤーが撃ってしまった。プレイヤーの得点。");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手が2連続で撃った。相手のミス。プレイヤーの得点。");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                break;
            case RallyState.BouncedOnPlayerCourt:
                if (wasHitByPlayer)
                {
                    Debug.Log("相手のラリーをプレイヤーが打ち返した");
                    currentState = RallyState.PlayerJustHit;
                }
                else
                {
                    Debug.Log("相手が連続で打った。相手のミス。プレイヤーの得点。");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                break;
            case RallyState.PlayerJustHit:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーが連続で打った。プレイヤーのミス。相手の得点。");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("プレイヤーが相手のコートに入れる前に相手が撃ってしまった。相手の得点。");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                break;
            case RallyState.BouncedOnEnemyCourt:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーが連続で打った。プレイヤーのミス。相手の得点。");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手がラリーを打ち返した");
                    currentState = RallyState.EnemyJustHit;
                }
                break;
            case RallyState.EnemyServeJustHit:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーが相手のサーブをコートに入ってないのに打ち返した。これは相手ーのサーブミス。");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手がサーブを連続で打った");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                break;
            case RallyState.EnemyServeBounceOnEnemyCourt:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーがノーバウンドで相手のサーブを打った");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手がサーブを連続で打った");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                break;
            case RallyState.EnemyServeBounceOnPlayerCourt:
                if (wasHitByPlayer)
                {
                    Debug.Log("プレイヤーが相手のサーブを打ち返した");
                    currentState = RallyState.PlayerJustHit;
                }
                else
                {
                    Debug.Log("相手がサーブを連続で打った");
                    AwardPointToPlayer();
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
            case RallyState.PlayerServeJustHit:
                if (isEnemySide)
                {
                    AwardPointToEnemy();
                    isRallyFinished = true;
                    Debug.Log("自分が打ったサーブが直接自分コートに");
                }
                else if (isPlayerSide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.PlayerServeBounceOnPlayerCourt; // 正規の1バウンド
                }
                else
                {
                    Debug.Log("プレイヤーが打ったサーブがネットに");
                    isRallyFinished = true;
                }
                break;
            case RallyState.PlayerServeBounceOnPlayerCourt:
                // lastBouncePositionとbouncePositionの距離が一定以下ならカウントしない
                // プレイヤー陣地で、かつ前回のバウンド位置から十分な距離があるかチェック
                if (isPlayerSide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToEnemy(); // プレイヤーコートで2バウンド -> Eの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: プレイヤーのサーブがプレイヤーコートでバウンド-----");
                }
                else if (isEnemySide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.PlayerServeBounceOnEnemyCourt; // 正規の1バウンド
                }
                else
                {
                    Debug.Log("プレイヤーが打ったサーブがネットに");
                    isRallyFinished = true;
                }
                break;
            case RallyState.PlayerServeBounceOnEnemyCourt:
                if (isEnemySide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToPlayer(); // 相手コートで2バウンド -> Pの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: 相手コートでプレイヤーのサーブがバウンド-----");
                }
                else if (isPlayerSide)
                {
                    Debug.Log("相手がプレイヤーのサーブを返せなかった");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("プレイヤーのサーブがネットに");
                }
                break;
            // 相手が打った後、初めてのバウンド
            case RallyState.EnemyJustHit:
                if (isPlayerSide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.BouncedOnPlayerCourt; // 正規の1バウンド
                }
                else if (isEnemySide)
                {
                    Debug.Log("相手が打ったボールが直接相手コートに");
                    AwardPointToPlayer(); // 相手が打ったボールが直接相手コートに -> Pの得点
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手が打ったボールがネットに");
                }
                break;
            case RallyState.BouncedOnPlayerCourt:
                // lastBouncePositionとbouncePositionの距離が一定以下ならカウントしない
                // プレイヤー陣地で、かつ前回のバウンド位置から十分な距離があるかチェック
                if (isPlayerSide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToEnemy(); // プレイヤーコートで2バウンド -> Pの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: プレイヤーコートでバウンド-----");
                }
                else if (isEnemySide)
                {
                    AwardPointToEnemy(); // プレイヤーコートで2バウンド -> Pの得点
                    isRallyFinished = true;
                    Debug.Log("相手が打ったボールをプレイヤーが返せなかった");
                }
                else
                {
                    Debug.Log("相手が打ったボールがネットに");
                }
                break;
            // プレイヤーが打った後、初めてのバウンド
            case RallyState.PlayerJustHit:
                if (isEnemySide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.BouncedOnEnemyCourt; // 正規の1バウンド
                }
                else if (isPlayerSide)
                {
                    AwardPointToEnemy(); // 自分が打ったボールが直接自分コートに -> Eの得点
                    isRallyFinished = true;
                    Debug.Log("自分が打ったボールが直接自分コートに");
                }
                else
                {
                    Debug.Log("プレイヤーが打ったボールがネットに");
                }
                break;
            case RallyState.BouncedOnEnemyCourt:
                if (isEnemySide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToPlayer(); // 相手コートで2バウンド -> Pの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: 相手コートでバウンド-----");
                }
                else if (isPlayerSide)
                {
                    Debug.Log("プレイヤーが打ったボールを相手が返せなかった");
                    AwardPointToPlayer();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("プレイヤーが打ったボールがネットに");
                }
                break;
            case RallyState.EnemyServeJustHit:
                if (isPlayerSide)
                {
                    AwardPointToPlayer();
                    isRallyFinished = true;
                    Debug.Log("相手が打ったサーブが直接相手コートに");
                }
                else if (isEnemySide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.EnemyServeBounceOnEnemyCourt; // 正規の1バウンド
                }
                else
                {
                    Debug.Log("相手が打ったサーブがネットに");
                    isRallyFinished = true;
                }
                break;
            case RallyState.EnemyServeBounceOnEnemyCourt:
                // lastBouncePositionとbouncePositionの距離が一定以下ならカウントしない
                // 相手陣地で、かつ前回のバウンド位置から十分な距離があるかチェック
                if (isEnemySide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToPlayer(); // 相手コートで2バウンド -> Pの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: 相手のサーブが相手コートでバウンド-----");
                }
                else if (isPlayerSide)
                {
                    lastBouncePosition = bouncePosition;
                    currentState = RallyState.EnemyServeBounceOnPlayerCourt; // 正規の1バウンド
                }
                else
                {
                    Debug.Log("相手が打ったサーブがネットに");
                    isRallyFinished = true;
                }
                break;
            case RallyState.EnemyServeBounceOnPlayerCourt:
                if (isPlayerSide && (bouncePosition - lastBouncePosition).sqrMagnitude > MIN_BOUNCE_DISTANCE_SQR)
                {
                    AwardPointToEnemy(); // プレイヤーコートで2バウンド -> Eの得点
                    isRallyFinished = true;
                    Debug.Log("2バウンド目: プレイヤーコートで相手のサーブがバウンド-----");
                }
                else if (isEnemySide)
                {
                    Debug.Log("相手が打ったサーブをプレイヤーが返せなかった");
                    AwardPointToEnemy();
                    isRallyFinished = true;
                }
                else
                {
                    Debug.Log("相手のサーブがネットに");
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
        currentState = RallyState.BeforeServe;
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