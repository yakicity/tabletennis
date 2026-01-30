using System.Collections;
using UnityEngine;
public enum EnemyAILevel
{
    Level1,
    Level2,
    Level3,
    Level4,
    Level5
}
public class EnemyRacketController : BaseRacketController
{
    [SerializeField] private EnemyAILevel aiLevel = EnemyAILevel.Level1;

    private EnemyAIBase enemyAI;
    private GameManager gameManager;
    private bool serveRoutineStarted = false;
    private Vector3 initialPosition;
    private float serveSpeed = -2.0f;
    private float serveMove = 0.3f;
    private float serveWaitTime = 3.0f;
    private float restoreWaitTime = 0.2f;
    private RigidbodyConstraints originalConstraints;
    public bool isAdjust = false;
    private Vector3 originalEulerAngles;


    protected override void Start()
    {
        base.Start(); // BaseRacketControllerのUpdate()を実行
        originalEulerAngles = gameObject.transform.eulerAngles;

        if (GameData.CpuLevel == 0)
        {
            switch (aiLevel)
            {
                case EnemyAILevel.Level1:
                    enemyAI = gameObject.AddComponent<EnemyAILevel1>();
                    break;
                case EnemyAILevel.Level2:
                    enemyAI = gameObject.AddComponent<EnemyAILevel2>();
                    break;
                case EnemyAILevel.Level3:
                    enemyAI = gameObject.AddComponent<EnemyAILevel3>();
                    break;
                case EnemyAILevel.Level4:
                    enemyAI = gameObject.AddComponent<EnemyAILevel4>();
                    break;
                case EnemyAILevel.Level5:
                    enemyAI = gameObject.AddComponent<EnemyAILevel5>();
                    break;
                default:
                    Debug.LogError("Unknown AI Level");
                    break;
            }
        }
        else if (GameData.CpuLevel == 1)
        {
            enemyAI = gameObject.AddComponent<EnemyAILevel1>();
        }
        else if (GameData.CpuLevel == 2)
        {
            enemyAI = gameObject.AddComponent<EnemyAILevel2>();
        }
        else if (GameData.CpuLevel == 3)
        {
            enemyAI = gameObject.AddComponent<EnemyAILevel3>();
        }
        else if (GameData.CpuLevel == 4)
        {
            enemyAI = gameObject.AddComponent<EnemyAILevel4>();
        }
        else if (GameData.CpuLevel == 5)
        {
            enemyAI = gameObject.AddComponent<EnemyAILevel5>();
        }
        else
        {
            Debug.LogError("Invalid CPU Level in GameData");
        }

        if (enemyAI == null)
            Debug.LogError("EnemyAIBase の取得に失敗しました！");

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        if (gameManager == null)
            Debug.LogError("GameManager の取得に失敗しました！");
        initialPosition = transform.position;
        originalConstraints = rb.constraints;
        rb.constraints = originalConstraints | RigidbodyConstraints.FreezePositionX;
    }
    // FixedUpdate
    void FixedUpdate()
    {
        // ラケットの位置をボールに合わせる処理
        AdjustPositionToBall(transform.position.x);

        // サーブ担当の確認
        GameManager.ServeStarter serverForNext = gameManager.GetServerForNextServe();
        bool isEnemyServe = serverForNext == GameManager.ServeStarter.Enemy && gameManager.GetCurrentRallyState() == GameManager.RallyState.BeforeServe;

        if (isEnemyServe && !serveRoutineStarted)
        {
            serveRoutineStarted = true;
            Debug.Log("Enemy Serve Start");
            StartCoroutine(EnemyServeAfterDelay());
        }

        if (!isEnemyServe)
        {
            serveRoutineStarted = false;
        }

        if (!isAdjust){
            GameObject ball = GameObject.Find("Ball");
            enemyAI.AdjustRacketBeforeReturn(gameObject, ball);
        }

    }

    // baseクラスのものは、味方のラケット用のAdjustPositionToBallである。オーバーライドする必要がある。
    void AdjustPositionToBall(float targetX)
    {
        if (ball == null || ballRb == null || ballMovement == null)
            return;

        Vector3 pos = transform.position;
        float speed = enemyAI.enemyRacketSpeed;
        pos.z = Mathf.MoveTowards(pos.z, ball.transform.position.z, speed * Time.fixedDeltaTime);

        float? predictedY = ballMovement.SimulateUntilX(ball.transform.position, ballRb.linearVelocity, targetX);

        if (ballRb.linearVelocity.x > 0)
        {
            pos.y = predictedY ?? pos.y;  // ラケットのy座標を, 弾の軌道から予測したy座標に設定する. predictedYがnullだったらラケットの位置はそのまま.
            rb.MovePosition(pos);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        BallMovement ballMovement = collision.gameObject.GetComponent<BallMovement>();

        float xSpeed = enemyAI.enemyRacketSpeed;

        // ラケットの傾きや速さ, 現在のボールの速さや回転から, 返球速度やボールの回転速度を計算する
        var returnData = ballMovement.CalculateBallReturn(gameObject, collision, xSpeed);

        // 返球速度
        Vector3 returnVelocity = new()
        {
            x = returnData.Item1.x * -1,
            y = returnData.Item1.y,
            z = enemyAI.CalculateReturnVelocityZ(rb.transform.position.z)
        };

        // 返球するボールの回転速度
        Vector3 returnAnglarVelocity = returnData.Item2;
        Debug.Log($"Enemy Return Velocity: {returnVelocity}, Angular Velocity: {returnAnglarVelocity}");

        // ボールに計算結果を適用する
        ballMovement.ApplyReturn(returnVelocity, returnAnglarVelocity);
        StartCoroutine(EnemyAngleRestoreAfterDelay());
        isAdjust = false;
    }

    private IEnumerator EnemyServeAfterDelay()
    {
        yield return new WaitForSeconds(serveWaitTime);
        rb.constraints = originalConstraints;

        rb.linearVelocity = new Vector3(serveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        while (Mathf.Abs(initialPosition.x - transform.position.x) < serveMove)
        {
            yield return new WaitForFixedUpdate(); // 1フレーム分ラケットを動かす
        }

        rb.MovePosition(GameManager.PlayerServe_EnemyPos);
        rb.linearVelocity = Vector3.zero;
        rb.constraints = originalConstraints | RigidbodyConstraints.FreezePositionX;
    }
    private IEnumerator EnemyAngleRestoreAfterDelay()
    {
        yield return new WaitForSeconds(restoreWaitTime);
        gameObject.transform.eulerAngles = originalEulerAngles;
    }
}
