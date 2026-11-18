using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>ウィンドウの生成・前後関係・フォーカスを管理する</summary>
    public class WindowManager : MonoBehaviour
    {
        RectTransform layer;
        readonly List<OSWindow> windows = new List<OSWindow>();
        int cascade;

        public IReadOnlyList<OSWindow> All => windows;
        public OSWindow Focused { get; private set; }
        public event Action Changed;

        public void Init(RectTransform l) => layer = l;

        public OSWindow Get(string key) =>
            string.IsNullOrEmpty(key) ? null : windows.FirstOrDefault(w => w != null && w.Key == key);

        /// <summary>
        /// key が同じウィンドウが既に開いていれば前面に出すだけ。
        /// key が null なら毎回新しく開く（エラーダイアログなど）。
        /// </summary>
        public OSWindow Open(string key, string title, Vector2 size, Action<OSWindow> build)
        {
            var existing = Get(key);
            if (existing != null) { existing.Restore(); return existing; }

            var w = OSWindow.Create(layer, title, size);
            w.Key = key;
            w.Manager = this;
            int i = cascade++ % 8;
            w.Rect.anchoredPosition = new Vector2(-120 + i * 30, 80 - i * 30);
            windows.Add(w);
            if (OSRoot.I != null) OSRoot.I.PlaySound("window_open", 0.6f);
            build?.Invoke(w);
            Focus(w);
            return w;
        }

        public void Focus(OSWindow w)
        {
            if (w == null) return;
            w.transform.SetAsLastSibling();
            Focused = w;
            foreach (var x in windows) x.SetActiveLook(x == w);
            Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();

        public void Remove(OSWindow w)
        {
            windows.Remove(w);
            if (Focused == w) Focused = windows.LastOrDefault(x => x.gameObject.activeSelf);
            Changed?.Invoke();
        }

        public void CloseAll()
        {
            foreach (var w in windows.ToList()) w.Close(true);
            cascade = 0;
        }
    }

    public class OSWindow : MonoBehaviour, IPointerDownHandler
    {
        public RectTransform Rect, Content;
        public string Key;
        public string Title;
        public WindowManager Manager;
        public event Action Closed;

        Image titleBg;
        TMP_Text titleText;
        bool closed;

        public static OSWindow Create(RectTransform layer, string title, Vector2 size)
        {
            var frame = UIKit.Panel(layer, Theme.WindowFrame, "Window");
            var rt = UIKit.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var shadow = frame.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.45f);
            shadow.effectDistance = new Vector2(5, -5);

            var w = frame.gameObject.AddComponent<OSWindow>();
            w.Rect = rt;
            w.Title = title;

            var bar = UIKit.Panel(rt, Theme.TitleBar, "TitleBar");
            UIKit.Top(bar.rectTransform, 34, 1, 1);
            bar.gameObject.AddComponent<WindowDrag>().Window = w;
            w.titleBg = bar;

            w.titleText = UIKit.Label(bar.transform, title, 15, Theme.TextLight, TextAnchor.MiddleLeft);
            UIKit.Stretch(w.titleText.rectTransform, 12, 0, 100, 0);
            w.titleText.textWrappingMode = TextWrappingModes.NoWrap;
            w.titleText.overflowMode = TextOverflowModes.Ellipsis;

            var close = UIKit.Button(bar.transform, "×", w.Close, Theme.Danger, Color.white, 18);
            UIKit.Place((RectTransform)close.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-2, 0), new Vector2(44, 30));
            var min = UIKit.Button(bar.transform, "－", w.Minimize, new Color(0.30f, 0.32f, 0.36f), Color.white, 16);
            UIKit.Place((RectTransform)min.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-48, 0), new Vector2(44, 30));

            var content = UIKit.Panel(rt, Theme.Window, "Content");
            UIKit.Stretch(content.rectTransform, 1, 35, 1, 1);
            w.Content = content.rectTransform;
            return w;
        }

        public void SetTitle(string t)
        {
            if (Title == t) return;
            Title = t;
            titleText.text = t;
            Manager?.NotifyChanged();
        }

        public void SetActiveLook(bool active) => titleBg.color = active ? Theme.TitleBar : Theme.TitleBarInactive;

        public void OnPointerDown(PointerEventData e) => Manager?.Focus(this);

        public void Close() => Close(false);

        public void Close(bool silent)
        {
            if (closed) return;
            closed = true;
            if (!silent && OSRoot.I != null) OSRoot.I.PlaySound("window_close", 0.5f);
            Closed?.Invoke();
            Manager?.Remove(this);
            Destroy(gameObject);
        }

        public void Minimize()
        {
            gameObject.SetActive(false);
            Manager?.NotifyChanged();
        }

        public void Restore()
        {
            gameObject.SetActive(true);
            Manager?.Focus(this);
        }
    }

    /// <summary>タイトルバーをドラッグしてウィンドウを動かす</summary>
    public class WindowDrag : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public OSWindow Window;
        Canvas canvas;

        public void OnPointerDown(PointerEventData e) => Window.Manager?.Focus(Window);

        public void OnDrag(PointerEventData e)
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>().rootCanvas;
            var rt = Window.Rect;
            var p = rt.anchoredPosition + e.delta / canvas.scaleFactor;
            var half = ((RectTransform)rt.parent).rect.size * 0.5f;
            p.x = Mathf.Clamp(p.x, -half.x, half.x);
            p.y = Mathf.Clamp(p.y, -half.y + 60, half.y - rt.sizeDelta.y * 0.5f + 17);
            rt.anchoredPosition = p;
        }
    }
}
