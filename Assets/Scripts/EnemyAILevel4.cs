using UnityEngine;

public class EnemyAILevel4 : EnemyAIBase
{
    public override float enemyRacketSpeed => Random.Range(1.0f, 2.0f);
    private float TargetZ = -1.23f; // 台中央のz座標

    public override void AdjustRacketBeforeReturn(GameObject racket,  GameObject ball)
    {
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();

        if (ballRb.linearVelocity.x < 0 || ball.transform.position.x < 0.5)
            return;

        if (Random.Range(0, 10) < 7)
            return;

        Vector3 baseRacketEulerAngles = racket.transform.eulerAngles;
        if (ballRb.angularVelocity.z < 0){
            baseRacketEulerAngles.x -= 20.0f;
            racket.transform.eulerAngles = baseRacketEulerAngles;
        } 
        else if (ballRb.angularVelocity.z > 0){
            baseRacketEulerAngles.x += 20.0f;
            racket.transform.eulerAngles = baseRacketEulerAngles;
        } 
        EnemyRacketController enemyRacketController = racket.GetComponent<EnemyRacketController>();
        enemyRacketController.isAdjust = true;
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ)
    {
        float targetZ = Random.Range(-1.95f, -0.45f);
        float returnZ = calculateReturnZ(targetZ, hitPositionZ);
        return returnZ;
    }

    private float calculateReturnZ(float target, float hitPositionZ){
        float VelocityScale = 1.6f;
        float deltaZ = target - hitPositionZ;
        return deltaZ * VelocityScale;
    }
}
