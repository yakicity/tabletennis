using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.Rendering;
using UnityEngine.UI;
using DG.Tweening;
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

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;
        StartHitStop();
        // ShakeRacket(transform, 90f, 1, 0.2f);

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

    public void ShakeRacket(Transform racketTransform, float width, int count, float duration)
    {
        Vector3 iniPos = racketTransform.localPosition;
        Vector3 iniRot = racketTransform.localEulerAngles;
        var seq = DOTween.Sequence();

        float partDuration = duration / count / 2f;
        float widthHalf = width / 2f;

        racketTransform.DOLocalMove(new Vector3(iniPos.x + 0.4f, iniPos.y, iniPos.z), 0.1f);

        seq.Append(racketTransform.DOLocalRotate(
            new Vector3(iniRot.x, iniRot.y - widthHalf, iniRot.z), partDuration));
        seq.Append(racketTransform.DOLocalRotate(iniRot, partDuration));
        racketTransform.DOLocalMove(new Vector3(iniPos.x, iniPos.y, iniPos.z), 0.1f);
    }

    public void StartHitStop()
    {
        StartCoroutine(HitStopCoroutine());
    }
    // コルーチンの内容
    private IEnumerator HitStopCoroutine()
    {
        // Shake(1.0f, 6, 1.0f);
        Time.timeScale = 4.0f;
        yield return new WaitForSecondsRealtime(0.01f);
        Time.timeScale = 1f;

        // ShakeRacket(transform, 10f, 1, 0.4f);
        // Shake(1.0f, 6, 1.0f);
    }

    public void Shake(float width, int count, float duration)
    {
        var camera = Camera.main.transform;
        Vector3 iniPos = camera.localEulerAngles;
        var seq = DOTween.Sequence();
        // 振れ演出の片道の揺れ分の時間
        var partDuration = duration / count / 2f;
        // 振れ幅の半分の値
        var widthHalf = width / 2f;
        // 往復回数-1回分の振動演出を作る
        for (int i = 0; i < count - 1; i++)
        {
            seq.Append(camera.DOLocalRotate(new Vector3(iniPos.x -widthHalf, iniPos.y, iniPos.z), partDuration));
            seq.Append(camera.DOLocalRotate(new Vector3(iniPos.x + widthHalf, iniPos.y, iniPos.z), partDuration));
        }
        // 最後の揺れは元の角度に戻す工程とする
        seq.Append(camera.DOLocalRotate(new Vector3(iniPos.x - widthHalf, iniPos.y, iniPos.z), partDuration));
        seq.Append(camera.DOLocalRotate(iniPos, partDuration));
    }

}