using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshProを使う場合

public class TitleManager : MonoBehaviour
{
    [Header("設定用UI")]
    public TMP_Dropdown cpuLevelDropdown;   // CPUレベル選択
    public TMP_Dropdown setsDropdown;       // セット数選択
    public TMP_Dropdown pointsDropdown;     // ポイント数選択

    // スタートボタンに紐付ける関数
    public void OnStartGameButtonClicked()
    {
        // 1. CPUレベルの保存 (Dropdownのインデックス 0, 1, 2... をそのまま使う想定)
        GameData.CpuLevel = cpuLevelDropdown.value + 1;

        // 2. 勝利セット数の保存
        // 選択されたテキストを取得して判定、またはインデックスで分岐させる
        int selectedSetIndex = setsDropdown.value;
        switch (selectedSetIndex)
        {
            case 0: GameData.SetsToWin = 1; break; // 1セットマッチ
            case 1: GameData.SetsToWin = 2; break; // 2セットマッチ(2本先取)
            case 2: GameData.SetsToWin = 3; break; // 3セットマッチ(3本先取)
            default: GameData.SetsToWin = 1; break;
        }

        // 3. ポイント数の保存
        int selectedPointIndex = pointsDropdown.value;
        if (selectedPointIndex == 0) GameData.PointsPerSet = 3;
        else if (selectedPointIndex == 1) GameData.PointsPerSet = 7;
        else if (selectedPointIndex == 2) GameData.PointsPerSet = 11;
        else GameData.PointsPerSet = 11;

        // 設定値の確認ログ
        Debug.Log($"設定完了: CPU Lv.{GameData.CpuLevel}, {GameData.SetsToWin}セット先取, {GameData.PointsPerSet}点制");

        // 4. ゲームシーンへ移動 ("GameScene"は実際のシーン名に合わせてください)
        SceneManager.LoadScene("rally");
    }
}