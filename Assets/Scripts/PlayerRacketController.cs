using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
// using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class PlayerRacketController : BaseRacketController
{
    // Updateメソッドをオーバーライド（上書き）して、
    // ベースクラスのUpdate処理を呼び出した後に、移動入力処理を行う
    protected override void Update()
    {
        base.Update(); // BaseRacketControllerのUpdate()を実行
        UpdateRotationDiscrete(); // ラケットの向きを離散的に更新
        HandleInput(); // このクラス固有の移動入力処理を実行
    }

    /// <summary>
    /// W,A,S,Dキーによる移動入力を受け付け、moveInput変数を更新する
    /// </summary>
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

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        BallMovement ballMovement = collision.gameObject.GetComponent<BallMovement>();

        // ラケットの傾きや速さ, 現在のボールの速さや回転から, 返球速度やボールの回転速度を計算する
        var returnData = ballMovement.CalculateBallReturn(gameObject, collision);

        // 返球速度
        Vector3 returnVelocity = returnData.Item1;

        // 返球するボールの回転速度
        Vector3 returnAnglarVelocity = returnData.Item2;

        // ボールに計算結果を適用する
        ballMovement.ApplyReturn(returnVelocity, returnAnglarVelocity);
    }

}