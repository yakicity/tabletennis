using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    private NetworkRunner networkRunnerPrefab;
    [SerializeField]
    private NetworkPrefabRef playerRacketPrefab;
    [SerializeField]
    private NetworkPrefabRef ballPrefab;
    [SerializeField]
    private TextMeshProUGUI comingText;

    private NetworkRunner networkRunner;
    Vector3 BallPosition = new Vector3(-0.8f, 1.3f, -1.188f);
    Vector3 Player1Position = new Vector3(-0.9f, 1.3f, -1.123f);
    Vector3 Player1Rotation = new Vector3(-90f, -90f, 180f);
    Vector3 Player2Position = new Vector3(2.0f, 1.3f, -1.123f);
    Vector3 Player2Rotation = new Vector3(-90f, 90f, 180f);
    /**
    * ラケットの傾きに関するパラメータや変数
    */
    protected Vector3 baseRotationVector = new Vector3(-90f, -90f, 180f); // 通常時の基本角度
    protected float drivePitchAngle = -10f; // ドライブは基本から-10度
    protected float cutPitchAngle = 20f;   // カットは基本から+20度
    protected float rollAnglePerLevel = 20f; // 1段階あたり20度傾く
    protected int[] racketFaceIndex = new int[2]; // ラケットの向きのインデックス. 0: drive cut,  1: right left

    private async void Start()
    {
        // NetworkRunnerを生成する
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.AddCallbacks(this);
        // StartGameArgsに渡した設定で、セッションに参加する
        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SceneManager = networkRunner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok){
            Debug.Log("成功！");
        } else{
            Debug.Log("失敗");
        }
        racketFaceIndex[0] = 0; // 通常のラケットの向き
        racketFaceIndex[1] = 0; // 通常のラケットの向き

        // int name = UnityEngine.Random.Range(0, 10000);
        // comingText.text = $"Player{name}: new hage coming!";
    }
    // INetworkRunnerCallbacksインターフェースの空実装

    private Quaternion CalculateTargetRotation()
    {
        // 1. 基本となる回転角度からスタート
        Vector3 targetEulerAngles = baseRotationVector;

        // 2. ドライブ/カットの状態に応じてピッチ角（X軸）を調整
        if (racketFaceIndex[0] == 1) // ドライブ
        {
            targetEulerAngles.x += drivePitchAngle;
        }
        else if (racketFaceIndex[0] == -1) // カット
        {
            targetEulerAngles.x += cutPitchAngle;
        }
        // 3. 左右の状態に応じてロール角（Z軸）を調整
        // これにより、-2, -1, 0, 1, 2 のすべての段階に対応できる
        targetEulerAngles.z += racketFaceIndex[1] * rollAnglePerLevel;
        // 4. 計算されたオイラー角から最終的なQuaternionを生成して返す
        return Quaternion.Euler(targetEulerAngles);
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        if (!runner.IsServer) { return; }
        // ホスト（サーバー兼クライアント）かどうかはIsServerで判定できる
        int activePlayers = runner.ActivePlayers.Count();
        int name = UnityEngine.Random.Range(0, 10000);

        comingText.text = $"Player{name}: new hage coming!";
        Debug.Log($"player number: {runner.ActivePlayers.Count()}");

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        // ランダムな生成位置（半径5の円の内部）を取得する
        var randomValue = UnityEngine.Random.insideUnitCircle * 5f;
        if (activePlayers == 1){
            spawnPosition = Player1Position;
            spawnRotation = Quaternion.Euler(Player1Rotation);

            runner.Spawn(ballPrefab, BallPosition, Quaternion.identity, null);
        }
        else {
            spawnPosition = Player2Position;
            spawnRotation = Quaternion.Euler(Player2Rotation);
        }

        // 参加したプレイヤーのアバターを生成する
        var avatar = runner.Spawn(playerRacketPrefab, spawnPosition, spawnRotation, player);
        Debug.Log($"spawnPosition: {spawnPosition}");
        // プレイヤー（PlayerRef）とアバター（NetworkObject）を関連付ける
        runner.SetPlayerObject(player, avatar);
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (!runner.IsServer) { return; }
        // 退出したプレイヤーのアバターを破棄する
        if (runner.TryGetPlayerObject(player, out var avatar))
        {
            runner.Despawn(avatar);
        }
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){}
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason){}
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason){}
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token){}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason){}
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message){}
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){}
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){}
    public void OnInput(NetworkRunner runner, NetworkInput input){
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move.x += 1;
        if (Input.GetKey(KeyCode.S)) move.x -= 1;
        if (Input.GetKey(KeyCode.A)) move.z += 1;
        if (Input.GetKey(KeyCode.D)) move.z -= 1;

        if (Input.GetKeyDown(KeyCode.UpArrow) && racketFaceIndex[0] < 1) racketFaceIndex[0]++;
        else if (Input.GetKeyDown(KeyCode.DownArrow) && racketFaceIndex[0] > -1) racketFaceIndex[0]--;
        else if (Input.GetKeyDown(KeyCode.RightArrow) && racketFaceIndex[1] < 2) racketFaceIndex[1]++;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && racketFaceIndex[1] > -2) racketFaceIndex[1]--;
        Quaternion racketFace = CalculateTargetRotation();

        input.Set(new RacketInput
        {
            Move = move.normalized,
            racketFace = racketFace,
        });
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input){}
    public void OnConnectedToServer(NetworkRunner runner){}
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList){}
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data){}
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken){}

    public void OnSceneLoadDone(NetworkRunner runner){}
    public void OnSceneLoadStart(NetworkRunner runner){}
}
