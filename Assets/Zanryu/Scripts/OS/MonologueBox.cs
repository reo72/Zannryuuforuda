using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// RPG風のメッセージウィンドウ（画面下）。主人公の独り言やヒントを1ページずつ表示する。
    ///  ・文字は1文字ずつ出る
    ///  ・クリック / Space / Enter で、全文表示 → 次のページ
    ///  ・最後のページを送るとウィンドウが閉じる
    ///  ・表示中は画面のどこをクリックしても進む（読み終わるまでPCの操作は止まる）
    /// </summary>
    public class MonologueBox : MonoBehaviour
    {
        class Page { public int id; public string text; public string speaker; }


        readonly Queue<Page> queue = new Queue<Page>();
        Page current;
        int nextId;
        int finishedId = -1;

        GameObject root, namePlate, catcher;
        TMP_Text body, nameText, arrow;
        string full = "";
        float typed;
        int lastBlip;
        bool typing;

        public bool Busy => current != null || queue.Count > 0;

        public void Init(RectTransform layer)
        {
            // 独り言の表示中は、画面全体のクリックを受け取って次へ進める（RPG と同じ操作）
            var catchImg = UIKit.Panel(layer, new Color(0, 0, 0, 0), "ClickCatcher");
            UIKit.Stretch(catchImg.rectTransform);
            var cc = catchImg.gameObject.AddComponent<ClickHandler>();
            cc.Single = Advance;
            cc.Double = Advance;
            catcher = catchImg.gameObject;
            catcher.SetActive(false);

            var panel = UIKit.Panel(layer, new Color(0.03f, 0.04f, 0.06f, 0.90f), "MonologueWindow");
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 66), new Vector2(1180, 176));
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.75f, 0.80f, 0.88f, 0.55f);
            border.effectDistance = new Vector2(2, -2);
            root = panel.gameObject;

            // 名前欄（speaker があるときだけ表示）
            var plate = UIKit.Panel(panel.transform, new Color(0.10f, 0.12f, 0.16f, 0.97f), "NamePlate");
            UIKit.Place(plate.rectTransform, new Vector2(0, 1), new Vector2(0, 0), new Vector2(28, -12), new Vector2(240, 40));
            var plateBorder = plate.gameObject.AddComponent<Outline>();
            plateBorder.effectColor = new Color(0.75f, 0.80f, 0.88f, 0.55f);
            nameText = UIKit.Label(plate.transform, "", 19, Theme.TextLight, TextAnchor.MiddleCenter);
            UIKit.Stretch(nameText.rectTransform);
            namePlate = plate.gameObject;

            body = UIKit.Label(panel.transform, "", 25, new Color(0.94f, 0.94f, 0.95f));
            body.lineSpacing = 18;
            UIKit.Stretch(body.rectTransform, 44, 34, 70, 20);

            arrow = UIKit.Label(panel.transform, "▼", 20, new Color(0.85f, 0.88f, 0.95f), TextAnchor.MiddleCenter);
            UIKit.Place(arrow.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 14), new Vector2(30, 30));

            var click = panel.gameObject.AddComponent<ClickHandler>();
            click.Single = Advance;
            click.Double = Advance;

            root.SetActive(false);
        }

        /// <summary>独り言を1ページ追加する。戻り値はページ番号（待つときに使う）。</summary>
        public int Say(string text, string speaker = null)
        {
            if (string.IsNullOrEmpty(text)) return finishedId;
            var p = new Page { id = nextId++, text = TextResolver.Resolve(text), speaker = TextResolver.Resolve(speaker) };
            queue.Enqueue(p);
            if (current == null) Next();
            return p.id;
        }

        /// <summary>複数ページをまとめて追加</summary>
        public int SayLines(IEnumerable<string> lines, string speaker = null)
        {
            int last = finishedId;
            foreach (var l in lines) last = Say(l, speaker);
            return last;
        }

        /// <summary>表示して、プレイヤーがそのページを送るまで待つ（イベント用）</summary>
        public IEnumerator SayAndWait(string text, string speaker = null)
        {
            int id = Say(text, speaker);
            yield return new WaitUntil(() => finishedId >= id);
        }

        public IEnumerator WaitUntilIdle()
        {
            yield return new WaitUntil(() => !Busy);
        }

        public void Clear()
        {
            queue.Clear();
            current = null;
            finishedId = nextId - 1; // 待っている処理を全部解放する
            if (root != null) root.SetActive(false);
            if (catcher != null) catcher.SetActive(false);
        }

        void Next()
        {
            if (current != null) finishedId = current.id;
            if (queue.Count == 0)
            {
                current = null;
                root.SetActive(false);
                catcher.SetActive(false);
                return;
            }
            current = queue.Dequeue();
            full = current.text ?? "";
            typed = 0;
            lastBlip = 0;
            typing = true;
            body.text = "";
            bool hasName = !string.IsNullOrEmpty(current.speaker);
            namePlate.SetActive(hasName);
            nameText.text = hasName ? current.speaker : "";
            root.SetActive(true);
            catcher.SetActive(true);
        }

        public void Advance()
        {
            if (current == null) return;
            if (typing)
            {
                typing = false;
                body.text = full;
                return;
            }
            Next();
        }

        void Update()
        {
            if (current == null) return;

            if (typing)
            {
                typed += Time.deltaTime * Settings.CharsPerSecond;
                int n = Mathf.Min(full.Length, (int)typed);
                body.text = full.Substring(0, n);
                // 2文字ごとに小さな「ポッ」という音（RPG風）
                if (n - lastBlip >= 2)
                {
                    lastBlip = n;
                    if (!char.IsWhiteSpace(full[n - 1])) OSRoot.I.PlaySound("type", 0.35f);
                }
                if (n >= full.Length) typing = false;
            }
            arrow.enabled = !typing && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.5f;

            if (InputCompat.AdvancePressed() && !IsTypingInField()) Advance();
        }

        // パスワード入力中に Space / Enter でメッセージが進まないようにする
        static bool IsTypingInField()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var sel = es.currentSelectedGameObject;
            return sel != null && sel.TryGetComponent<TMP_InputField>(out _);
        }
    }
}
