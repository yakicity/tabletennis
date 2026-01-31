using UnityEngine;
public class EnemyAILevel1 : EnemyAIBase
{

    public override void AdjustRacketBeforeReturn(GameObject racket,  GameObject ball){
        // level1 では AI はラケットの速さや角度を変更しない
    }
    
    public override float CalculateReturnVelocityZ(float hitPositionZ, float ballPositionY)
    {
        return 0f;
    }
    public override float CalculateReturnVelocityZForServe(float hitPositionZ)
    {
        return 0f;
    }

}
