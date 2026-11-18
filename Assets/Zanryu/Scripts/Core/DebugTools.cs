using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// テスト用のショートカット（エディタと開発ビルドだけで有効。製品版では自動で無効）
    ///   F1 … 状態表示（日数・時刻・フラグ・見たエンド）の切り替え
    ///   F2 … 今すぐシャットダウン（次の日へ）
    ///   F3 … グリッチ演出を試す
    ///   F4 … カーソル乗っ取りを試す
    ///   F5 … セーブを全部消して最初から
    /// </summary>
    public class DebugTools : MonoBehaviour
    {
        TMP_Text info;
        bool show;

        void Update()
        {
            var gm = GameManager.I;
            var os = OSRoot.I;
            if (gm == null || os == null) return;

            if (InputCompat.FunctionKeyDown(1)) show = !show;
            if (InputCompat.FunctionKeyDown(2)) gm.RequestShutdown();
            if (InputCompat.FunctionKeyDown(3)) os.Glitch.Play(0.5f);
            if (InputCompat.FunctionKeyDown(4) && gm.InDesktop) StartCoroutine(os.Cursor.Hijack(null, 1.2f));
            if (InputCompat.FunctionKeyDown(5)) gm.ResetEverything();

            if (info == null)
            {
                info = UIKit.Label(os.OverlayLayer, "", 14, new Color(0.4f, 1f, 0.5f), TextAnchor.UpperLeft, "DebugInfo");
                UIKit.Place(info.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(700, 400));
                info.outlineWidth = 0.2f;
                info.outlineColor = new Color32(0, 0, 0, 255);
            }
            info.gameObject.SetActive(show);
            if (!show) return;
            var s = gm.State;
            info.text =
                $"[DEBUG]  F1 表示  F2 次の日へ  F3 グリッチ  F4 カーソル  F5 全リセット\n" +
                $"profile: {s.profile} / Day {s.day} / {gm.ClockText()} ({gm.DayTime:0}s)\n" +
                $"endings({gm.EndingCount}): {string.Join(", ", gm.Global.endings)}\n" +
                $"pretend: {gm.CountFlags("pretend_")} / seller: {gm.CountFlags("seller_")}\n" +
                $"flags: {string.Join(", ", s.flags.Skip(Mathf.Max(0, s.flags.Count - 20)))}";
        }
    }
}
