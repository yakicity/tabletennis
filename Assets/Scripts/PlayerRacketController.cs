using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class PlayerRacketController : BaseRacketController
{

    private float verticalSpeedTimer = 0.0f;
    private float verticalSpeedMin = 1.0f;
    private float verticalSpeedMax = 4.0f;
    private float accelerationDuration = 1.0f; // 何秒で最大になるか
    private float lastPushWTime = 0.0f;
    private float lastPushSTime = 0.0f;
    private bool isWkeyHeld = false;
    private bool isSkeyHeld = false;
    // スマッシュ可能状態
    private bool isSmashAvailable = false;

    protected override void Start()
    {
        base.Start(); // 親クラス（BaseRacketController）の初期化処理を必ず呼ぶ

        // 移動範囲の制限値を設定
        // X: 奥行き (手前〜奥)
        minX = -2.0f; // プレイヤーの後ろ側の限界
        maxX = 0.65f; // ネット手前の限界

        // Z: 横幅 (左〜右)
        minZ = -3.0f; // 左側の限界
        maxZ =  0.8f; // 右側の限界
    }
    // Updateメソッドをオーバーライド（上書き）して、
    // ベースクラスのUpdate処理を呼び出した後に、移動入力処理を行う
    protected override void Update()
    {
        base.Update(); // BaseRacketControllerのUpdate()を実行
        UpdateRotationDiscrete(); // ラケットの向きを離散的に更新
        HandleInput(); // このクラス固有の移動入力処理を実行
        UpdateSmashUI(); // スマッシュUI更新
    }

    /// <summary>
    /// W,A,S,Dキーによる移動入力を受け付け、moveInput変数を更新する
    /// </summary>
    void HandleInput()
    {
        bool up = Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.S);
        bool verticalHeld = (isWkeyHeld && up) || (isSkeyHeld && down);

        if (verticalHeld)
        {
            float now = Time.time;
            float lastPushTime = up ? lastPushWTime : lastPushSTime;
            float t = Mathf.Clamp01((now - lastPushTime) / accelerationDuration);
            verticalSpeed = Mathf.Lerp(verticalSpeedMin, verticalSpeedMax, t);
            // Debug.Log("verticalSpeedTimer: " + verticalSpeedTimer + ", verticalSpeed: " + verticalSpeed+ " t: " + t);

            if (up) moveInput.x += 1;
            if (down) moveInput.x -= 1;
        }
        else if(up || down)
        {
            if (up)
            {
                lastPushWTime = Time.time;
                isWkeyHeld = true;
            }
            else if (down)
            {
                lastPushSTime = Time.time;
                isSkeyHeld = true;
            }
        }
        else
        {
            isWkeyHeld = false;
            isSkeyHeld = false;
            verticalSpeed = verticalSpeedMin;
        }

        moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.x += 0.5f;
        if (Input.GetKey(KeyCode.S)) moveInput.x -= 0.5f;
        if (Input.GetKey(KeyCode.A)) moveInput.z += 1;
        if (Input.GetKey(KeyCode.D)) moveInput.z -= 1;
    }

    void UpdateRotationDiscrete()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && racketFaceIndex[0] < 1) racketFaceIndex[0]++;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && racketFaceIndex[0] > -1) racketFaceIndex[0]--;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && racketFaceIndex[1] < 2) racketFaceIndex[1]++;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && racketFaceIndex[1] > -2) racketFaceIndex[1]--;

        // 現在のracketFaceIndexに基づいてラケットの向きを更新する
        transform.rotation = CalculateTargetRotation();
    }
    private Quaternion CalculateTargetRotation()
    {
        // 1. 基本となる回転角度からスタート
        Vector3 targetEulerAngles = baseRotationVector;

        // 2. ドライブ/カットの状態に応じてピッチ角（X軸）を調整
        if (racketFaceIndex[0] == 1) // ドライブ
        {
            targetEulerAngles.x += drivePitchAngle;
        }
        else if (racketFaceIndex[0] == -1) // カット
        {
            targetEulerAngles.x += cutPitchAngle;
        }
        // 3. 左右の状態に応じてロール角（Z軸）を調整
        // これにより、-2, -1, 0, 1, 2 のすべての段階に対応できる
        targetEulerAngles.z += racketFaceIndex[1] * rollAnglePerLevel;
        // 4. 計算されたオイラー角から最終的なQuaternionを生成して返す
        return Quaternion.Euler(targetEulerAngles);
    }
    /// <summary>
    /// ボールの高さに応じてスマッシュUIを表示/非表示
    /// </summary>
    private void UpdateSmashUI()
    {
        if (ball == null) return;
        GameManager.RallyState currentState = gameManager.GetCurrentRallyState();
        // スマッシュ可能な状態か判定
        // - ボールが一定の高さ以上
        // - プレイヤーが打つ番（自陣にバウンド後）
        bool ballHighEnough = ball.transform.position.y >= SmashHeightThreshold;
        // bool ballonTable = ball.transform.position.x > -0.9;
        bool ballonTable = true;
        bool isPlayerTurn = (currentState == GameManager.RallyState.BouncedOnPlayerCourt ||
                            currentState == GameManager.RallyState.EnemyServeBounceOnPlayerCourt);

        isSmashAvailable = ballHighEnough && isPlayerTurn && ballonTable;


        // スマッシュUI表示切替
        if (smashUIText != null)
        {
            smashUIText.SetActive(isSmashAvailable);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        BallMovement ballMovement = collision.gameObject.GetComponent<BallMovement>();
        // スマッシュ判定: ボールが高い位置 + Wキーが押されている
        bool isSmash = isSmashAvailable && Input.GetKey(KeyCode.W);
        // ラケットの傾きや速さ, 現在のボールの速さや回転から, 返球速度やボールの回転速度を計算する
        var returnData = ballMovement.CalculateBallReturn(gameObject, collision);

        if (isSmash)
        {
            // スマッシュ返球（高速・低弾道）
            Debug.Log("スマッシュ!");
            // 現在のボール位置
            Vector3 hitPosition = collision.gameObject.transform.position;
            // スマッシュ速度を計算（直線的にターゲットへ）
            Vector3 returnVelocity = ballMovement.CalculateSmashVelocity(hitPosition, racketFaceIndex[1]);
            // 返球するボールの回転速度
            Vector3 returnAnglarVelocity = returnData.Item2;
            Debug.Log($"Smash Return Velocity: {returnVelocity}");
            ballMovement.ApplyReturn(returnVelocity, returnAnglarVelocity);

        }
        else
        {
            // ラケットの傾きや速さ, 現在のボールの速さや回転から, 返球速度やボールの回転速度を計算する
            // 返球速度
            Vector3 returnVelocity = returnData.Item1;
            Debug.Log($"Player Return Velocity: {returnVelocity}");
            // 返球するボールの回転速度
            Vector3 returnAnglarVelocity = returnData.Item2;
            ballMovement.ApplyReturn(returnVelocity, returnAnglarVelocity);
        }
    }

}