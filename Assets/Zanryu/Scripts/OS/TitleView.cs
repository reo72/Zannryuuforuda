using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// タイトル画面（ゲームを起動して最初に出る画面）
    ///   はじめる / 最初から / END一覧（図鑑とヒント）/ 設定 / おわる
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        GameManager gm;
        RectTransform root, menu, page;

        static readonly Color Dim = new Color(0.62f, 0.64f, 0.68f);
        static readonly Color PanelBg = new Color(0.06f, 0.07f, 0.09f, 0.94f);

        public void Build()
        {
            gm = GameManager.I;
            root = (RectTransform)transform;

            // 背景：壁紙の写真を暗くして使う
            var bg = gameObject.AddComponent<Image>();
            // タイトル専用の背景（title_bg）があればそれを、なければ壁紙を暗くして使う
            var sp = UIKit.LoadSprite("Images/title_bg");
            if (sp != null) { bg.sprite = sp; bg.color = Color.white; }
            else if ((sp = UIKit.LoadSprite("Images/" + Content.Config.defaultWallpaper)) != null) { bg.sprite = sp; bg.color = new Color(0.32f, 0.32f, 0.36f); }
            else bg.color = new Color(0.04f, 0.05f, 0.07f);
            var shade = UIKit.Panel(root, new Color(0, 0, 0, 0.35f), "Shade");
            UIKit.Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            // タイトルロゴ（画像があれば画像、なければ文字）
            var logo = UIKit.LoadSprite("Images/title_logo");
            if (logo != null)
            {
                var img = UIKit.Panel(root, Color.white, "Logo");
                img.sprite = logo;
                img.preserveAspect = true;
                img.raycastTarget = false;
                UIKit.Place(img.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 210), new Vector2(760, 190));
            }
            else
            {
                var title = UIKit.Label(root, Content.Config.gameTitle, 72, new Color(0.90f, 0.91f, 0.93f), TextAnchor.MiddleLeft);
                UIKit.Place(title.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 230), new Vector2(1000, 110));
                var sub = UIKit.Label(root, Content.Config.gameSubtitle, 22, Dim, TextAnchor.MiddleLeft);
                UIKit.Place(sub.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(146, 160), new Vector2(1000, 40));
            }

            menu = UIKit.Rect("Menu", root);
            UIKit.Place(menu, new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(140, 80), new Vector2(360, 400));
            var v = menu.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;

            bool hasSave = SaveSystem.LoadState("main") != null;
            MenuButton(hasSave ? "つづきから" : "はじめる", () => OSRoot.I.ShowBoot());
            if (hasSave) MenuButton("最初から", ConfirmNewGame);
            MenuButton($"END 一覧　{SeenCount()} / {Content.Config.endings.Count}", ShowGallery);
            MenuButton("設定", ShowSettings);
            MenuButton("おわる", Quit);

            if (!string.IsNullOrEmpty(Content.Config.earphoneNotice))
            {
                var ear = UIKit.Label(root, TextResolver.Resolve(Content.Config.earphoneNotice).Replace("\n", "　"), 16, new Color(1, 1, 1, 0.45f), TextAnchor.LowerLeft);
                UIKit.Place(ear.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 16), new Vector2(900, 24));
            }

            var ver = UIKit.Label(root, $"v{Application.version}", 14, new Color(1, 1, 1, 0.3f), TextAnchor.LowerRight);
            UIKit.Place(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 16), new Vector2(300, 24));
        }

        int SeenCount() => Content.Config.endings.Count(e => gm.HasEnding(e.id));

        void MenuButton(string label, System.Action onClick)
        {
            var b = UIKit.Button(menu, "  " + label, onClick, new Color(0.10f, 0.11f, 0.14f, 0.85f), new Color(0.88f, 0.89f, 0.92f), 24);
            b.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
            UIKit.Size(b, 58);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ================= 共通：右側のページ =================

        RectTransform OpenPage(string heading)
        {
            ClosePage();
            var p = UIKit.Panel(root, PanelBg, "Page");
            UIKit.Place(p.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-100, 0), new Vector2(980, 860));
            page = p.rectTransform;
            var h = UIKit.Label(page, heading, 30, Theme.TextLight, TextAnchor.MiddleLeft);
            UIKit.Top(h.rectTransform, 50, 36, 24);
            var close = UIKit.Button(page, "戻る", ClosePage, new Color(0.18f, 0.19f, 0.23f), Theme.TextLight, 16);
            UIKit.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -28), new Vector2(110, 40));
            return page;
        }

        void ClosePage()
        {
            if (page != null) Destroy(page.gameObject);
            page = null;
        }

        // ================= 最初から =================

        void ConfirmNewGame()
        {
            var p = OpenPage("最初から");
            var t = UIKit.Label(p, "今の進行状況（何日目か・フラグ）を消して、Day1 から始めます。\n見たENDと見つけた隠し要素は残ります。", 20, Theme.TextLight);
            UIKit.Top(t.rectTransform, 120, 36, 100);
            var yes = UIKit.Button(p, "最初から始める", () => { gm.NewGame(); OSRoot.I.ShowBoot(); }, Theme.Danger, Color.white, 18);
            UIKit.Place((RectTransform)yes.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -250), new Vector2(240, 48));
        }

        // ================= END 一覧（図鑑） =================

        void ShowGallery()
        {
            var p = OpenPage($"END 一覧　{SeenCount()} / {Content.Config.endings.Count}");
            int secrets = gm.Global.secrets.Count;
            var sub = UIKit.Label(p, $"見つけた隠し要素：{secrets}", 16, Dim, TextAnchor.MiddleLeft);
            UIKit.Top(sub.rectTransform, 24, 38, 76);

            var area = UIKit.Rect("List", p);
            UIKit.Stretch(area, 24, 112, 24, 24);
            var list = UIKit.ScrollList(area, out _, 12, 8);

            foreach (var e in Content.Config.endings)
            {
                bool seen = gm.HasEnding(e.id);
                var card = UIKit.Panel(list, seen ? new Color(0.14f, 0.15f, 0.19f) : new Color(0.10f, 0.10f, 0.12f), "Card:" + e.id);
                var cv = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cv.padding = new RectOffset(24, 24, 16, 18);
                cv.spacing = 8;
                cv.childControlHeight = true;
                cv.childControlWidth = true;
                cv.childForceExpandHeight = false;

                var head = UIKit.Label(card.transform, seen ? e.title : "？？？", 22, seen ? Theme.TextLight : new Color(0.45f, 0.46f, 0.50f));
                if (seen)
                {
                    var body = UIKit.Label(card.transform, TextResolver.Resolve(e.text), 16, new Color(0.74f, 0.75f, 0.78f));
                    body.lineSpacing = 4;
                    // メタ演出を OFF にしているときは「残されたファイル」をここで見せる
                    if (!RealDesktop.Enabled && !string.IsNullOrEmpty(e.realFile))
                    {
                        var rf = UIKit.Label(card.transform, $"――　残されたファイル「{e.realFile}」\n" + TextResolver.Resolve(e.realText),
                            16, new Color(0.85f, 0.55f, 0.55f));
                        rf.lineSpacing = 4;
                    }
                }
                else if (!string.IsNullOrEmpty(e.hint))
                {
                    // ヒントは、押したときだけ表示する（ネタバレを見たくない人のため）
                    var toggle = UIKit.Label(card.transform, "▶ ヒントを表示", 15, new Color(0.45f, 0.62f, 0.85f));
                    toggle.raycastTarget = true;
                    var hint = UIKit.Label(card.transform, "ヒント：" + e.hint, 16, new Color(0.62f, 0.56f, 0.44f));
                    hint.gameObject.SetActive(false);
                    var click = toggle.gameObject.AddComponent<ClickHandler>();
                    click.Single = () =>
                    {
                        bool show = !hint.gameObject.activeSelf;
                        hint.gameObject.SetActive(show);
                        toggle.text = show ? "▼ ヒントをかくす" : "▶ ヒントを表示";
                        OSRoot.I.PlaySound("click", 0.5f);
                    };
                    click.Double = click.Single;
                }
            }
        }

        // ================= 設定 =================

        void ShowSettings()
        {
            var p = OpenPage("設定");
            var g = gm.Global;
            float y = 110;

            Toggle(p, ref y, "メタ演出",
                "演出として、あなたのPCのデスクトップにテキストファイルを作ります。\nゲームが作ったファイル以外は、読み取り・変更・削除をしません。",
                () => g.metaFiles, val => g.metaFiles = val);

            Toggle(p, ref y, "PC のユーザー名を使う",
                "ゲームの中に、あなたのPCのユーザー名が出てくることがあります。OFF にすると別の名前になります。",
                () => g.useRealPcName, val => g.useRealPcName = val);

            Choice(p, ref y, "文字の速さ", "独り言のメッセージが表示される速さ。",
                new[] { "ゆっくり", "ふつう", "はやい" }, () => g.textSpeed, val => g.textSpeed = val);

            Slider(p, ref y, "全体の音量", () => g.volume, val => { g.volume = val; AudioListener.volume = val; });
            Slider(p, ref y, "BGM", () => g.bgmVolume, val => { g.bgmVolume = val; OSRoot.I.ApplyVolumes(); });
            Slider(p, ref y, "効果音", () => g.seVolume, val => { g.seVolume = val; OSRoot.I.PlaySound("click", 0.6f); });
            Slider(p, ref y, "環境音", () => g.ambientVolume, val => { g.ambientVolume = val; OSRoot.I.ApplyVolumes(); });

            if (!Application.isEditor)
                Choice(p, ref y, "画面", "", new[] { "ウィンドウ", "フルスクリーン" },
                    () => g.fullscreen ? 1 : 0, val => { g.fullscreen = val == 1; Settings.ApplyScreen(g); });
        }

        RectTransform Row(RectTransform p, ref float y, string name, string desc, float height)
        {
            var row = UIKit.Rect("Row:" + name, p);
            UIKit.Top(row, height, 36, y);
            var n = UIKit.Label(row, name, 20, Theme.TextLight, TextAnchor.UpperLeft);
            UIKit.Top(n.rectTransform, 30, 0, 0);
            if (!string.IsNullOrEmpty(desc))
            {
                var d = UIKit.Label(row, desc, 15, Dim, TextAnchor.UpperLeft);
                UIKit.Top(d.rectTransform, height - 34, 0, 34);
                d.rectTransform.offsetMax = new Vector2(-300, d.rectTransform.offsetMax.y);
            }
            y += height + 18;
            return row;
        }

        void Toggle(RectTransform p, ref float y, string name, string desc, System.Func<bool> get, System.Action<bool> set)
        {
            var row = Row(p, ref y, name, desc, 90);
            UnityEngine.UI.Button b = null;
            b = UIKit.Button(row, "", () =>
            {
                set(!get());
                SaveSystem.SaveGlobal(gm.Global);
                b.GetComponentInChildren<TMP_Text>().text = get() ? "ON" : "OFF";
                b.GetComponent<Image>().color = get() ? Theme.Accent : new Color(0.25f, 0.26f, 0.30f);
            }, get() ? Theme.Accent : new Color(0.25f, 0.26f, 0.30f), Color.white, 18);
            b.GetComponentInChildren<TMP_Text>().text = get() ? "ON" : "OFF";
            UIKit.Place((RectTransform)b.transform, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(140, 42));
        }

        void Choice(RectTransform p, ref float y, string name, string desc, string[] options, System.Func<int> get, System.Action<int> set)
        {
            var row = Row(p, ref y, name, desc, string.IsNullOrEmpty(desc) ? 50 : 70);
            var buttons = new UnityEngine.UI.Button[options.Length];
            void Paint()
            {
                for (int i = 0; i < buttons.Length; i++)
                    buttons[i].GetComponent<Image>().color = i == get() ? Theme.Accent : new Color(0.25f, 0.26f, 0.30f);
            }
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                buttons[i] = UIKit.Button(row, options[i], () => { set(idx); SaveSystem.SaveGlobal(gm.Global); Paint(); },
                    new Color(0.25f, 0.26f, 0.30f), Color.white, 15);
                float w = 290f / options.Length;
                UIKit.Place((RectTransform)buttons[i].transform, new Vector2(1, 1), new Vector2(1, 1),
                    new Vector2(-(options.Length - 1 - i) * (w + 4), 0), new Vector2(w, 42));
            }
            Paint();
        }

        void Slider(RectTransform p, ref float y, string name, System.Func<float> get, System.Action<float> set)
        {
            var row = Row(p, ref y, name, "", 50);
            var track = UIKit.Panel(row, new Color(0.25f, 0.26f, 0.30f), "Track");
            UIKit.Place(track.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -14), new Vector2(230, 14));
            var fillArea = UIKit.Stretch(UIKit.Rect("FillArea", track.transform));
            var fill = UIKit.Panel(fillArea, Theme.Accent, "Fill");
            UIKit.Stretch(fill.rectTransform);
            var handleArea = UIKit.Stretch(UIKit.Rect("HandleArea", track.transform));
            var handle = UIKit.Panel(handleArea, Color.white, "Handle");
            handle.rectTransform.sizeDelta = new Vector2(20, 28);
            var s = track.gameObject.AddComponent<UnityEngine.UI.Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0;
            s.maxValue = 1;
            s.value = get();
            var pct = UIKit.Label(row, Mathf.RoundToInt(get() * 100) + "%", 16, Dim, TextAnchor.MiddleRight);
            UIKit.Place(pct.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -6), new Vector2(54, 30));
            s.onValueChanged.AddListener(val =>
            {
                set(val);
                pct.text = Mathf.RoundToInt(val * 100) + "%";
                SaveSystem.SaveGlobal(gm.Global);
            });
        }
    }

    /// <summary>設定の反映（起動時と変更時）</summary>
    public static class Settings
    {
        public static void Apply(GlobalSave g)
        {
            AudioListener.volume = g.volume;
            ApplyScreen(g);
            if (OSRoot.I != null) OSRoot.I.ApplyVolumes();
        }

        public static void ApplyScreen(GlobalSave g)
        {
            if (Application.isEditor) return;
            if (g.fullscreen)
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            else
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
        }

        /// <summary>独り言の1秒あたりの文字数</summary>
        public static float CharsPerSecond
        {
            get
            {
                var g = GameManager.I != null ? GameManager.I.Global : null;
                int s = g != null ? g.textSpeed : 1;
                return s == 0 ? 20f : s == 2 ? 80f : 38f;
            }
        }
    }
}
