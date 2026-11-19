using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// 画面全体の土台。Canvas とレイヤーを作り、起動→ログイン→デスクトップ→終了→エンドの画面を切り替える。
    ///
    /// レイヤー（下から順に）:
    ///   ScreenLayer   … 起動画面・ログイン画面・デスクトップ（壁紙とアイコン）
    ///   WindowLayer   … ウィンドウ
    ///   TaskbarLayer  … タスクバーとスタートメニュー
    ///   MessageLayer  … 独り言のメッセージウィンドウ（RPG風）
    ///   OverlayLayer  … 通知・グリッチ演出
    ///   CursorLayer   … 偽カーソル（乗っ取り演出のため本物のカーソルは隠す）
    /// </summary>
    public class OSRoot : MonoBehaviour
    {
        public static OSRoot I { get; private set; }

        public RectTransform Root, ScreenLayer, WindowLayer, TaskbarLayer, MessageLayer, OverlayLayer, CursorLayer;
        public WindowManager Windows { get; private set; }
        public DesktopView Desktop { get; private set; }
        public Taskbar Taskbar { get; private set; }
        public FakeCursor Cursor { get; private set; }
        public GlitchOverlay Glitch { get; private set; }
        public MonologueBox Monologue { get; private set; }

        AudioSource sfx, ambient;
        readonly AudioSource[] bgm = new AudioSource[2];
        int bgmIndex;
        string bgmName;
        Coroutine bgmFade;
        float ambientBase = 0.6f;
        readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        readonly HashSet<string> missingLogged = new HashSet<string>();
        readonly List<GameObject> toasts = new List<GameObject>();
        Coroutine screenRoutine;

        public void Build()
        {
            I = this;
            var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = 5 };
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Root = (RectTransform)canvasGo.transform;

            ScreenLayer = UIKit.Stretch(UIKit.Rect("ScreenLayer", Root));
            WindowLayer = UIKit.Stretch(UIKit.Rect("WindowLayer", Root));
            TaskbarLayer = UIKit.Stretch(UIKit.Rect("TaskbarLayer", Root));
            MessageLayer = UIKit.Stretch(UIKit.Rect("MessageLayer", Root));
            OverlayLayer = UIKit.Stretch(UIKit.Rect("OverlayLayer", Root));
            CursorLayer = UIKit.Stretch(UIKit.Rect("CursorLayer", Root));

            Windows = WindowLayer.gameObject.AddComponent<WindowManager>();
            Windows.Init(WindowLayer);
            Monologue = MessageLayer.gameObject.AddComponent<MonologueBox>();
            Monologue.Init(MessageLayer);
            Glitch = OverlayLayer.gameObject.AddComponent<GlitchOverlay>();
            Glitch.Init(OverlayLayer);
            Cursor = CursorLayer.gameObject.AddComponent<FakeCursor>();
            Cursor.Init(CursorLayer, Content.Config.useFakeCursor);

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            ambient = gameObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            ambient.loop = true;
            for (int i = 0; i < 2; i++)
            {
                bgm[i] = gameObject.AddComponent<AudioSource>();
                bgm[i].playOnAwake = false;
                bgm[i].loop = true;
                bgm[i].volume = 0;
            }
        }

        // ================= 画面切り替え =================

        void ClearAll()
        {
            Windows.CloseAll();
            Monologue.Clear();
            UIKit.Clear(ScreenLayer);
            UIKit.Clear(TaskbarLayer);
            foreach (var t in toasts) if (t != null) Destroy(t);
            toasts.Clear();
            Desktop = null;
            Taskbar = null;
        }

        void RunScreen(IEnumerator routine)
        {
            if (screenRoutine != null) StopCoroutine(screenRoutine);
            screenRoutine = StartCoroutine(routine);
        }

        public void ShowBoot() => RunScreen(BootRoutine());

        IEnumerator BootRoutine()
        {
            ClearAll();
            var gmb = GameManager.I;
            if (gmb.State.profile == "main" && gmb.State.day == 1 && !gmb.HasFlag("seen_prologue"))
            {
                yield return PrologueRoutine();
                gmb.State.flags.Add("seen_prologue");
                gmb.Save();
                ClearAll();
            }
            PlayBgm(null, 1f);
            PlaySound("boot", 0.8f);
            var bg = UIKit.Panel(ScreenLayer, Color.black, "Boot");
            UIKit.Stretch(bg.rectTransform);
            var txt = UIKit.Label(bg.transform, "", 22, new Color(0.72f, 0.72f, 0.72f));
            UIKit.Stretch(txt.rectTransform, 80, 80, 80, 80);
            yield return new WaitForSeconds(0.8f);
            foreach (var line in Content.Config.bootLines)
            {
                txt.text += TextResolver.Resolve(line) + "\n";
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(0.8f);
            ShowLogin();
        }

        // ---------- 導入（はじめて起動する前に1回だけ） ----------
        IEnumerator PrologueRoutine()
        {
            var cfg = Content.Config;
            if (cfg.prologue.Count == 0 && string.IsNullOrEmpty(cfg.howToPlay)) yield break;
            PlayBgm(null, 1.5f);

            var bg = UIKit.Panel(ScreenLayer, Color.black, "Prologue");
            UIKit.Stretch(bg.rectTransform);
            bool clicked = false, skip = false;
            var catcher = UIKit.Button(bg.transform, "", () => clicked = true, new Color(0, 0, 0, 0), Color.clear, 10);
            UIKit.Stretch((RectTransform)catcher.transform);

            var line = UIKit.Label(bg.transform, "", 30, new Color(0.86f, 0.86f, 0.88f), TextAnchor.MiddleCenter);
            UIKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1500, 300));
            line.lineSpacing = 14;
            line.raycastTarget = false;
            var next = UIKit.Label(bg.transform, "クリック / Space で進む", 16, new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter);
            UIKit.Place(next.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(600, 30));
            next.raycastTarget = false;
            var skipBtn = UIKit.Button(bg.transform, "スキップ ▶▶", () => skip = true, new Color(1, 1, 1, 0.08f), new Color(1, 1, 1, 0.6f), 16);
            UIKit.Place((RectTransform)skipBtn.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(160, 40));

            // 最初に「イヤホン推奨」
            if (!string.IsNullOrEmpty(cfg.earphoneNotice))
            {
                line.text = TextResolver.Resolve(cfg.earphoneNotice);
                for (float t = 0; t < 1f && !skip; t += Time.deltaTime / 0.8f) { line.alpha = t; yield return null; }
                line.alpha = 1;
                for (float w = 0; w < 2.8f && !skip && !clicked && !InputCompat.AdvancePressed(); w += Time.deltaTime) yield return null;
                for (float t = 1; t > 0 && !skip; t -= Time.deltaTime / 0.6f) { line.alpha = t; yield return null; }
                line.alpha = 0;
                clicked = false;
                yield return new WaitForSeconds(0.4f);
            }

            foreach (var raw in cfg.prologue)
            {
                if (skip) break;
                line.text = TextResolver.Resolve(raw);
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
                {
                    line.alpha = t;
                    if (skip) break;
                    yield return null;
                }
                line.alpha = 1;
                clicked = false;
                while (!clicked && !skip && !InputCompat.AdvancePressed()) yield return null;
                for (float t = 1; t > 0 && !skip; t -= Time.deltaTime / 0.5f) { line.alpha = t; yield return null; }
                line.alpha = 0;
                yield return new WaitForSeconds(0.25f);
            }
            Destroy(catcher.gameObject);
            Destroy(skipBtn.gameObject);
            line.text = "";
            next.text = "";

            if (!string.IsNullOrEmpty(cfg.howToPlay))
            {
                var card = UIKit.Panel(bg.transform, new Color(0.09f, 0.10f, 0.12f), "HowToPlay");
                UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 760));
                var head = UIKit.Label(card.transform, TextResolver.Resolve(cfg.howToPlayTitle), 32, Theme.TextLight, TextAnchor.MiddleLeft);
                UIKit.Place(head.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, -40), new Vector2(1060, 56));
                var body = UIKit.Label(card.transform, TextResolver.Resolve(cfg.howToPlay), 21, new Color(0.82f, 0.83f, 0.86f), TextAnchor.UpperLeft);
                UIKit.Place(body.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, -116), new Vector2(1070, 520));
                body.lineSpacing = 10;
                bool go = false;
                var start = UIKit.Button(card.transform, "PC を起動する", () => go = true, Theme.Accent, Color.white, 20);
                UIKit.Place((RectTransform)start.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(320, 56));
                yield return new WaitUntil(() => go);
            }
            Destroy(bg.gameObject);
            yield return new WaitForSeconds(0.6f);
        }

        public void ShowLogin()
        {
            if (screenRoutine != null) StopCoroutine(screenRoutine);
            ClearAll();
            var v = UIKit.Stretch(UIKit.Rect("Login", ScreenLayer));
            v.gameObject.AddComponent<LoginView>().Build();
        }

        /// <summary>タイトル画面。最初の起動時だけ、先にメタ演出の注意書きを出す。</summary>
        public void ShowTitle() => RunScreen(TitleRoutine());

        IEnumerator TitleRoutine()
        {
            ClearAll();
            PlayBgm(Content.Config.titleBgm, 2f);
            var gm = GameManager.I;
            if (!gm.Global.metaNoticeShown)
            {
                var bg = UIKit.Panel(ScreenLayer, Color.black, "Notice");
                UIKit.Stretch(bg.rectTransform);
                var t = UIKit.Label(bg.transform, TextResolver.Resolve(Content.Config.metaNotice), 24, Theme.TextLight, TextAnchor.MiddleCenter);
                UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1300, 400));
                t.lineSpacing = 12;
                int choice = -1;
                var on = UIKit.Button(bg.transform, "ON のまま始める（おすすめ）", () => choice = 1, Theme.Accent, Color.white, 20);
                UIKit.Place((RectTransform)on.transform, new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, -200), new Vector2(380, 56));
                var off = UIKit.Button(bg.transform, "OFF にして始める", () => choice = 0, new Color(0.22f, 0.23f, 0.27f), Color.white, 20);
                UIKit.Place((RectTransform)off.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -200), new Vector2(380, 56));
                yield return new WaitUntil(() => choice >= 0);
                gm.Global.metaFiles = choice == 1;
                gm.Global.metaNoticeShown = true;
                SaveSystem.SaveGlobal(gm.Global);
                ClearAll();
            }
            var v = UIKit.Stretch(UIKit.Rect("Title", ScreenLayer));
            v.gameObject.AddComponent<TitleView>().Build();
        }

        public void ShowDesktop()
        {
            if (screenRoutine != null) StopCoroutine(screenRoutine);
            ClearAll();
            var d = UIKit.Stretch(UIKit.Rect("Desktop", ScreenLayer));
            Desktop = d.gameObject.AddComponent<DesktopView>();
            Desktop.Build();
            var tb = UIKit.Rect("Taskbar", TaskbarLayer);
            Taskbar = tb.gameObject.AddComponent<Taskbar>();
            Taskbar.Build();
        }

        /// <summary>シャットダウン演出。最後に日ごとのメッセージ（「またね」→「いかないで」）が出る＝伏線 #15</summary>
        public IEnumerator ShowShutdown(string message)
        {
            if (screenRoutine != null) StopCoroutine(screenRoutine);
            ClearAll();
            var bg = UIKit.Panel(ScreenLayer, Color.black, "Shutdown");
            UIKit.Stretch(bg.rectTransform);
            PlayBgm(null, 1f);
            PlaySound("shutdown", 0.8f);
            var t = UIKit.Label(bg.transform, "シャットダウンしています…", 24, Theme.TextLight, TextAnchor.MiddleCenter);
            UIKit.Stretch(t.rectTransform);
            yield return new WaitForSeconds(1.8f);
            t.text = "";
            yield return new WaitForSeconds(0.8f);
            if (!string.IsNullOrEmpty(message))
            {
                t.fontSize = 20;
                t.text = message;
                yield return Fade(t, 0, 0.8f, 1.2f);
                yield return new WaitForSeconds(1.2f);
                yield return Fade(t, 0.8f, 0, 1.0f);
            }
            yield return new WaitForSeconds(1.0f);
        }

        public IEnumerator ShowEnding(EndingDef def)
        {
            if (screenRoutine != null) StopCoroutine(screenRoutine);
            ClearAll();
            PlayBgm(string.IsNullOrEmpty(def.bgm) ? Content.Config.endingBgm : def.bgm, 3f);
            var bg = UIKit.Panel(ScreenLayer, Color.black, "Ending");
            UIKit.Stretch(bg.rectTransform);
            yield return new WaitForSeconds(1.5f);

            var title = UIKit.Label(bg.transform, def.title, 36, Theme.TextLight, TextAnchor.MiddleCenter);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1200, 60));
            yield return Fade(title, 0, 1, 2f);

            var body = UIKit.Label(bg.transform, "", 22, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperCenter);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 180), new Vector2(1200, 500));
            foreach (var line in TextResolver.Resolve(def.text).Split('\n'))
            {
                body.text += line + "\n";
                yield return new WaitForSeconds(string.IsNullOrEmpty(line) ? 0.4f : 1.1f);
            }

            var hint = UIKit.Label(bg.transform, "クリックで続ける", 16, Theme.TextDim, TextAnchor.MiddleCenter);
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(400, 30));
            bool clicked = false;
            var click = bg.gameObject.AddComponent<ClickHandler>();
            click.Single = () => clicked = true;
            click.Double = () => clicked = true;
            yield return new WaitUntil(() => clicked);
        }

        static IEnumerator Fade(TMP_Text t, float from, float to, float time)
        {
            var c = t.color;
            for (float x = 0; x < time; x += Time.deltaTime)
            {
                c.a = Mathf.Lerp(from, to, x / time);
                t.color = c;
                yield return null;
            }
            c.a = to;
            t.color = c;
        }

        // ================= 通知・ダイアログ =================

        /// <summary>
        /// 右下の通知（メッセージ着信など）。画面の右外からスライドして入ってきて、数秒後に右へ抜けていく。
        /// 1行目がタイトル（送り主）、2行目以降が本文。
        /// </summary>
        public void Toast(string text)
        {
            toasts.RemoveAll(x => x == null);
            var p = UIKit.Panel(OverlayLayer, new Color(0.12f, 0.13f, 0.16f, 0.96f), "Toast");
            p.raycastTarget = false;
            var outline = p.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, 0.12f);
            var rt = p.rectTransform;
            const float W = 400, H = 84;
            UIKit.Place(rt, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(W, H));

            // 左端のアクセント線
            var bar = UIKit.Panel(rt, Theme.Accent, "Accent");
            bar.raycastTarget = false;
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(0, 1);
            bar.rectTransform.pivot = new Vector2(0, 0.5f);
            bar.rectTransform.offsetMin = Vector2.zero;
            bar.rectTransform.offsetMax = new Vector2(4, 0);

            // アイコン（メッセージアプリと同じもの）
            var iconSprite = UIKit.LoadSprite("Icons/chat");
            float textLeft = 18;
            if (iconSprite != null)
            {
                var icon = UIKit.Panel(rt, Color.white, "Icon");
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UIKit.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 44));
                textLeft = 72;
            }

            var lines = (text ?? "").Split(new[] { '\n' }, 2);
            var head = UIKit.Label(rt, lines[0], 16, Theme.TextLight, TextAnchor.UpperLeft);
            UIKit.Stretch(head.rectTransform, textLeft, 12, 14, H - 36);
            head.textWrappingMode = TextWrappingModes.NoWrap;
            head.overflowMode = TextOverflowModes.Ellipsis;
            if (lines.Length > 1)
            {
                var body = UIKit.Label(rt, lines[1], 15, new Color(0.72f, 0.74f, 0.78f), TextAnchor.UpperLeft);
                UIKit.Stretch(body.rectTransform, textLeft, 38, 14, 8);
                body.overflowMode = TextOverflowModes.Ellipsis;
            }

            var slide = p.gameObject.AddComponent<ToastSlide>();
            slide.Play(new Vector2(-16, 60 + toasts.Count * (H + 10)), W + 40, 4f);
            toasts.Add(p.gameObject);
            PlaySound("notify", 0.7f);
        }

        public void MessageBox(string title, string text)
        {
            PlaySound("error", 0.7f);
            Windows.Open(null, title, new Vector2(460, 210), w =>
            {
                var t = UIKit.Label(w.Content, text, 17, Theme.Text, TextAnchor.UpperLeft);
                UIKit.Stretch(t.rectTransform, 20, 20, 20, 60);
                var ok = UIKit.Button(w.Content, "OK", w.Close);
                UIKit.Place((RectTransform)ok.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 12), new Vector2(110, 34));
            });
        }

        // ================= 音（素材は Resources/Audio に置く。ファイル名＝呼び出す名前） =================

        static GlobalSave G => GameManager.I != null ? GameManager.I.Global : null;
        static float SeVol => G != null ? G.seVolume : 1f;
        static float BgmVol => G != null ? G.bgmVolume : 1f;
        static float AmbVol => G != null ? G.ambientVolume : 1f;

        AudioClip LoadClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (clipCache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            clipCache[name] = c;
            if (c == null && missingLogged.Add(name)) Debug.Log($"[音声未設定] {name}");
            return c;
        }

        public void PlaySound(string name, float volume = 1f)
        {
            var clip = LoadClip(name);
            if (clip != null) sfx.PlayOneShot(clip, volume * SeVol);
        }

        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip != null) sfx.PlayOneShot(clip, volume * SeVol);
        }

        /// <summary>環境音（ファンの音など）。name を空にすると止める。</summary>
        public void PlayAmbient(string name, float volume)
        {
            if (string.IsNullOrEmpty(name)) { ambient.Stop(); return; }
            var clip = LoadClip(name);
            if (clip == null) { ambient.Stop(); return; }
            if (ambient.clip != clip) { ambient.clip = clip; ambient.Play(); }
            else if (!ambient.isPlaying) ambient.Play();
            ambientBase = volume;
            ambient.volume = ambientBase * AmbVol;
        }

        /// <summary>環境音の大きさだけ変える（だんだん音が消えていく演出用）</summary>
        public void SetAmbientVolume(float volume)
        {
            ambientBase = volume;
            ambient.volume = ambientBase * AmbVol;
        }

        /// <summary>BGM をクロスフェードで切り替える。name を空にするとフェードアウトして止める。</summary>
        public void PlayBgm(string name, float fade = 1.5f)
        {
            if (string.IsNullOrEmpty(name)) name = null;
            if (name == bgmName) return;
            bgmName = name;
            if (bgmFade != null) StopCoroutine(bgmFade);
            bgmFade = StartCoroutine(BgmCrossfade(LoadClip(name), Mathf.Max(0.05f, fade)));
        }

        IEnumerator BgmCrossfade(AudioClip clip, float fade)
        {
            var from = bgm[bgmIndex];
            AudioSource to = null;
            if (clip != null)
            {
                bgmIndex = 1 - bgmIndex;
                to = bgm[bgmIndex];
                to.clip = clip;
                to.volume = 0;
                to.Play();
            }
            float startFrom = from.volume;
            for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
            {
                float k = t / fade;
                if (from != to) from.volume = Mathf.Lerp(startFrom, 0, k);
                if (to != null) to.volume = Mathf.Lerp(0, BgmVol, k);
                yield return null;
            }
            if (from != to) { from.volume = 0; from.Stop(); }
            if (to != null) to.volume = BgmVol;
            bgmFade = null;
        }

        /// <summary>設定画面で音量を変えたときに呼ぶ</summary>
        public void ApplyVolumes()
        {
            ambient.volume = ambientBase * AmbVol;
            if (bgmFade == null && bgm[bgmIndex].isPlaying) bgm[bgmIndex].volume = BgmVol;
        }
    }

    /// <summary>通知のスライドイン／アウト</summary>
    public class ToastSlide : MonoBehaviour
    {
        public void Play(Vector2 target, float offscreen, float stay) => StartCoroutine(Run(target, offscreen, stay));

        IEnumerator Run(Vector2 target, float offscreen, float stay)
        {
            var rt = (RectTransform)transform;
            var group = gameObject.AddComponent<CanvasGroup>();
            var from = target + new Vector2(offscreen, 0);

            // 入ってくる（最後にゆっくり止まる）
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = 1 - Mathf.Pow(1 - t / 0.35f, 3);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, target, k);
                group.alpha = Mathf.Clamp01(t / 0.2f);
                yield return null;
            }
            rt.anchoredPosition = target;
            group.alpha = 1;

            yield return new WaitForSecondsRealtime(stay);

            // 抜けていく（だんだん速く）
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Pow(t / 0.3f, 2);
                rt.anchoredPosition = Vector2.LerpUnclamped(target, from, k);
                group.alpha = 1 - k;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
