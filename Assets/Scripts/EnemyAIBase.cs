using UnityEngine;

public abstract class EnemyAIBase : MonoBehaviour
{
    public abstract void AdjustRacketBeforeReturn(GameObject racket, Rigidbody racketRb);

    public abstract float CalculateReturnVelocityZ(float hitPositionZ);
}
