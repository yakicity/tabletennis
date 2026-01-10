using UnityEngine;

public class EnemyAILevel4 : EnemyAIBase
{
    public override float enemyRacketSpeed => 1.3f;
    private float TargetZ = -1.23f; // 台中央のz座標

    public override void AdjustRacketBeforeReturn(GameObject racket, Rigidbody racketRb)
    {
        // level2 では AI はラケットの速さや角度を変更しない
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ)
    {
        float targetZ = Random.Range(-1.95f, -0.45f);
        // float targetZ = -1.95f;
        float returnZ = calculateReturnZ(targetZ, hitPositionZ);
        return returnZ;
    }

    private float calculateReturnZ(float target, float hitPositionZ){
        float VelocityScale = 1.6f;
        float deltaZ = target - hitPositionZ;
        return deltaZ * VelocityScale;
    }
}
