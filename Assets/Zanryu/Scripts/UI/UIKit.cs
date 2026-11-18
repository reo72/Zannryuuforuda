using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>色の一括管理。見た目を変えたいときはここを触る。</summary>
    public static class Theme
    {
        public static readonly Color Desktop = new Color(0.08f, 0.09f, 0.11f);
        public static readonly Color Taskbar = new Color(0.10f, 0.11f, 0.13f, 0.97f);
        public static readonly Color WindowFrame = new Color(0.16f, 0.17f, 0.19f);
        public static readonly Color Window = new Color(0.93f, 0.93f, 0.92f);
        public static readonly Color Toolbar = new Color(0.86f, 0.86f, 0.85f);
        public static readonly Color TitleBar = new Color(0.20f, 0.22f, 0.26f);
        public static readonly Color TitleBarInactive = new Color(0.38f, 0.39f, 0.42f);
        public static readonly Color Text = new Color(0.12f, 0.12f, 0.13f);
        public static readonly Color TextLight = new Color(0.92f, 0.92f, 0.92f);
        public static readonly Color TextDim = new Color(0.50f, 0.50f, 0.52f);
        public static readonly Color Accent = new Color(0.30f, 0.48f, 0.72f);
        public static readonly Color Button = new Color(0.80f, 0.80f, 0.79f);
        public static readonly Color Danger = new Color(0.70f, 0.25f, 0.25f);
        public static readonly Color Select = new Color(0.30f, 0.48f, 0.72f, 0.35f);
        public static readonly Color BubbleThem = new Color(1f, 1f, 1f);
        public static readonly Color BubbleMe = new Color(0.72f, 0.84f, 0.96f);
    }

    /// <summary>uGUI をコードから組み立てるための小さな道具箱（プレハブなしで動かすため）</summary>
    public static class UIKit
    {
        static TMP_FontAsset fontAsset;

        /// <summary>
        /// 文字は TextMeshPro（SDF）で描くので、どの解像度でもくっきり表示される。
        /// Resources/Fonts/Main.ttf（Noto Sans JP）から実行時にフォントアセットを作る。
        /// </summary>
        public static TMP_FontAsset FontAsset => fontAsset != null ? fontAsset : (fontAsset = LoadFont());

        static TMP_FontAsset LoadFont()
        {
            // エディタで作ったフォントアセットがあればそれを優先
            var asset = Resources.Load<TMP_FontAsset>("Fonts/MainSDF");
            if (asset != null) return asset;

            var src = Resources.Load<Font>("Fonts/Main");
            if (src != null)
            {
                var fa = TMP_FontAsset.CreateFontAsset(src, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (fa != null) { fa.name = "Main (runtime SDF)"; return fa; }
            }
            // フォントファイルがないときは OS の日本語フォントを使う
            foreach (var family in new[] { "Yu Gothic UI", "Meiryo", "Hiragino Sans", "Noto Sans CJK JP" })
            {
                try
                {
                    var fa = TMP_FontAsset.CreateFontAsset(family, "Regular");
                    if (fa != null) return fa;
                }
                catch { /* そのフォントがない環境 */ }
            }
            Debug.LogWarning("[UIKit] 日本語フォントが見つかりません。Resources/Fonts/Main.ttf を置いてください。");
            return TMP_Settings.defaultFontAsset;
        }

        public static TextAlignmentOptions Align(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                default: return TextAlignmentOptions.BottomRight;
            }
        }

        // ---------- RectTransform ----------

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>親いっぱいに広げる（l,t,r,b は内側への余白）</summary>
        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>親の上端に固定の高さで貼り付ける</summary>
        public static RectTransform Top(RectTransform rt, float height, float inset = 0, float yOffset = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(inset, -height - yOffset);
            rt.offsetMax = new Vector2(-inset, -yOffset);
            return rt;
        }

        /// <summary>親の下端に固定の高さで貼り付ける</summary>
        public static RectTransform Bottom(RectTransform rt, float height, float inset = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, height + inset);
            return rt;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        // ---------- 部品 ----------

        public static Image Panel(Transform parent, Color color, string name = "Panel")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static TMP_Text Label(Transform parent, string text, int size, Color color,
                                     TextAnchor anchor = TextAnchor.UpperLeft, string name = "Label")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = FontAsset;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = Align(anchor);
            t.lineSpacing = 6;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = false;
            return t;
        }

        public static UnityEngine.UI.Button Button(Transform parent, string label, Action onClick,
                                                   Color? bg = null, Color? fg = null, int size = 16)
        {
            var img = Panel(parent, bg ?? Theme.Button, "Button:" + label);
            var b = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colors.selectedColor = Color.white;
            b.colors = colors;
            b.onClick.AddListener(() =>
            {
                // 押したボタンが選択状態のまま残ると Space / Enter で再度押されてしまうので解除
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                if (OSRoot.I != null) OSRoot.I.PlaySound("click", 0.5f);
                onClick?.Invoke();
            });
            var t = Label(img.transform, label, size, fg ?? Theme.Text, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 6, 0, 6, 0);
            return b;
        }

        public static TMP_InputField Input(Transform parent, string placeholder, int size = 18)
        {
            var img = Panel(parent, Color.white, "Input");
            // 設定が終わってから有効にする（TMP_InputField は有効になった瞬間にカーソルなどを作るため）
            img.gameObject.SetActive(false);
            var field = img.gameObject.AddComponent<TMP_InputField>();
            var area = Stretch(Rect("TextArea", img.transform), 10, 2, 10, 2);
            area.gameObject.AddComponent<RectMask2D>();
            var text = Label(area, "", size, Theme.Text, TextAnchor.MiddleLeft, "Text");
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var ph = Label(area, placeholder, size, Theme.TextDim, TextAnchor.MiddleLeft, "Placeholder");
            Stretch(ph.rectTransform);
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = ph;
            field.fontAsset = FontAsset;
            field.pointSize = size;
            img.gameObject.SetActive(true);
            return field;
        }

        /// <summary>縦スクロールのリスト。戻り値は中身を入れる Content。</summary>
        public static RectTransform ScrollList(Transform parent, out ScrollRect scroll, float spacing = 2, int padding = 6)
        {
            var bg = Panel(parent, new Color(1, 1, 1, 0), "Scroll");
            Stretch(bg.rectTransform);
            scroll = bg.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var viewport = Stretch(Rect("Viewport", bg.transform));
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return content;
        }

        public static LayoutElement Size(Component c, float height = -1, float width = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) { le.preferredHeight = height; le.minHeight = height; }
            if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
            return le;
        }

        // ---------- 素材 ----------

        /// <summary>Resources から画像を読む。テクスチャ形式のままでも Sprite に変換する。</summary>
        public static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path) || path.EndsWith("/")) return null;
            // Unity 6 の2Dテンプレートは画像を「Multiple」で読み込み、パーツごとに自動分割することがある。
            // 分割されていても元の1枚として使えるよう、まずテクスチャとして読む。
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                var sprites = Resources.LoadAll<Sprite>(path);
                if (sprites.Length == 1 && sprites[0].rect.width >= tex.width && sprites[0].rect.height >= tex.height) return sprites[0];
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            }
            return Resources.Load<Sprite>(path);
        }

        /// <summary>ファイルのアイコン。Resources/Icons に画像がなければ色付きの仮アイコンを作る。</summary>
        public static RectTransform FileIcon(Transform parent, FileEntry f, float size)
        {
            string key = f.type == "app" ? f.app : f.type;
            var sprite = LoadSprite("Icons/" + (string.IsNullOrEmpty(f.icon) ? key : f.icon));
            var img = Panel(parent, Color.white, "Icon");
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                return img.rectTransform;
            }
            Color c; string mark;
            switch (key)
            {
                case "folder": c = new Color(0.86f, 0.70f, 0.32f); mark = "DIR"; break;
                case "text": c = new Color(0.88f, 0.88f, 0.86f); mark = "TXT"; break;
                case "image": c = new Color(0.42f, 0.60f, 0.84f); mark = "IMG"; break;
                case "audio": c = new Color(0.60f, 0.46f, 0.80f); mark = "SND"; break;
                case "confirm": c = new Color(0.74f, 0.34f, 0.34f); mark = "EXE"; break;
                case "explorer": c = new Color(0.40f, 0.62f, 0.66f); mark = "PC"; break;
                case "chat": c = new Color(0.38f, 0.68f, 0.48f); mark = "MSG"; break;
                case "taskmgr": c = new Color(0.52f, 0.52f, 0.58f); mark = "TSK"; break;
                case "browser": c = new Color(0.30f, 0.55f, 0.85f); mark = "WEB"; break;
                default: c = new Color(0.55f, 0.55f, 0.55f); mark = "?"; break;
            }
            if (f.icon == "trash") { c = new Color(0.58f, 0.62f, 0.64f); mark = "BIN"; }
            img.color = c;
            var t = Label(img.transform, mark, Mathf.RoundToInt(size * 0.28f), new Color(0.1f, 0.1f, 0.1f, 0.85f), TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return img.rectTransform;
        }
    }

    /// <summary>クリック・ダブルクリックを受け取るための汎用部品</summary>
    public class ClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public Action Single;
        public Action Double;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (e.clickCount >= 2) Double?.Invoke();
            else Single?.Invoke();
        }
    }
}
