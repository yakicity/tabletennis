using UnityEngine;

public class EnemyAILevel3 : EnemyAIBase
{
    private float TargetZ = -1.23f; // 台中央のz座標
    private const float VelocityScale = 3.5f;

    public override void AdjustRacketBeforeReturn(GameObject racket,  GameObject ball)
    {
        // level3 では AI はラケットの速さや角度を変更しない
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ)
    {
        float deltaZ = TargetZ - hitPositionZ;
        return deltaZ * VelocityScale;
    }
    public override float CalculateReturnVelocityZForServe(float hitPositionZ)
    {
        float deltaZ = TargetZ - hitPositionZ;
        return deltaZ * VelocityScale;
    }
}
