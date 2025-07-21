using UnityEngine;
using UnityEngine.SceneManagement; // シーンを操作するために必要

public class GameManager : MonoBehaviour
{
    [Header("リセット対象オブジェクト")]
    public BaseRacketController playerRacket; // プレイヤーのラケット
    public BaseRacketController enemyRacket;  // 相手のラケット
    public BaseBallMovement ball;                 // ボール


    // 各オブジェクトの初期位置と回転を保存する変数
    private Vector3 playerInitialPosition;
    private Quaternion playerInitialRotation;
    private Vector3 enemyInitialPosition;
    private Quaternion enemyInitialRotation;
    private Vector3 ballInitialPosition;
    private Quaternion ballInitialRotation;

    void Start()
    {
        // ゲーム開始時に各オブジェクトの初期位置と回転を記憶する
        StoreInitialTransforms();
    }

    void Update()
    {
        // ボールがセットされていなければ何もしない
        if (ball == null) return;

        // ボールのy座標が0より小さくなったらサーブモードに移行
        if (ball.transform.position.y < 0f)
        {
            EnterServeMode();
        }
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

    /// <summary>
    /// サーブモードに切り替えます。
    /// </summary>
    public void EnterServeMode()
    {
        Debug.Log("サーブモードに切り替えます。");

        // 各オブジェクトに、保存しておいた初期位置・回転を渡してリセットを命令
        playerRacket.ResetState(playerInitialPosition, playerInitialRotation);
        enemyRacket.ResetState(enemyInitialPosition, enemyInitialRotation);
        ball.ResetState(ballInitialPosition, ballInitialRotation);
    }
}
