using System.Collections.Generic;
using UnityEngine;

public class MoveRacket : MonoBehaviour
{
    private float moveSpeed = 3.5f;    // ラケットの移動速度
    private GameObject ball;

    private Vector3 initialPosition;

    private Quaternion normalRotation;  // 通常のラケットの向き

    // 衝突(離散)
    private float returnSpeed = 5f;

    // 矢印キーでボールを放つZ座標
    private float rightArrowZTarget = -1.74f;
    private float leftArrowZTarget = -0.76f;
    private float xTarget = 1.70f;
    private float zTarget;

    private Vector3 moveInput = Vector3.zero;
    Rigidbody rb;
    Rigidbody ballRb;
    BallMovement ballMovement;
    LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;

        normalRotation = Quaternion.Euler(-90f, -90f, 180f); // ラケットの向きを常に通常に設定

        // 衝突判定をトリガーにする
        // ラケットの物理演算を無効にする（衝突を物理計算に任せないため）
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;


        ball = GameObject.Find("Ball");
        ballRb = ball.GetComponent<Rigidbody>();
        ballMovement = ball.GetComponent<BallMovement>();
        lineRenderer = ball.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput(); // キー入力を取得
        update_target_position(); // ラケットの位置を更新
        // キー入力がある時だけ位置を更新
        if (moveInput != Vector3.zero)
        {
            rb.MovePosition(transform.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
        AdjustPositionToBall(transform.position.x); // ラケットの位置をボールに合わせる
        transform.rotation = normalRotation; // ラケットの向きを常に通常に設定
    }
    void FixedUpdate()
    {

    }
    void HandleInput()
    {
        moveInput = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) moveInput.z += 1;
        if (Input.GetKey(KeyCode.D)) moveInput.z -= 1;
        if (Input.GetKey(KeyCode.W)) moveInput.x += 1;
        if (Input.GetKey(KeyCode.S)) moveInput.x -= 1;
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

    void update_target_position()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            zTarget = rightArrowZTarget;
            Debug.Log("Right Arrow pressed, zTarget: " + zTarget);
        }
        else if (Input.GetKey(KeyCode.LeftArrow)) zTarget = leftArrowZTarget;
        else zTarget = ball.transform.position.z; // 矢印キーが押されていない場合は、卓球台の中心方向へ返す
    }

    // return directionを決定する
    Vector3 CalculateReturnDirection(Vector3 currentBallPosition)
    {
        Vector3 targetPosition = new Vector3(xTarget, currentBallPosition.y, zTarget); // Xはラケットの現在位置、Yはボールの高さを使う
        Debug.Log("targetPosition: " + targetPosition);
        // ボールから目標地点へのベクトルを計算し、正規化する
        Vector3 direction = (targetPosition - currentBallPosition).normalized;
        Debug.Log("direction: " + (targetPosition - currentBallPosition));
        // X方向は常にプレイヤーから相手コート方向へ、Y方向は少し上向きになるように調整
        // direction.x = Mathf.Abs(direction.x); // 常に相手コート方向（-X）
        direction.y = Mathf.Max(direction.y, 0.2f); // 最低でも少し上向きに
        return direction.normalized; // 再度正規化
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            ballRb.useGravity = true;
            if (ballRb != null)
            {
                Vector3 returnDirection = CalculateReturnDirection(ball.transform.position);
                ballRb.linearVelocity = returnDirection * returnSpeed; // 速さを与えてボールを返す

                // ボールの回転処理はBallMovementスクリプトに任せる
                if (ballMovement != null)
                {
                    // 今回の仕様変更では矢印キーが打球方向を決めるため、
                    // スピンの適用はシンプルに固定値にしたり、
                    // 矢印キーの押下状態によって変化させたりすることが考えられます。
                    // 現状は、元のコードでドライブとカットスピンのDebug.Logしかないので、
                    // ここでのスピンの適用は割愛します。必要であれば追加してください。
                    Debug.Log("ボールがラケットに接触しました。");
                }
            }
        }
    }
}