using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// メールアプリ「Lumen Mail」（Gmail 風）。中身の進行は ChatSystem が担当。
    ///  ・左：作成ボタンとフォルダ（受信トレイなど）
    ///  ・右：受信トレイの一覧 → クリックでスレッド表示
    ///  ・スレッドの下に「返信の候補」ボタン（＝選択肢）が出る
    /// </summary>
    public class ChatApp : MonoBehaviour
    {
        public static void Open()
        {
            OSRoot.I.Windows.Open("chat", "Lumen Mail", new Vector2(1060, 720),
                w => w.gameObject.AddComponent<ChatApp>().Build(w));
        }

        static readonly Color Sidebar = new Color(0.96f, 0.97f, 0.98f);
        static readonly Color Selected = new Color(0.83f, 0.89f, 0.99f);
        static readonly Color ReadRow = new Color(0.95f, 0.96f, 0.97f);
        static readonly Color Line = new Color(0, 0, 0, 0.08f);
        static readonly Color Dim = new Color(0.40f, 0.42f, 0.45f);
        static readonly Color[] AvatarColors =
        {
            new Color(0.85f, 0.35f, 0.30f), new Color(0.30f, 0.55f, 0.85f), new Color(0.35f, 0.65f, 0.40f),
            new Color(0.60f, 0.40f, 0.75f), new Color(0.90f, 0.55f, 0.20f), new Color(0.40f, 0.45f, 0.50f)
        };

        OSWindow win;
        RectTransform main, sidebar;
        string openThread;      // null なら一覧
        string folder = "inbox";
        GameManager gm;

        void Build(OSWindow w)
        {
            win = w;
            gm = GameManager.I;
            var c = w.Content;
            c.GetComponent<Image>().color = Color.white;

            // 上のバー（ロゴと検索窓）
            var top = UIKit.Panel(c, Sidebar, "Top");
            UIKit.Top(top.rectTransform, 58);
            var logo = UIKit.Label(top.transform, "Lumen Mail", 22, new Color(0.85f, 0.30f, 0.25f), TextAnchor.MiddleLeft);
            UIKit.Stretch(logo.rectTransform, 22, 0, 0, 0);
            logo.fontStyle = FontStyles.Bold;
            var search = UIKit.Panel(top.transform, new Color(0.91f, 0.93f, 0.96f), "Search");
            UIKit.Stretch(search.rectTransform, 220, 10, 160, 10);
            var st = UIKit.Label(search.transform, "メールを検索", 15, Dim, TextAnchor.MiddleLeft);
            UIKit.Stretch(st.rectTransform, 16, 0, 0, 0);
            var acct = UIKit.Label(top.transform, gm.AccountName, 15, Dim, TextAnchor.MiddleRight);
            UIKit.Stretch(acct.rectTransform, 0, 0, 20, 0);

            sidebar = UIKit.Panel(c, Sidebar, "Sidebar").rectTransform;
            sidebar.anchorMin = new Vector2(0, 0);
            sidebar.anchorMax = new Vector2(0, 1);
            sidebar.pivot = new Vector2(0, 0.5f);
            sidebar.offsetMin = Vector2.zero;
            sidebar.offsetMax = new Vector2(210, -58);

            var area = UIKit.Rect("Main", c);
            UIKit.Stretch(area, 210, 58, 0, 0);
            main = area;

            gm.Chat.Updated += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (gm != null && gm.Chat != null) gm.Chat.Updated -= Refresh;
        }

        // ================= 共通 =================

        List<ChatThread> Threads()
        {
            var hist = gm.State.chatHistory;
            // 最後にメールが来た順（新しいものが上）
            return hist.Select((l, i) => (l.thread, i))
                       .GroupBy(x => x.thread)
                       .Select(g => (thread: Content.GetThread(g.Key), last: g.Max(x => x.i)))
                       .Where(x => x.thread != null && gm.ProfileOk(x.thread.profile))
                       .OrderByDescending(x => x.last)
                       .Select(x => x.thread).ToList();
        }

        void Refresh()
        {
            if (main == null) return;
            BuildSidebar();
            UIKit.Clear(main);
            if (folder == "inbox" && openThread != null && Content.GetThread(openThread) != null) BuildThread(openThread);
            else BuildList();
        }

        void BuildSidebar()
        {
            UIKit.Clear(sidebar);
            var compose = UIKit.Button(sidebar, "＋  作成", () =>
                OSRoot.I.MessageBox("Lumen Mail", "このアカウントからは、メールを送信できません。"),
                new Color(0.76f, 0.86f, 0.98f), Theme.Text, 16);
            UIKit.Place((RectTransform)compose.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(150, 50));

            int unread = gm.State.unread.Count(id => { var t = Content.GetThread(id); return t != null && gm.ProfileOk(t.profile); });
            var folders = new[] { ("inbox", "受信トレイ"), ("starred", "スター付き"), ("sent", "送信済み"), ("drafts", "下書き"), ("trash", "ゴミ箱") };
            float y = -84;
            foreach (var (key, name) in folders)
            {
                var k = key;
                bool sel = folder == key;
                var row = UIKit.Panel(sidebar, sel ? new Color(0.99f, 0.86f, 0.84f) : new Color(1, 1, 1, 0), "Folder:" + key);
                UIKit.Place(row.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(200, 36));
                var label = UIKit.Label(row.transform, name, 15, sel ? new Color(0.70f, 0.15f, 0.12f) : Theme.Text, TextAnchor.MiddleLeft);
                UIKit.Stretch(label.rectTransform, 26, 0, 40, 0);
                if (sel || (key == "inbox" && unread > 0)) label.fontStyle = FontStyles.Bold;
                if (key == "inbox" && unread > 0)
                {
                    var n = UIKit.Label(row.transform, unread.ToString(), 14, Theme.Text, TextAnchor.MiddleRight);
                    UIKit.Stretch(n.rectTransform, 0, 0, 14, 0);
                    n.fontStyle = FontStyles.Bold;
                }
                var click = row.gameObject.AddComponent<ClickHandler>();
                click.Single = () => { folder = k; openThread = null; OSRoot.I.PlaySound("click", 0.5f); Refresh(); };
                y -= 38;
            }
        }

        string DateOf(ChatLine l)
        {
            var c = Content.Config;
            int day = Mathf.Max(1, l.day);
            return $"{c.startMonth}月{c.startDay + day - 1}日";
        }

        string Address(ChatThread t) => string.IsNullOrEmpty(t.address) ? "" : TextResolver.Resolve(t.address);
        string Subject(ChatThread t) => string.IsNullOrEmpty(t.subject) ? "（件名なし）" : TextResolver.Resolve(t.subject);
        string MyAddress => gm.AccountName + "@lumen.local";

        // ================= 一覧 =================

        void BuildList()
        {
            win.SetTitle("受信トレイ - Lumen Mail");
            var bar = UIKit.Panel(main, Color.white, "ListBar");
            UIKit.Top(bar.rectTransform, 44);
            var tabs = UIKit.Label(bar.transform, "メイン　　　　ソーシャル　　　　プロモーション", 14, Dim, TextAnchor.MiddleLeft);
            UIKit.Stretch(tabs.rectTransform, 20, 0, 0, 0);
            var rule = UIKit.Panel(main, Line, "Rule");
            UIKit.Top(rule.rectTransform, 1, 0, 44);

            var area = UIKit.Rect("List", main);
            UIKit.Stretch(area, 0, 45, 0, 0);
            var list = UIKit.ScrollList(area, out _, 0, 0);

            var threads = folder == "inbox" ? Threads() : new List<ChatThread>();
            if (threads.Count == 0)
            {
                var none = UIKit.Label(list, folder == "inbox" ? "メールはありません。" : "このフォルダにメールはありません。", 16, Dim, TextAnchor.MiddleCenter);
                UIKit.Size(none, 120);
                return;
            }
            foreach (var t in threads) ListRow(list, t);
        }

        void ListRow(RectTransform list, ChatThread t)
        {
            var lines = gm.State.chatHistory.Where(l => l.thread == t.id).ToList();
            var last = lines.LastOrDefault();
            bool unread = gm.State.unread.Contains(t.id);

            var row = UIKit.Panel(list, unread ? Color.white : ReadRow, "Row:" + t.id);
            UIKit.Size(row, 46);
            var star = UIKit.Label(row.transform, "☆", 18, new Color(0.65f, 0.65f, 0.68f), TextAnchor.MiddleCenter);
            UIKit.Place(star.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(28, 40));

            var from = UIKit.Label(row.transform, t.contact + (lines.Count > 1 ? $"  {lines.Count}" : ""), 15, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Place(from.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(44, 0), new Vector2(190, 40));
            from.textWrappingMode = TextWrappingModes.NoWrap;
            from.overflowMode = TextOverflowModes.Ellipsis;

            var snippet = last == null ? "" : last.text.Replace("\n", " ");
            var subj = UIKit.Label(row.transform, $"{Subject(t)}  -  {snippet}", 15, unread ? Theme.Text : Dim, TextAnchor.MiddleLeft);
            UIKit.Stretch(subj.rectTransform, 244, 0, 110, 0);
            subj.textWrappingMode = TextWrappingModes.NoWrap;
            subj.overflowMode = TextOverflowModes.Ellipsis;

            var date = UIKit.Label(row.transform, last == null ? "" : (string.IsNullOrEmpty(last.time) ? DateOf(last) : last.time), 13, unread ? Theme.Text : Dim, TextAnchor.MiddleRight);
            UIKit.Stretch(date.rectTransform, 0, 0, 18, 0);

            if (unread)
            {
                from.fontStyle = FontStyles.Bold;
                date.fontStyle = FontStyles.Bold;
            }
            var rule = UIKit.Panel(row.transform, Line, "Rule");
            rule.raycastTarget = false;
            UIKit.Bottom(rule.rectTransform, 1);

            var id = t.id;
            var click = row.gameObject.AddComponent<ClickHandler>();
            click.Single = () => { OSRoot.I.PlaySound("click", 0.5f); openThread = id; Refresh(); };
            click.Double = click.Single;
        }

        // ================= スレッド =================

        void BuildThread(string threadId)
        {
            var t = Content.GetThread(threadId);
            gm.Chat.MarkRead(threadId);
            win.SetTitle(Subject(t) + " - Lumen Mail");

            var bar = UIKit.Panel(main, Color.white, "ThreadBar");
            UIKit.Top(bar.rectTransform, 44);
            var back = UIKit.Button(bar.transform, "←  受信トレイ", () => { openThread = null; Refresh(); }, new Color(0.94f, 0.95f, 0.97f), Theme.Text, 14);
            UIKit.Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(140, 32));

            var choices = gm.Chat.GetChoices(threadId);
            float bottom = choices != null ? 74 : 60;
            var area = UIKit.Rect("Thread", main);
            UIKit.Stretch(area, 0, 44, 0, bottom);
            var list = UIKit.ScrollList(area, out var scroll, 12, 22);

            var subject = UIKit.Label(list, Subject(t), 24, Theme.Text);
            var chip = UIKit.Label(list, "受信トレイ", 12, Dim);

            int colorIdx = Mathf.Abs(t.id.GetHashCode()) % AvatarColors.Length;
            foreach (var l in gm.State.chatHistory.Where(x => x.thread == threadId))
                MessageCard(list, t, l, colorIdx);

            // 返信の候補（＝選択肢）
            var footer = UIKit.Panel(main, Color.white, "Footer");
            UIKit.Bottom(footer.rectTransform, bottom);
            var fr = UIKit.Panel(footer.transform, Line, "Rule");
            UIKit.Top(fr.rectTransform, 1);
            var row = UIKit.Rect("Replies", footer.transform);
            UIKit.Stretch(row, 22, 14, 22, 12);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            if (choices != null)
            {
                var label = UIKit.Label(row, "返信の候補：", 14, Dim, TextAnchor.MiddleLeft);
                UIKit.Size(label, -1, 100);
                for (int i = 0; i < choices.Count; i++)
                {
                    int idx = i;
                    var text = TextResolver.Resolve(choices[i].text);
                    var b = UIKit.Button(row, text, () => gm.Chat.Choose(threadId, idx), new Color(0.90f, 0.94f, 1f), new Color(0.10f, 0.35f, 0.75f), 15);
                    var tmp = b.GetComponentInChildren<TMP_Text>();
                    UIKit.Size(b, -1, Mathf.Clamp(tmp.GetPreferredValues(text).x + 40, 120, 420));
                }
            }
            else
            {
                foreach (var name in new[] { "↩  返信", "↪  転送" })
                {
                    var b = UIKit.Button(row, name, () => OSRoot.I.Toast("Lumen Mail\n今は返信できません"), Color.white, Dim, 14);
                    UIKit.Size(b, -1, 120);
                }
            }

            if (isActiveAndEnabled) StartCoroutine(ScrollToBottom(scroll));
        }

        void MessageCard(RectTransform list, ChatThread t, ChatLine l, int colorIdx)
        {
            bool mine = l.from == "me";
            var card = UIKit.Panel(list, Color.white, "Mail");
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(64, 16, 10, 14);
            v.spacing = 6;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;

            // アイコン（頭文字の丸）
            var avatar = UIKit.Panel(card.transform, mine ? AvatarColors[5] : AvatarColors[colorIdx], "Avatar");
            avatar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIKit.Place(avatar.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -12), new Vector2(40, 40));
            var name = mine ? "自分" : t.contact;
            var initial = UIKit.Label(avatar.transform, name.Length > 0 ? name.Substring(0, 1) : "?", 18, Color.white, TextAnchor.MiddleCenter);
            UIKit.Stretch(initial.rectTransform);

            var head = UIKit.Label(card.transform, mine ? $"自分  <{MyAddress}>" : $"{t.contact}  <{Address(t)}>", 15, Theme.Text);
            head.fontStyle = FontStyles.Bold;
            var to = UIKit.Label(card.transform, $"宛先：{(mine ? t.contact : "自分")}　　{DateOf(l)} {l.time}", 12, Dim);
            var body = UIKit.Label(card.transform, l.text, 17, Theme.Text);
            body.lineSpacing = 8;

            var rule = UIKit.Panel(list, Line, "Rule");
            UIKit.Size(rule, 1);
        }

        IEnumerator ScrollToBottom(ScrollRect s)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (s != null) s.verticalNormalizedPosition = 0;
        }
    }
}
