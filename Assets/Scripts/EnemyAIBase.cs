using UnityEngine;

public abstract class EnemyAIBase : MonoBehaviour
{
    public virtual float enemyRacketSpeed => 1.0f;
    public abstract void AdjustRacketBeforeReturn(GameObject racket,  GameObject ball);

    public abstract float CalculateReturnVelocityZ(float hitPositionZ);
    public abstract float CalculateReturnVelocityZForServe(float hitPositionZ);
}
