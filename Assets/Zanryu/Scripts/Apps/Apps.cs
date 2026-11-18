using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>ファイルの種類ごとに、どのアプリで開くかを振り分ける</summary>
    public static class Apps
    {
        static GameManager GM => GameManager.I;
        static WindowManager WM => OSRoot.I.Windows;

        public static void Open(FileEntry f)
        {
            switch (f.type)
            {
                case "folder": ExplorerApp.Open(f.id); break;
                case "text": OpenText(f); break;
                case "image": OpenImage(f); break;
                case "audio": OpenAudio(f); break;
                case "confirm": OpenConfirm(f); break;
                case "app": OpenApp(f); break;
                default: Debug.LogWarning($"[Apps] 未対応の種類: {f.type}（{f.id}）"); break;
            }
        }

        static void OpenApp(FileEntry f)
        {
            switch (f.app)
            {
                case "explorer": ExplorerApp.Open(string.IsNullOrEmpty(f.content) ? "root" : f.content); break;
                case "chat": ChatApp.Open(); break;
                case "taskmgr": TaskManagerApp.Open(); break;
                default: Debug.LogWarning($"[Apps] 未対応のアプリ: {f.app}"); break;
            }
        }

        // ---------- メモ帳 ----------
        static void OpenText(FileEntry f)
        {
            var photo = UIKit.LoadSprite("Images/" + GM.GetAsset(f));
            var size = photo != null ? new Vector2(780, 720) : new Vector2(640, 480);
            var w = WM.Open("file:" + f.id, GM.DisplayName(f) + " - メモ帳", size, win =>
            {
                var list = UIKit.ScrollList(win.Content, out _, 12, 16);
                if (photo != null)
                {
                    // 記事の写真：ウィンドウの幅に合わせて縦横比を保つ
                    var img = UIKit.Panel(list, Color.white, "Photo");
                    img.sprite = photo;
                    img.preserveAspect = true;
                    float width = size.x - 2 - 32;
                    UIKit.Size(img, width * photo.rect.height / photo.rect.width);
                }
                var t = UIKit.Label(list, "", 19, Theme.Text);
                t.gameObject.name = "Body";
                // 開くたびに最新の中身を表示する（日をまたいで中身が変わるため）
                win.gameObject.AddComponent<TextFileBinder>().Bind(f, t, win);
            });
            if (w.TryGetComponent<TextFileBinder>(out var binder)) binder.Reload();
        }

        // ---------- 画像ビューア ----------
        static void OpenImage(FileEntry f)
        {
            WM.Open("file:" + f.id, GM.DisplayName(f) + " - フォト", new Vector2(760, 580), win =>
            {
                var bg = UIKit.Panel(win.Content, new Color(0.12f, 0.12f, 0.13f), "Viewer");
                UIKit.Stretch(bg.rectTransform);
                var asset = GM.GetAsset(f);
                var sp = UIKit.LoadSprite("Images/" + asset);
                if (sp != null)
                {
                    var img = UIKit.Panel(bg.transform, Color.white, "Photo");
                    img.sprite = sp;
                    img.preserveAspect = true;
                    UIKit.Stretch(img.rectTransform, 12, 12, 12, 12);
                }
                else
                {
                    // 素材がまだないときは「撮影メモ」を表示（あとで写真を撮るときの指示書になる）
                    var head = string.IsNullOrEmpty(asset) ? "[このファイルは表示できません]" : $"[画像未設定: {asset}]";
                    var t = UIKit.Label(bg.transform, head + "\n\n" + GM.GetContent(f), 18, new Color(0.75f, 0.75f, 0.75f), TextAnchor.MiddleCenter);
                    UIKit.Stretch(t.rectTransform, 40, 40, 40, 40);
                }
            });
        }

        // ---------- 音声プレーヤー（逆再生つき） ----------
        static void OpenAudio(FileEntry f)
        {
            WM.Open("file:" + f.id, GM.DisplayName(f) + " - オーディオ", new Vector2(560, 280), win =>
            {
                var asset = GM.GetAsset(f);
                var clip = string.IsNullOrEmpty(asset) ? null : Resources.Load<AudioClip>("Audio/" + asset);
                var info = clip != null ? GM.DisplayName(f) : $"[音声未設定: {asset}]\n{GM.GetContent(f)}";
                var t = UIKit.Label(win.Content, info, 17, Theme.Text, TextAnchor.UpperLeft);
                UIKit.Stretch(t.rectTransform, 20, 20, 20, 70);

                var play = UIKit.Button(win.Content, "▶ 再生", () => { if (clip != null) OSRoot.I.PlayClip(clip); });
                UIKit.Place((RectTransform)play.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(140, 38));

                // 逆再生：謎解きに使う（押すと「reversed_ファイルID」フラグが立つ）
                var rev = UIKit.Button(win.Content, "◀ 逆再生", () =>
                {
                    if (clip != null) OSRoot.I.PlayClip(Reverse(clip));
                    GM.SetFlag("reversed_" + f.id);
                });
                UIKit.Place((RectTransform)rev.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(170, 16), new Vector2(140, 38));
            });
        }

        /// <summary>音声を逆向きにした新しいクリップを作る（Load Type が Decompress On Load の音声のみ）</summary>
        static AudioClip Reverse(AudioClip c)
        {
            try
            {
                int ch = c.channels, frames = c.samples;
                var data = new float[frames * ch];
                if (!c.GetData(data, 0)) return c;
                var rev = new float[data.Length];
                for (int i = 0; i < frames; i++)
                    for (int k = 0; k < ch; k++)
                        rev[i * ch + k] = data[(frames - 1 - i) * ch + k];
                var r = AudioClip.Create(c.name + "_reverse", frames, ch, c.frequency, false);
                r.SetData(rev, 0);
                return r;
            }
            catch { return c; }
        }

        // ---------- 実行確認（初期化ツールなど） ----------
        static void OpenConfirm(FileEntry f)
        {
            WM.Open("file:" + f.id, GM.DisplayName(f), new Vector2(480, 240), win =>
            {
                var t = UIKit.Label(win.Content, GM.GetContent(f), 18, Theme.Text);
                UIKit.Stretch(t.rectTransform, 24, 24, 24, 70);
                var yes = UIKit.Button(win.Content, "はい", () => { win.Close(); GM.SetFlag(f.setFlag); }, Theme.Danger, Color.white);
                UIKit.Place((RectTransform)yes.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-138, 14), new Vector2(110, 36));
                var no = UIKit.Button(win.Content, "いいえ", win.Close);
                UIKit.Place((RectTransform)no.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 14), new Vector2(110, 36));
            });
        }

        // ---------- パスワード入力 ----------
        public static void OpenPassword(FileEntry f)
        {
            WM.Open("pw:" + f.id, "パスワード", new Vector2(460, 240), win =>
            {
                var t = UIKit.Label(win.Content, $"「{GM.DisplayName(f)}」はパスワードで保護されています。", 17, Theme.Text);
                UIKit.Top(t.rectTransform, 30, 20, 20);
                var input = UIKit.Input(win.Content, "パスワード");
                input.contentType = TMP_InputField.ContentType.Password;
                UIKit.Top((RectTransform)input.transform, 40, 20, 62);
                var err = UIKit.Label(win.Content, "", 15, Theme.Danger);
                UIKit.Top(err.rectTransform, 24, 20, 110);

                void Submit()
                {
                    if (input.text.Trim() == f.password)
                    {
                        win.Close();
                        GM.Unlock(f);
                        GM.OpenFile(f);
                    }
                    else
                    {
                        err.text = "パスワードが違います";
                        input.text = "";
                        input.ActivateInputField();
                    }
                }

                input.onEndEdit.AddListener(_ => { if (input.text.Length > 0) Submit(); });
                var ok = UIKit.Button(win.Content, "OK", Submit, Theme.Accent, Color.white);
                UIKit.Place((RectTransform)ok.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 14), new Vector2(110, 36));
                input.ActivateInputField();
            });
        }
    }

    /// <summary>メモ帳の中身を最新に保つ（イベントでファイルが書き換わったときも反映）</summary>
    public class TextFileBinder : MonoBehaviour
    {
        FileEntry file;
        TMP_Text text;
        OSWindow win;

        public void Bind(FileEntry f, TMP_Text t, OSWindow w)
        {
            file = f; text = t; win = w;
            GameManager.I.FilesChanged += Reload;
            GameManager.I.FlagSet += OnFlag; // 「わかったこと」メモのように、フラグで中身が変わるファイルのため
            Reload();
        }

        void OnDestroy()
        {
            if (GameManager.I == null) return;
            GameManager.I.FilesChanged -= Reload;
            GameManager.I.FlagSet -= OnFlag;
        }

        void OnFlag(string _) => Reload();

        public void Reload()
        {
            if (file == null || text == null) return;
            text.text = GameManager.I.GetContent(file);
            win.SetTitle(GameManager.I.DisplayName(file) + " - メモ帳");
        }
    }
}
