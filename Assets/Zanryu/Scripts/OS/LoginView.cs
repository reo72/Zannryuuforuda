using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// ログイン画面。
    ///  ・日によってパスワードが変わる（config.json の loginPasswords）
    ///  ・間違えるたびに反応が変わり、最後は勝手にログインされる（wrongPasswordMessages）
    ///  ・隠しパスワードで何かが起きる（secretPasswords）
    ///  ・灰色のアカウントも、隠しパスワードだけは受け付ける
    ///  ・メタ演出のファイルが消されていたら気づく
    ///  ・メタ演出の ON/OFF はタイトル画面の「設定」
    /// </summary>
    public class LoginView : MonoBehaviour
    {
        GameManager gm;
        GameObject pwPanel;
        TMP_Text pwTitle, info, hintText, warning;
        TMP_InputField input;
        AccountDef selected;
        LoginPassword currentPw;
        bool selectable;
        int wrong;
        bool busy;
        bool assisting;   // 何を打っても正しい文字に変わるモード

        /// <summary>この回数だけ間違えると、次の入力から「勝手に正しい文字になる」</summary>
        const int AssistAfterWrong = 3;

        static readonly Color Dim = new Color(0.55f, 0.56f, 0.60f);
        static readonly Color Red = new Color(0.82f, 0.42f, 0.42f);

        public void Build()
        {
            gm = GameManager.I;
            var rt = (RectTransform)transform;
            gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f);

            var title = UIKit.Label(rt, Content.Config.osName, 40, Theme.TextLight, TextAnchor.MiddleCenter);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(800, 60));
            var date = UIKit.Label(rt, DateForMain(), 20, Dim, TextAnchor.MiddleCenter);
            UIKit.Place(date.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 285), new Vector2(800, 30));

            // アカウント一覧
            var list = UIKit.Rect("Accounts", rt);
            UIKit.Place(list, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 240), new Vector2(420, 330));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            foreach (var a in Content.Config.accounts)
            {
                if (!gm.HasEndings(a.showAfterEndings)) continue;
                bool ok = IsSelectable(a);
                var acc = a;
                var b = UIKit.Button(list, "   " + TextResolver.Resolve(a.name), () => Select(acc),
                    ok ? new Color(0.22f, 0.25f, 0.30f) : new Color(0.14f, 0.15f, 0.17f),
                    ok ? Theme.TextLight : new Color(0.38f, 0.38f, 0.40f), 22);
                b.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
                UIKit.Size(b, 60);
            }

            // パスワード入力欄（アカウントを選ぶと出る）
            var panel = UIKit.Panel(rt, new Color(0.10f, 0.11f, 0.14f, 0.96f), "PasswordPanel");
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(520, 200));
            pwPanel = panel.gameObject;
            pwTitle = UIKit.Label(panel.transform, "", 18, Theme.TextLight, TextAnchor.MiddleLeft);
            UIKit.Top(pwTitle.rectTransform, 30, 20, 14);
            input = UIKit.Input(panel.transform, "パスワード", 20);
            input.contentType = TMP_InputField.ContentType.Password;
            UIKit.Top((RectTransform)input.transform, 44, 20, 52);
            input.onSubmit.AddListener(_ => Submit());
            var ok2 = UIKit.Button(panel.transform, "→", Submit, Theme.Accent, Color.white, 22);
            UIKit.Place((RectTransform)ok2.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -52), new Vector2(60, 44));
            ((RectTransform)input.transform).offsetMax = new Vector2(-88, ((RectTransform)input.transform).offsetMax.y);
            var hintBtn = UIKit.Button(panel.transform, "ヒント", ShowHint, new Color(0.18f, 0.19f, 0.22f), Theme.TextLight, 14);
            UIKit.Place((RectTransform)hintBtn.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(90, 32));
            hintText = UIKit.Label(panel.transform, "", 16, Dim, TextAnchor.MiddleLeft);
            UIKit.Place(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(124, 16), new Vector2(380, 32));
            info = UIKit.Label(panel.transform, "", 16, Red, TextAnchor.MiddleLeft);
            UIKit.Top(info.rectTransform, 26, 20, 104);
            pwPanel.SetActive(false);

            // メタ演出のファイルが消されていたら……
            warning = UIKit.Label(rt, "", 26, Red, TextAnchor.MiddleCenter);
            UIKit.Place(warning.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(900, 40));
            if (RealDesktop.NewlyDeleted().Count > 0) StartCoroutine(ShowDeleted());

            var back = UIKit.Button(rt, "タイトルへ", () => OSRoot.I.ShowTitle(), new Color(0.14f, 0.15f, 0.17f), Dim, 14);
            UIKit.Place((RectTransform)back.transform, Vector2.zero, Vector2.zero, new Vector2(24, 24), new Vector2(160, 36));
        }

        string DateForMain()
        {
            var c = Content.Config;
            return c.dateFormat.Replace("{M}", c.startMonth.ToString()).Replace("{D}", (c.startDay + gm.MainDay - 1).ToString());
        }

        bool IsSelectable(AccountDef a) => a.selectable && gm.HasEndings(a.unlockEndings);

        LoginPassword PasswordFor(AccountDef a, int day) =>
            Content.Config.loginPasswords.FirstOrDefault(p =>
                p.account == a.id && day >= Mathf.Max(1, p.fromDay) && (p.toDay <= 0 || day <= p.toDay));

        int DayFor(AccountDef a) => (string.IsNullOrEmpty(a.profile) || a.profile == "main") ? gm.MainDay : 1;

        void Select(AccountDef a)
        {
            if (busy) return;
            selectable = IsSelectable(a);
            int day = DayFor(a);
            currentPw = selectable ? PasswordFor(a, day) : null;

            // パスワードなしのアカウントはそのまま入る
            if (selectable && currentPw == null) { OSRoot.I.PlaySound("login", 0.8f); gm.LoginAs(a); return; }

            if (selected != a) wrong = 0;
            selected = a;
            SetAssist(false);
            pwPanel.SetActive(true);
            pwTitle.text = $"{TextResolver.Resolve(a.name)} のパスワード";
            hintText.text = "";
            input.text = "";
            info.text = "";
            if (!selectable) info.text = "このアカウントは無効です";
            else
            {
                // 昨日とパスワードが変わっていたら知らせる（誰が変えた？）
                var yesterday = PasswordFor(a, day - 1);
                if (day > 1 && (yesterday == null || yesterday.password != currentPw.password))
                    info.text = "パスワードが変更されました";

                // 2周目以降（END を1つでも見た）は、メインのアカウントのパスワードを覚えている扱い
                bool mainAccount = string.IsNullOrEmpty(a.profile) || a.profile == "main";
                if (mainAccount && gm.Global.endings.Count > 0)
                {
                    var pw = TextResolver.Resolve(currentPw.password);
                    input.text = pw;
                    input.caretPosition = pw.Length;
                    hintText.text = $"覚えているパスワード：{pw}（Enter でログイン）";
                }
            }
            input.ActivateInputField();
        }

        void ShowHint()
        {
            if (selected == null) return;
            if (currentPw == null) { hintText.text = "ヒントはありません"; return; }
            // 2回以上間違えていたら、もっと具体的なヒントを出す
            var h = wrong >= 2 && !string.IsNullOrEmpty(currentPw.hint2) ? currentPw.hint2 : currentPw.hint;
            hintText.text = string.IsNullOrEmpty(h) ? "ヒントはありません" : "ヒント：" + TextResolver.Resolve(h);
        }

        static string Norm(string s) => (s ?? "").Trim().ToLowerInvariant();

        void Submit()
        {
            if (busy || selected == null) return;
            var text = Norm(input.text);
            if (text.Length == 0) return;
            input.text = "";

            // 1) 隠しパスワード
            var secret = FindSecret(selected, text);
            if (secret != null) { StartCoroutine(HandleSecret(secret)); return; }

            // 2) 正しいパスワード
            if (selectable && currentPw != null && text == Norm(TextResolver.Resolve(currentPw.password)))
            {
                wrong = 0;
                OSRoot.I.PlaySound("login", 0.8f);
                gm.LoginAs(selected);
                return;
            }

            // 3) 無効なアカウント
            if (!selectable) { info.text = "このアカウントは無効です"; input.ActivateInputField(); return; }

            // 4) 間違い：反応がだんだん変わり、最後は勝手にログインされる
            wrong++;
            OSRoot.I.PlaySound("error", 0.6f);
            var msgs = Content.Config.wrongPasswordMessages;
            if (msgs.Count == 0) { info.text = "パスワードが違います"; input.ActivateInputField(); return; }
            info.text = TextResolver.Resolve(msgs[Mathf.Min(wrong, msgs.Count) - 1]);
            if (wrong == 2 && currentPw != null && !string.IsNullOrEmpty(currentPw.hint2)) ShowHint();
            if (wrong >= AssistAfterWrong && wrong < msgs.Count) SetAssist(true);
            if (wrong >= msgs.Count) StartCoroutine(ForcedLogin(selected));
            else input.ActivateInputField();
        }

        SecretPassword FindSecret(AccountDef a, string text)
        {
            int day = DayFor(a);
            foreach (var s in Content.Config.secretPasswords)
            {
                if (s.account != "*" && s.account != a.id) continue;
                if (s.minDay > 0 && day < s.minDay) continue;
                if (s.maxDay > 0 && day > s.maxDay) continue;
                if (!gm.HasEndings(s.requireEndings)) continue;
                if (s.passwords.Any(p => Norm(TextResolver.Resolve(p)) == text)) return s;
            }
            return null;
        }

        IEnumerator HandleSecret(SecretPassword s)
        {
            busy = true;
            if (s.glitch) OSRoot.I.Glitch.Play(0.45f);
            gm.AddSecret(s.setSecret);
            info.text = TextResolver.Resolve(s.message);
            yield return new WaitForSeconds(s.login ? 1.6f : 0.5f);
            busy = false;
            if (s.login) gm.LoginAs(selected);
            else input.ActivateInputField();
        }

        /// <summary>
        /// 4回目の入力から：どのキーを押しても、正しいパスワードの次の1文字に置き換わる。
        /// 伏せ字をやめて文字を見せるので、「勝手に打たれていく」のが分かる。
        /// </summary>
        void SetAssist(bool on)
        {
            if (assisting == on && on) return;
            assisting = on;
            var text = input.textComponent;
            if (on && currentPw != null)
            {
                var answer = TextResolver.Resolve(currentPw.password);
                input.text = "";
                input.contentType = TMP_InputField.ContentType.Standard;
                input.onValidateInput = (current, index, added) =>
                    current.Length < answer.Length ? answer[current.Length] : '\0';
                if (text != null) text.color = new Color(0.62f, 0.14f, 0.16f);
                OSRoot.I.Glitch.Play(0.15f);
            }
            else
            {
                input.onValidateInput = null;
                input.contentType = TMP_InputField.ContentType.Password;
                if (text != null) text.color = Theme.Text;
            }
            input.ForceLabelUpdate();
        }

        IEnumerator ForcedLogin(AccountDef a)
        {
            busy = true;
            yield return new WaitForSeconds(1.4f);
            OSRoot.I.Glitch.Play(0.6f);
            yield return new WaitForSeconds(0.8f);
            busy = false;
            gm.LoginAs(a);
        }

        IEnumerator ShowDeleted()
        {
            yield return new WaitForSeconds(1.2f);
            OSRoot.I.Glitch.Play(0.25f);
            warning.text = "……けしたね";
            yield return new WaitForSeconds(3.5f);
            warning.text = "";
        }
    }
}
