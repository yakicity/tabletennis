using UnityEngine;

public abstract class EnemyAIBase : MonoBehaviour
{
    public virtual float EnemyRacketSpeed => 1.0f;
    public abstract void AdjustRacketBeforeReturn(GameObject racket,  GameObject ball);

    public abstract float CalculateReturnVelocityZ(float hitPositionZ, float ballPositionY);
    public abstract float CalculateReturnVelocityZForServe(float hitPositionZ);
}
