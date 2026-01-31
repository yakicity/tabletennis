using UnityEditor.Build;

using UnityEngine;

public class EnemyAILevel5 : EnemyAIBase
{
    public override float EnemyRacketSpeed => 2.0f;

    public override void AdjustRacketBeforeReturn(GameObject racket,  GameObject ball)
    {
        float racketAdjustAngle = 20.0f;
        float racketAdjustThreshold = 0f;
        float netX = 0.5f;
        bool ballToPlayer;
        bool ballAtPlayerSide;
        Vector3 baseRacketEulerAngles = racket.transform.eulerAngles;
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        EnemyRacketController enemyRacketController = racket.GetComponent<EnemyRacketController>();

        ballToPlayer = ballRb.linearVelocity.x < 0;
        ballAtPlayerSide = ball.transform.position.x < netX;
        if (ballToPlayer || ballAtPlayerSide)
            return;

        Debug.Log("ballRb.angularVelocity.z: " + ballRb.angularVelocity.z);
        // ボールが下回転の時
        if (ballRb.angularVelocity.z < racketAdjustThreshold){
            baseRacketEulerAngles.x -= racketAdjustAngle;
        } 
        // ボールが上回転の時
        else if (ballRb.angularVelocity.z > racketAdjustThreshold){
            baseRacketEulerAngles.x += racketAdjustAngle;
        } 
        racket.transform.eulerAngles = baseRacketEulerAngles;

        enemyRacketController.isAdjust = true;
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ, float ballPositionY)
    {
        float targetZ;
        float ballPositionYThreshold = 1.5f;
        float[] targetZList;
        
        if (ballPositionY < ballPositionYThreshold)
            targetZList = new float[] {-1.95f, -0.45f};
        else
            targetZList = new float[] {-1.75f, -0.65f};
        targetZ = Random.Range(0, 2) == 0 ? targetZList[0] : targetZList[1];
        float returnZ = CalculateReturnZ(targetZ, hitPositionZ);
        return returnZ;
    }
    public override float CalculateReturnVelocityZForServe(float hitPositionZ)
    {
        float targetZ = Random.Range(-1.6f, -0.8f);
        // float targetZ = -1.95f;
        float returnZ = CalculateReturnZ(targetZ, hitPositionZ);
        return returnZ;
    }

    private float CalculateReturnZ(float target, float hitPositionZ){
        float velocityScale = 1.6f;
        float deltaZ = target - hitPositionZ;
        return deltaZ * velocityScale;
    }
}
