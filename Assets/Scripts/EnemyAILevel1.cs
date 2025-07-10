using UnityEngine;
public class EnemyAILevel1 : EnemyAIBase
{

    public override void AdjustRacketBeforeReturn(GameObject racket, Rigidbody racketRb){
        // level1 では AI はラケットの速さや角度を変更しない
    }
    public override float CalculateReturnVelocityZ(float hitPositionZ)
    {
        // Level1 ではそのまま前に返す
        return 0f;
    }

}
