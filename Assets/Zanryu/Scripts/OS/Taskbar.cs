using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>画面下のタスクバー（スタートメニュー・開いているウィンドウ・時計）</summary>
    public class Taskbar : MonoBehaviour
    {
        TMP_Text clock;
        RectTransform buttons;
        GameObject startMenu;
        WindowManager wm;

        public void Build()
        {
            wm = OSRoot.I.Windows;
            var rt = (RectTransform)transform;
            UIKit.Bottom(rt, 48);
            gameObject.AddComponent<Image>().color = Theme.Taskbar;

            var start = UIKit.Button(rt, Content.Config.osName, ToggleStart, Theme.Accent, Color.white, 15);
            UIKit.Place((RectTransform)start.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(130, 38));

            buttons = UIKit.Stretch(UIKit.Rect("WindowButtons", rt), 146, 5, 280, 5);
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            clock = UIKit.Label(rt, "", 15, Theme.TextLight, TextAnchor.MiddleCenter);
            UIKit.Place(clock.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(150, 48));

            // 迷ったとき用のヒントボタン（独り言で次にやることを教えてくれる）
            var hint = UIKit.Button(rt, "？ ヒント", () => GameManager.I.Mono.ShowHintNow(), new Color(0.22f, 0.24f, 0.28f), Theme.TextLight, 14);
            UIKit.Place((RectTransform)hint.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-166, 0), new Vector2(104, 38));

            wm.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (wm != null) wm.Changed -= Refresh;
            if (startMenu != null) Destroy(startMenu);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && clock != null) clock.text = gm.ClockText() + "\n" + gm.DateText();
        }

        void Refresh()
        {
            if (buttons == null) return;
            UIKit.Clear(buttons);
            foreach (var w in wm.All)
            {
                if (w == null) continue;
                var win = w;
                bool active = win.gameObject.activeSelf && wm.Focused == win;
                var b = UIKit.Button(buttons, Shorten(win.Title, 16), () => OnWindowButton(win),
                    active ? new Color(0.30f, 0.33f, 0.40f) : new Color(0.18f, 0.19f, 0.22f), Theme.TextLight, 14);
                UIKit.Size(b, -1, 190);
            }
        }

        void OnWindowButton(OSWindow w)
        {
            if (!w.gameObject.activeSelf) w.Restore();
            else if (wm.Focused == w) w.Minimize();
            else wm.Focus(w);
        }

        static string Shorten(string s, int n) => string.IsNullOrEmpty(s) || s.Length <= n ? s : s.Substring(0, n) + "…";

        void ToggleStart()
        {
            if (startMenu != null) { Destroy(startMenu); return; }
            var gm = GameManager.I;
            var menu = UIKit.Panel(transform.parent, new Color(0.13f, 0.14f, 0.16f, 0.98f), "StartMenu");
            UIKit.Place(menu.rectTransform, Vector2.zero, Vector2.zero, new Vector2(6, 52), new Vector2(260, 190));
            startMenu = menu.gameObject;

            var user = UIKit.Label(menu.transform, gm.AccountName, 20, Theme.TextLight, TextAnchor.MiddleLeft);
            UIKit.Top(user.rectTransform, 50, 16, 6);

            var explorer = UIKit.Button(menu.transform, "エクスプローラー", () =>
            {
                Destroy(startMenu);
                ExplorerApp.Open("root");
            }, new Color(0.20f, 0.22f, 0.26f), Theme.TextLight);
            UIKit.Top((RectTransform)explorer.transform, 40, 10, 66);

            var shutdown = UIKit.Button(menu.transform, "シャットダウン", () =>
            {
                Destroy(startMenu);
                gm.RequestShutdown();
            }, new Color(0.35f, 0.20f, 0.22f), Theme.TextLight);
            UIKit.Top((RectTransform)shutdown.transform, 40, 10, 116);
        }
    }
}
