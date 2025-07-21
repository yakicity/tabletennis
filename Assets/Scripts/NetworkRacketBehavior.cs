using Fusion;
using UnityEngine;

public class NetworkRacketBehavior : NetworkBehaviour
{
    // [SerializeField] BaseRacketController baseLogic;

    Rigidbody rb;
    float moveSpeed = 2.0f;
    [SerializeField] private BaseBallMovement baseBallMovement;
    [SerializeField] private PlayerRacketController PlayerRacketController;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (GetInput<RacketInput>(out var input))
        {
            Vector3 moveInput = new Vector3(input.Move.x, input.Move.y, input.Move.z);
                rb.linearVelocity = moveInput * moveSpeed;
            transform.rotation = input.racketFace;
        }
        PlayerRacketController.AdjustPositionToBall(transform.position.x, gameObject); // ラケットの位置をボールに合わせる
    }
    void OnCollisionEnter(Collision collision)
    {
        if (!HasStateAuthority) return; // 自分が StateAuthority を持っている時のみ処理

        if (!collision.gameObject.CompareTag("Ball")) return;

        // // ラケットの傾きや速さ, 現在のボールの速さや回転から, 返球速度やボールの回転速度を計算する
        var returnData = baseBallMovement.CalculateBallReturn(gameObject, collision);

        // // 返球速度とスピンを取得
        Vector3 returnVelocity = returnData.Item1;
        Vector3 returnAngularVelocity = returnData.Item2;

        // // ボールに返球の力を適用
        baseBallMovement.ApplyReturn(returnVelocity, returnAngularVelocity, collision.rigidbody);
    }
}
