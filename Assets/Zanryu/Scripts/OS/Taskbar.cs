using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// 画面下のタスクバー（アイコン式）
    ///  ・左端：スタートボタン（シャットダウンなど）
    ///  ・ピン留めアプリ：PC / メッセージ / タスクマネージャー（いつも表示。未読があると赤い点）
    ///  ・開いているファイル：アイコンで並ぶ
    ///  ・開いているものは下に線、前面のものは長い線。マウスを乗せると名前が出る
    ///  ・右側：ヒントボタンと時計
    /// </summary>
    public class Taskbar : MonoBehaviour
    {
        TMP_Text clock;
        RectTransform items;
        GameObject startMenu, tip;
        WindowManager wm;
        GameManager gm;

        const float ItemW = 54, BarH = 48;

        static readonly (string key, string title)[] Pinned =
        {
            ("explorer", "エクスプローラー"),
            ("browser", "Lumen Search"),
            ("chat", "Lumen Mail"),
            ("taskmgr", "タスクマネージャー"),
        };

        public void Build()
        {
            wm = OSRoot.I.Windows;
            gm = GameManager.I;
            var rt = (RectTransform)transform;
            UIKit.Bottom(rt, BarH);
            gameObject.AddComponent<Image>().color = Theme.Taskbar;

            // スタートボタン（Lumen OS のマーク）
            var start = MakeItem(rt, "Lumen OS", ToggleStart);
            UIKit.Place(start.Rect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(ItemW, BarH - 6));
            var logo = UIKit.LoadSprite("Icons/start");
            if (logo != null)
            {
                var img = UIKit.Panel(start.Rect, Color.white, "Logo");
                img.sprite = logo;
                img.preserveAspect = true;
                img.raycastTarget = false;
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32, 32));
            }
            else
            {
                var mark = UIKit.Label(start.Rect, "◐", 28, Theme.Accent, TextAnchor.MiddleCenter);
                UIKit.Stretch(mark.rectTransform);
            }

            items = UIKit.Stretch(UIKit.Rect("Items", rt), 66, 3, 290, 3);
            var h = items.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 2;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            // ヒントボタン
            var hint = UIKit.Button(rt, "？ ヒント", () => gm.Mono.ShowHintNow(), new Color(0.22f, 0.24f, 0.28f), Theme.TextLight, 14);
            UIKit.Place((RectTransform)hint.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-166, 0), new Vector2(104, 38));

            clock = UIKit.Label(rt, "", 15, Theme.TextLight, TextAnchor.MiddleCenter);
            UIKit.Place(clock.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(150, BarH));

            wm.Changed += Refresh;
            gm.Chat.Updated += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (wm != null) wm.Changed -= Refresh;
            if (gm != null && gm.Chat != null) gm.Chat.Updated -= Refresh;
            if (startMenu != null) Destroy(startMenu);
            HideTip();
        }

        void Update()
        {
            if (gm != null && clock != null) clock.text = gm.ClockText() + "\n" + gm.DateText();
        }

        // ================= アイコンの並び =================

        void Refresh()
        {
            if (items == null) return;
            HideTip();
            UIKit.Clear(items);

            // ピン留めアプリ
            foreach (var (key, title) in Pinned)
            {
                var win = wm.Get(key);
                var k = key;
                bool badge = key == "chat" && gm.State.unread.Count > 0;
                AddIcon(new FileEntry { type = "app", app = key }, title, win, () => OpenApp(k), badge);
            }

            // 区切り線
            var others = wm.All.Where(w => w != null && !Pinned.Any(p => p.key == w.Key)).ToList();
            if (others.Count > 0)
            {
                var sep = UIKit.Panel(items, new Color(1, 1, 1, 0.12f), "Separator");
                UIKit.Size(sep, -1, 1);
            }

            // 開いているファイル・ダイアログ
            foreach (var w in others) AddIcon(EntryFor(w), w.Title, w, null, false);
        }

        static void OpenApp(string key)
        {
            // デスクトップに同じアプリがあれば、そこから開いたことにする（独り言・フラグが働くように）
            string fileId = key == "explorer" ? "pc" : key;
            var f = Content.GetFile(fileId);
            if (f != null && GameManager.I.IsAvailable(f)) { GameManager.I.OpenFile(f); return; }
            switch (key)
            {
                case "explorer": ExplorerApp.Open("root"); break;
                case "chat": ChatApp.Open(); break;
                case "taskmgr": TaskManagerApp.Open(); break;
                case "browser": BrowserApp.Open(); break;
            }
        }

        static FileEntry EntryFor(OSWindow w)
        {
            var key = w.Key ?? "";
            int colon = key.IndexOf(':');
            if (colon > 0)
            {
                var f = Content.GetFile(key.Substring(colon + 1));
                if (f != null) return f;
            }
            return new FileEntry { type = "confirm" }; // エラーダイアログなど
        }

        void AddIcon(FileEntry entry, string title, OSWindow win, Action openWhenClosed, bool badge)
        {
            bool open = win != null;
            bool front = open && win.gameObject.activeSelf && wm.Focused == win;
            var item = MakeItem(items, title, () =>
            {
                if (win != null) OnWindowButton(win);
                else openWhenClosed?.Invoke();
            });
            item.Normal = front ? new Color(1, 1, 1, 0.12f) : new Color(1, 1, 1, 0);
            item.Bg.color = item.Normal;
            UIKit.Size(item.Bg, -1, ItemW);

            var icon = UIKit.FileIcon(item.Rect, entry, 30);
            UIKit.Place(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(30, 30));

            // 開いている印（下の線）
            if (open)
            {
                var line = UIKit.Panel(item.Rect, front ? Theme.Accent : new Color(0.75f, 0.78f, 0.85f, 0.8f), "Indicator");
                line.raycastTarget = false;
                UIKit.Place(line.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(front ? 22 : 8, 3));
            }
            // 未読の赤い点
            if (badge)
            {
                var dot = UIKit.Panel(item.Rect, new Color(0.88f, 0.26f, 0.26f), "Badge");
                dot.raycastTarget = false;
                UIKit.Place(dot.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(10, 10));
            }
        }

        TaskbarItem MakeItem(RectTransform parent, string title, Action onClick)
        {
            var bg = UIKit.Panel(parent, new Color(1, 1, 1, 0), "TaskItem");
            var item = bg.gameObject.AddComponent<TaskbarItem>();
            item.Bg = bg;
            item.Rect = bg.rectTransform;
            item.Title = title;
            item.Owner = this;
            item.OnClick = () =>
            {
                OSRoot.I.PlaySound("click", 0.5f);
                onClick?.Invoke();
            };
            return item;
        }

        void OnWindowButton(OSWindow w)
        {
            if (!w.gameObject.activeSelf) w.Restore();
            else if (wm.Focused == w) w.Minimize();
            else wm.Focus(w);
        }

        // ================= 名前の吹き出し =================

        public void ShowTip(string text, RectTransform over)
        {
            HideTip();
            if (string.IsNullOrEmpty(text)) return;
            var p = UIKit.Panel(transform.parent, new Color(0.13f, 0.14f, 0.17f, 0.97f), "Tooltip");
            p.raycastTarget = false;
            var t = UIKit.Label(p.transform, text, 14, Theme.TextLight, TextAnchor.MiddleCenter);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            float w = Mathf.Clamp(t.GetPreferredValues(text).x + 24, 60, 360);
            UIKit.Stretch(t.rectTransform);
            p.rectTransform.pivot = new Vector2(0.5f, 0);
            p.rectTransform.sizeDelta = new Vector2(w, 30);
            p.rectTransform.position = over.TransformPoint(new Vector3(0, over.rect.height * 0.5f + 8, 0));
            // 画面の端で見切れないように、左右をおさめる
            var parentRect = ((RectTransform)p.rectTransform.parent).rect;
            var ap = p.rectTransform.anchoredPosition;
            float half = parentRect.width * 0.5f, margin = 6;
            ap.x = Mathf.Clamp(ap.x, -half + w * 0.5f + margin, half - w * 0.5f - margin);
            p.rectTransform.anchoredPosition = ap;
            tip = p.gameObject;
        }

        public void HideTip()
        {
            if (tip != null) Destroy(tip);
            tip = null;
        }

        // ================= スタートメニュー =================

        void ToggleStart()
        {
            if (startMenu != null) { Destroy(startMenu); return; }
            var menu = UIKit.Panel(transform.parent, new Color(0.13f, 0.14f, 0.16f, 0.98f), "StartMenu");
            UIKit.Place(menu.rectTransform, Vector2.zero, Vector2.zero, new Vector2(6, BarH + 4), new Vector2(260, 190));
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

    /// <summary>タスクバーの1つのアイコン（マウスを乗せると明るくなり、名前が出る）</summary>
    public class TaskbarItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Image Bg;
        public RectTransform Rect;
        public string Title;
        public Taskbar Owner;
        public Action OnClick;
        public Color Normal = new Color(1, 1, 1, 0);

        static readonly Color Hover = new Color(1, 1, 1, 0.18f);

        public void OnPointerEnter(PointerEventData e)
        {
            Bg.color = Hover;
            if (Owner != null) Owner.ShowTip(Title, Rect);
        }

        public void OnPointerExit(PointerEventData e)
        {
            Bg.color = Normal;
            if (Owner != null) Owner.HideTip();
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            OnClick?.Invoke();
        }
    }
}
