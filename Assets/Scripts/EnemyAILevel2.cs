using UnityEngine;

public class EnemyAILevel2 : EnemyAIBase
{
    private float TargetZ = -1.23f; // 台中央のz座標
    private const float VelocityScale = 4.0f;

    public override void AdjustRacketBeforeReturn(GameObject racket, Rigidbody racketRb)
    {
        // level2 では AI はラケットの速さや角度を変更しない
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ)
    {
        float deltaZ = TargetZ - hitPositionZ;
        return deltaZ * VelocityScale;
    }
}
