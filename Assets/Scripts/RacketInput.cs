using Fusion;
using UnityEngine;

// Photon Fusionがネットワークで同期するための入力データ構造
public struct RacketInput : INetworkInput
{
    public Vector3 MoveDirection;    // 移動方向
    public Quaternion TargetRotation; // 目標のラケット角度
    public NetworkButtons Buttons;    // ブーストボタンなどの入力状態
}

// 「指示」を生成するためのルールの定義 (インターフェース)
public interface IControlStrategy
{
    /// <summary>
    /// 毎フレームのラケットへの指示（入力）を生成して返す
    /// </summary>
    RacketInput GetInput();
}