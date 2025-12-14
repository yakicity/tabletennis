using Fusion;
using UnityEngine;

public class PlayerControlStrategy : MonoBehaviour, IControlStrategy
{
    [Header("回転パラメータ")]
    private Vector3 baseRotationVector = new Vector3(-90f, -90f, 180f);
    private float drivePitchAngle = -10f;
    private float cutPitchAngle = 20f;
    private float rollAnglePerLevel = 20f;

    // 0: drive/cut,  1: right/left
    private int[] racketFaceIndex = new int[2];

    /// <summary>
    /// 毎フレームのキー入力を読み取り、RacketInputとして返す
    /// </summary>
    public RacketInput GetInput()
    {
        // --- 移動入力 ---
        var moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) moveInput.x += 1; // 前進
        if (Input.GetKey(KeyCode.S)) moveInput.x -= 1; // 後退
        if (Input.GetKey(KeyCode.A)) moveInput.z += 1; // 左
        if (Input.GetKey(KeyCode.D)) moveInput.z -= 1; // 右

        // --- 回転入力 ---
        UpdateRotationIndices();

        // --- ボタン入力 ---
        // 0番目のボタンにブースト(Cキー)を割り当てる
        var buttons = new NetworkButtons();
        if (Input.GetKey(KeyCode.C))
        {
            buttons.Set(0, true);
        }
        
        return new RacketInput
        {
            MoveDirection = moveInput.normalized,
            TargetRotation = CalculateTargetRotation(),
            Buttons = buttons
        };
    }

    /// <summary>
    /// 矢印キーでラケットの角度インデックスを更新する
    /// </summary>
    private void UpdateRotationIndices()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && racketFaceIndex[0] < 1) racketFaceIndex[0]++;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && racketFaceIndex[0] > -1) racketFaceIndex[0]--;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && racketFaceIndex[1] < 2) racketFaceIndex[1]++;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && racketFaceIndex[1] > -2) racketFaceIndex[1]--;
    }

    /// <summary>
    /// 現在の角度インデックスから、最終的なラケットのQuaternionを計算する
    /// </summary>
    private Quaternion CalculateTargetRotation()
    {
        Vector3 targetEulerAngles = baseRotationVector;

        // ドライブ/カットに応じてX軸回転を調整
        if (racketFaceIndex[0] == 1) // ドライブ
            targetEulerAngles.x += drivePitchAngle;
        else if (racketFaceIndex[0] == -1) // カット
            targetEulerAngles.x += cutPitchAngle;

        // 左右の傾きに応じてZ軸回転を調整
        targetEulerAngles.z += racketFaceIndex[1] * rollAnglePerLevel;

        return Quaternion.Euler(targetEulerAngles);
    }
}