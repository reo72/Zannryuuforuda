using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>メッセージアプリの画面（左に相手一覧、右に会話と返信の選択肢）</summary>
    public class ChatApp : MonoBehaviour
    {
        public static void Open()
        {
            OSRoot.I.Windows.Open("chat", "メッセージ", new Vector2(860, 580),
                w => w.gameObject.AddComponent<ChatApp>().Build(w));
        }

        RectTransform threadList, messageList, choiceArea;
        ScrollRect messageScroll;
        TMP_Text header;
        string current;
        GameManager gm;

        void Build(OSWindow w)
        {
            gm = GameManager.I;
            var c = w.Content;

            var left = UIKit.Panel(c, Theme.Toolbar, "Threads");
            left.rectTransform.anchorMin = new Vector2(0, 0);
            left.rectTransform.anchorMax = new Vector2(0, 1);
            left.rectTransform.pivot = new Vector2(0, 0.5f);
            left.rectTransform.offsetMin = Vector2.zero;
            left.rectTransform.offsetMax = new Vector2(230, 0);
            threadList = UIKit.ScrollList(left.transform, out var tsr, 4, 8);
            UIKit.Stretch((RectTransform)tsr.transform);

            var right = UIKit.Rect("Conversation", c);
            UIKit.Stretch(right, 230, 0, 0, 0);

            var head = UIKit.Panel(right, new Color(0.88f, 0.88f, 0.87f), "Header");
            UIKit.Top(head.rectTransform, 44);
            header = UIKit.Label(head.transform, "", 18, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(header.rectTransform, 16, 0, 16, 0);

            var msgArea = UIKit.Rect("Messages", right);
            UIKit.Stretch(msgArea, 0, 44, 0, 150);
            messageList = UIKit.ScrollList(msgArea, out messageScroll, 8, 14);
            UIKit.Stretch((RectTransform)messageScroll.transform);

            var choiceBg = UIKit.Panel(right, new Color(0.86f, 0.86f, 0.85f), "Choices");
            UIKit.Bottom(choiceBg.rectTransform, 150);
            choiceArea = UIKit.Stretch(UIKit.Rect("ChoiceList", choiceBg.transform), 12, 10, 12, 10);
            var v = choiceArea.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;

            gm.Chat.Updated += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (gm != null && gm.Chat != null) gm.Chat.Updated -= Refresh;
        }

        void Refresh()
        {
            if (threadList == null) return;
            var hist = gm.State.chatHistory;

            // やりとりがある相手だけ表示（届いた順）
            var threads = hist.Select(l => l.thread).Distinct()
                              .Select(Content.GetThread)
                              .Where(t => t != null && gm.ProfileOk(t.profile))
                              .ToList();
            if (current == null || threads.All(t => t.id != current))
                current = threads.Count > 0 ? threads[threads.Count - 1].id : null;

            UIKit.Clear(threadList);
            foreach (var t in threads)
            {
                var th = t;
                bool unread = gm.State.unread.Contains(t.id) && t.id != current;
                var b = UIKit.Button(threadList, (unread ? "● " : "") + t.contact, () => { current = th.id; Refresh(); },
                    t.id == current ? new Color(0.72f, 0.80f, 0.92f) : new Color(0.93f, 0.93f, 0.92f), Theme.Text, 16);
                b.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
                UIKit.Size(b, 48);
            }

            UIKit.Clear(messageList);
            UIKit.Clear(choiceArea);
            if (current == null)
            {
                header.text = "メッセージはありません";
                return;
            }

            var thread = Content.GetThread(current);
            header.text = thread.contact;
            gm.Chat.MarkRead(current);

            foreach (var line in hist.Where(l => l.thread == current)) AddBubble(line.text, line.from == "me");

            var choices = gm.Chat.GetChoices(current);
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    int idx = i;
                    var b = UIKit.Button(choiceArea, TextResolver.Resolve(choices[i].text), () => gm.Chat.Choose(current, idx),
                        Color.white, Theme.Text, 16);
                    UIKit.Size(b, 36);
                }
            }
            else
            {
                var t = UIKit.Label(choiceArea, "（返信できるメッセージはありません）", 14, Theme.TextDim, TextAnchor.MiddleCenter);
                UIKit.Size(t, 36);
            }

            if (isActiveAndEnabled) StartCoroutine(ScrollToBottom());
        }

        void AddBubble(string text, bool mine)
        {
            var row = UIKit.Rect("Row", messageList);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = mine ? new RectOffset(160, 0, 0, 0) : new RectOffset(0, 160, 0, 0);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;

            var bubble = UIKit.Panel(row, mine ? Theme.BubbleMe : Theme.BubbleThem, "Bubble");
            var v = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 10, 10);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            var t = UIKit.Label(bubble.transform, text, 17, Theme.Text, mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            t.gameObject.name = "Text";
        }

        IEnumerator ScrollToBottom()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (messageScroll != null) messageScroll.verticalNormalizedPosition = 0;
        }
    }
}
