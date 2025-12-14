using System;
using System.Collections.Generic;
using System.Linq;

using Fusion;
using Fusion.Sockets;

using TMPro;

using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private NetworkPrefabRef playerRacketPrefab;
    [SerializeField] private NetworkPrefabRef cpuRacketPrefab; // AI用のラケットプレハブを別途用意すると管理が楽
    [SerializeField] private NetworkPrefabRef ballPrefab;
    static public GameObject ball;
    [Networked] public NetworkObject ballObj { get; set; }

    private NetworkRunner networkRunner;
    Vector3 BallPosition = new Vector3(-0.8f, 1.3f, -1.188f);
    Vector3 Player1Position = new Vector3(-0.9f, 1.3f, -1.123f);
    Quaternion Player1Rotation = Quaternion.Euler(-90f, -90f, 180f);
    Vector3 Player2Position = new Vector3(2.0f, 1.3f, -1.123f);
    Quaternion Player2Rotation = Quaternion.Euler(-90f, 90f, 180f);
    /**
    * ラケットの傾きに関するパラメータや変数
    */
    protected Vector3 baseRotationVector = new Vector3(-90f, -90f, 180f); // 通常時の基本角度
    protected float drivePitchAngle = -10f; // ドライブは基本から-10度
    protected float cutPitchAngle = 20f;   // カットは基本から+20度
    protected float rollAnglePerLevel = 20f; // 1段階あたり20度傾く
    protected int[] racketFaceIndex = new int[2]; // ラケットの向きのインデックス. 0: drive cut,  1: right left
    public static GameMode CurrentMode;


    private async void Start()
    {
        // NetworkRunnerを生成する
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.AddCallbacks(this);

        var scene = SceneManager.GetActiveScene();
        GameMode gameMode;
        if (scene.name == "cpu")
        {
            Debug.Log("CPU 対戦モードで起動します");
            gameMode = GameMode.Single;
        }
        else
        {
            Debug.Log("オンライン対戦モードで起動します。");
            gameMode = GameMode.AutoHostOrClient; // オンラインモード
        }
        
        CurrentMode = gameMode;

        // StartGameArgsに渡した設定で、セッションに参加する
        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            SceneManager = networkRunner.GetComponent<NetworkSceneManagerDefault>(),
            SessionName = (gameMode == GameMode.Single) ? "CpuMatch" : "OnlineMatch", // セッション名を分ける
        });

        if (result.Ok)
        {
            Debug.Log("成功！");
        }
        else
        {
            Debug.Log("失敗");
        }
        racketFaceIndex[0] = 0; // 通常のラケットの向き
        racketFaceIndex[1] = 0; // 通常のラケットの向き
    }

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
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) { return; }

        // --- CPU 対戦モードの処理 --- 
        if (runner.GameMode == GameMode.Single)
        {
            var playerRacket = runner.Spawn(playerRacketPrefab, Player1Position, Player1Rotation, player);
            runner.SetPlayerObject(player, playerRacket);

            // CPU用のラケットを生成
            var cpuRacket = runner.Spawn(cpuRacketPrefab, Player2Position, Player2Rotation);
            // AI操縦者に切り替える
            var aiStrategy = cpuRacket.gameObject.AddComponent<AIControlStrategy>();
            // もしPlayerControlStrategyが最初からアタッチされているなら削除
            var playerStrategy = cpuRacket.gameObject.GetComponent<PlayerControlStrategy>();
            if (playerStrategy != null) Destroy(playerStrategy);

            // ボールを生成
            runner.Spawn(ballPrefab, BallPosition, Quaternion.identity, player);
        }

        else
        {
            int playerCount = runner.ActivePlayers.Count();
            Vector3 spawnPosition;
            Quaternion spawnRotation;

            if (playerCount == 1)
            {
                spawnPosition = Player1Position;
                spawnRotation = Player1Rotation;
                // 最初のプレイヤーが入室した時にボールを生成
                ballObj = runner.Spawn(ballPrefab, BallPosition, Quaternion.identity);
                ball = ballObj?.gameObject; 
            }
            else
            {
                spawnPosition = Player2Position;
                spawnRotation = Player2Rotation;
                ball = ballObj?.gameObject;
            }

            var playerRacket = runner.Spawn(playerRacketPrefab, spawnPosition, spawnRotation, player);
            runner.SetPlayerObject(player, playerRacket);
            if (ball != null){
                Debug.Log($"Setting ball for player {player}");
                var controller = playerRacket.GetComponent<RacketController>();
                controller?.SetBall(ball);
            }
            if (ball == null){
                Debug.LogWarning("Ball not found! Make sure the ball prefab is correctly set up.");
            }
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) { return; }
        // 退出したプレイヤーのアバターを破棄する
        if (runner.TryGetPlayerObject(player, out var avatar))
        {
            runner.Despawn(avatar);
        }
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // ローカルプレイヤーの操作オブジェクト（ラケット）を探す
        if (runner.LocalPlayer != PlayerRef.None && runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObject))
        {
            // ラケットのRacketControllerから「操縦者」を取得
            var racketController = playerObject.GetComponent<RacketController>();
            var strategy = racketController != null ? racketController.GetControlStrategy() : null;
            if (strategy != null)
            {
            Debug.Log($"[OnInput] Called by PlayerRef: {runner.LocalPlayer}");
                // 操縦者から入力データを取得し、ネットワークにセットする
                var racketInput = strategy.GetInput();

                // セットしてからデバッグ表示
                input.Set(racketInput);
                Debug.Log($"[FixedUpdateNetwork] Player={runner.LocalPlayer}");
                Debug.Log($"[OnInput] Move={racketInput.MoveDirection}, Rot={racketInput.TargetRotation}");

            }
            else
            {
                Debug.LogWarning("ControlStrategy is null!");
            }
        }
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
