using Fusion;

using UnityEngine;

public class AIControlStrategy : MonoBehaviour, IControlStrategy
{
    // AIのレベルなど、必要なパラメータはここに記述
    // [SerializeField] private EnemyAILevel aiLevel;

    /// <summary>
    /// AIの思考に基づき、RacketInputを生成して返す
    /// </summary>
    public RacketInput GetInput()
    {
        // TODO: ここにAIの思考ロジックを実装する
        // 例：ボールの位置を予測して移動方向を決める
        // 　　相手プレイヤーの位置を見て、返すコース（ラケットの角度）を決める

        Vector3 decidedMove = Vector3.zero; // AIが判断した移動方向
        Quaternion decidedRotation = Quaternion.identity; // AIが判断したラケットの角度

        return new RacketInput
        {
            MoveDirection = decidedMove,
            TargetRotation = decidedRotation,
            Buttons = new NetworkButtons() // AIもブーストを使う場合は、ここでボタンをセットする
        };
    }

    /// <summary>
    /// AIが打ち返す際の、Z方向の速度を計算する
    /// </summary>
    public float CalculateReturnVelocityZ(float currentZ)
    {
        // TODO: ここで相手のいない方向を狙うなどのロジックを実装
        // 例：単純に逆方向に返す
        return -currentZ * 0.5f;
    }
    /// <summary>
    /// AIが返したい仮想インパクト情報を生成
    /// </summary>
    public RacketImpactData GetVirtualImpactData(Collision collision)
    {
        return new RacketImpactData
        {
            RacketVelocity = GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero,
            RacketRotation = transform.rotation,
            ContactNormal = collision.contacts[0].normal,
            RacketTag = gameObject.tag
        };
    }
}