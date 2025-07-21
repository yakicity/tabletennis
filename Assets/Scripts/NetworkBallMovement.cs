using Fusion;
using UnityEngine;

public class NetworkBallMovement : NetworkBehaviour
{
    private Rigidbody rb;
    [SerializeField] private BaseBallMovement baseBallMovement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }
    public override void Spawned()
    {
        base.Spawned();
        rb = GetComponent<Rigidbody>();
        baseBallMovement.InitializeParameter(rb);
        Debug.Log("initialized!");
    }

    public override void FixedUpdateNetwork()
    {
        baseBallMovement.ApplyMagnusEffect(rb); // マグナス力の適用
    }

    // Update is called once per frame
    void OnCollisionEnter(Collision collision)
    {
        // ラケットがボールに触れたら重力を付与
        rb.useGravity = true;
    }
}
