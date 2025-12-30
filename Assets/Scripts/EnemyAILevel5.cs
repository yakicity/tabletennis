using UnityEngine;

public class EnemyAILevel5 : EnemyAIBase
{
    private float TargetZ = -1.23f; // 台中央のz座標

    public override void AdjustRacketBeforeReturn(GameObject racket, Rigidbody racketRb)
    {
        // level2 では AI はラケットの速さや角度を変更しない
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ)
    {
        float targetZ;
        float[] targetZList = {-1.95f, -0.45f};
        if (Random.Range(0, 2) == 0){
            targetZ = targetZList[0];
        } else {
            targetZ = targetZList[1];   
        }
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
