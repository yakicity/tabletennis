using System.Collections;

using UnityEngine;
using DG.Tweening;


public class HitStopManager: MonoBehaviour
{
    // どこからでも呼び出せるようにする
    public static HitStopManager instance;

    private void Start()
    {
        instance = this;
    }

    // ヒットストップを開始する関数
    public void StartHitStop()
    {
        instance.StartCoroutine(instance.HitStopCoroutine());
    }
    // コルーチンの内容
    private IEnumerator HitStopCoroutine()
    {
        // Shake(1.0f, 6, 1.0f);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.02f);
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(0.02f);
        Time.timeScale = 0.7f;
        yield return new WaitForSecondsRealtime(0.04f);
        Time.timeScale = 1f;

        // PlayerRacketController.ShakeRacket(instance.transform, 40f, 1, 0.2f);
    }

    public void Shake(float width, int count, float duration)
    {
        var camera = Camera.main.transform;
        Vector3 iniPos = camera.localEulerAngles;
        var seq = DOTween.Sequence();
        // 振れ演出の片道の揺れ分の時間
        var partDuration = duration / count / 2f;
        // 振れ幅の半分の値
        var widthHalf = width / 2f;
        // 往復回数-1回分の振動演出を作る
        for (int i = 0; i < count - 1; i++)
        {
            seq.Append(camera.DOLocalRotate(new Vector3(iniPos.x -widthHalf, iniPos.y, iniPos.z), partDuration));
            seq.Append(camera.DOLocalRotate(new Vector3(iniPos.x + widthHalf, iniPos.y, iniPos.z), partDuration));
        }
        // 最後の揺れは元の角度に戻す工程とする
        seq.Append(camera.DOLocalRotate(new Vector3(iniPos.x - widthHalf, iniPos.y, iniPos.z), partDuration));
        seq.Append(camera.DOLocalRotate(iniPos, partDuration));
    }


}
