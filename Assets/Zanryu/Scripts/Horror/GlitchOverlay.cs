using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// 画面が一瞬乱れる演出（シェーダー不要の簡易版）。
    /// あとで URP のポストエフェクトに置き換えてもよい。
    /// </summary>
    public class GlitchOverlay : MonoBehaviour
    {
        readonly List<Image> bars = new List<Image>();
        Coroutine running;

        static readonly Color[] Palette =
        {
            new Color(1, 1, 1), new Color(0.9f, 0.2f, 0.25f), new Color(0.3f, 0.9f, 0.95f), new Color(0, 0, 0)
        };

        public void Init(RectTransform layer)
        {
            for (int i = 0; i < 12; i++)
            {
                var img = UIKit.Panel(layer, Color.clear, "GlitchBar");
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                img.gameObject.SetActive(false);
                bars.Add(img);
            }
        }

        public void Play(float duration)
        {
            if (running != null) StopCoroutine(running);
            OSRoot.I.PlaySound("glitch", 0.8f);
            running = StartCoroutine(Run(duration));
        }

        IEnumerator Run(float duration)
        {
            var os = OSRoot.I;
            var shake = new[] { os.ScreenLayer, os.WindowLayer };
            for (float t = 0; t < duration; t += 0.035f)
            {
                foreach (var b in bars)
                {
                    bool on = Random.value < 0.5f;
                    b.gameObject.SetActive(on);
                    if (!on) continue;
                    var c = Palette[Random.Range(0, Palette.Length)];
                    c.a = Random.Range(0.25f, 0.8f);
                    b.color = c;
                    b.rectTransform.anchoredPosition = new Vector2(Random.Range(-40f, 40f), Random.Range(-540f, 540f));
                    b.rectTransform.sizeDelta = new Vector2(0, Random.Range(2f, 40f));
                }
                foreach (var s in shake) s.anchoredPosition = Random.insideUnitCircle * 10f;
                yield return new WaitForSeconds(0.035f);
            }
            foreach (var b in bars) b.gameObject.SetActive(false);
            foreach (var s in shake) s.anchoredPosition = Vector2.zero;
            running = null;
        }
    }
}
