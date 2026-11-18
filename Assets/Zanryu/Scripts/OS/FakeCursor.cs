using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// 本物のマウスカーソルを隠して、代わりに画像のカーソルを表示する。
    /// これで「カーソルが勝手に動く」演出ができる（本物のカーソルは OS の都合で動かせないため）。
    /// 好きなカーソル画像を Resources/Icons/cursor.png に置けばそれを使う。
    /// </summary>
    public class FakeCursor : MonoBehaviour
    {
        RectTransform layer, cursor;
        Image blocker;
        bool hijacked;
        public bool Enabled { get; set; }

        public void Init(RectTransform l, bool enabled)
        {
            layer = l;
            Enabled = enabled;

            // 乗っ取り中はクリックを受け付けないための透明な板
            blocker = UIKit.Panel(layer, new Color(0, 0, 0, 0), "InputBlocker");
            UIKit.Stretch(blocker.rectTransform);
            blocker.gameObject.SetActive(false);

            var img = UIKit.Panel(layer, Color.white, "Cursor");
            img.raycastTarget = false;
            var custom = UIKit.LoadSprite("Icons/cursor");
            img.sprite = custom != null ? custom : CreateArrowSprite();
            img.SetNativeSize();
            cursor = img.rectTransform;
            cursor.anchorMin = cursor.anchorMax = new Vector2(0.5f, 0.5f);
            cursor.pivot = new Vector2(0, 1);
            cursor.localScale = Vector3.one * 1.2f;
        }

        void Update()
        {
            if (!Enabled)
            {
                if (cursor.gameObject.activeSelf) cursor.gameObject.SetActive(false);
                Cursor.visible = true;
                return;
            }
            Cursor.visible = false;
            if (!cursor.gameObject.activeSelf) cursor.gameObject.SetActive(true);
            if (hijacked) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, InputCompat.MousePosition, null, out var p);
            cursor.anchoredPosition = p;
        }

        void OnDisable() => Cursor.visible = true;

        /// <summary>カーソルを target まで勝手に動かす（target が null なら画面中央）</summary>
        public IEnumerator Hijack(RectTransform target, float duration)
        {
            hijacked = true;
            blocker.gameObject.SetActive(true);
            blocker.transform.SetAsLastSibling();
            cursor.SetAsLastSibling();

            Vector2 from = cursor.anchoredPosition;
            Vector2 to = Vector2.zero;
            if (target != null)
            {
                var world = target.TransformPoint(target.rect.center);
                to = layer.InverseTransformPoint(world);
            }
            duration = Mathf.Max(0.2f, duration);

            yield return new WaitForSeconds(0.3f); // 一瞬止まってから動き出す
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duration);
                var wobble = new Vector2(Mathf.Sin(t * 13f), Mathf.Cos(t * 11f)) * 3f * (1 - k);
                cursor.anchoredPosition = Vector2.Lerp(from, to, k) + wobble;
                yield return null;
            }
            cursor.anchoredPosition = to;
            yield return new WaitForSeconds(0.5f);

            blocker.gameObject.SetActive(false);
            hijacked = false;
        }

        /// <summary>画像がなくても動くように、矢印カーソルをコードで描く</summary>
        static Sprite CreateArrowSprite()
        {
            const int w = 14, h = 22;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            bool Inside(int x, int y)
            {
                // y は上から数える。三角形 (0,0)-(0,17)-(12,13) と軸部分
                if (x < 0 || y < 0 || x >= w || y >= h) return false;
                bool tri = x <= y * 0.75f && x <= 12 && y <= 17 && (y <= 13 || x <= (17 - y) * 3);
                bool tail = y >= 12 && y <= 20 && x >= (y - 12) * 0.6f + 4 && x <= (y - 12) * 0.6f + 7;
                return tri || tail;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = new Color(0, 0, 0, 0);
                if (Inside(x, y))
                {
                    bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
                    c = edge ? Color.black : Color.white;
                }
                tex.SetPixel(x, h - 1 - y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 1), 100);
        }
    }
}
