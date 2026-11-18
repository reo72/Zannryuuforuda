using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>
    /// 検索ソフト「Lumen Search」（Google のようなもの）。
    ///  ・ホーム：検索窓・急上昇ワード・ニュース・最近の検索
    ///  ・キーワードで検索 → web.json の searches に一致したページが並ぶ（なければ辞書・Q&A の一般的な結果）
    ///  ・ページはサイトごとの見た目（新聞・ブログ・掲示板・SNS・フリマ…）で表示。広告やコメント欄もある
    ///  ・ページを初めて開くと「read_ページID」フラグが立つ（ヒント・メモ・演出の条件に使える）
    /// </summary>
    public class BrowserApp : MonoBehaviour
    {
        public static void Open(string pageId = null)
        {
            var w = OSRoot.I.Windows.Open("browser", "Lumen Search", new Vector2(1040, 780),
                win => win.gameObject.AddComponent<BrowserApp>().Build(win));
            if (pageId != null && w.TryGetComponent<BrowserApp>(out var b)) b.ShowPage(pageId, null);
        }

        static readonly List<string> recent = new List<string>(); // 最近の検索（起動中だけ）
        static readonly Color Link = new Color(0.16f, 0.36f, 0.70f);
        static readonly Color Visited = new Color(0.42f, 0.24f, 0.60f);
        static readonly Color UrlGreen = new Color(0.20f, 0.50f, 0.30f);
        static readonly Color Dim = new Color(0.42f, 0.43f, 0.46f);
        const float PageWidth = 1040 - 2 - 56;

        OSWindow win;
        RectTransform content;
        ScrollRect scroll;
        TMP_Text address;
        GameManager gm;
        readonly Stack<Action> history = new Stack<Action>();
        Action current;

        void Build(OSWindow w)
        {
            win = w;
            gm = GameManager.I;

            var bar = UIKit.Panel(w.Content, Theme.Toolbar, "Toolbar");
            UIKit.Top(bar.rectTransform, 48);
            var back = UIKit.Button(bar.transform, "←", Back, Theme.Button, Theme.Text, 20);
            UIKit.Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(44, 34));
            var home = UIKit.Button(bar.transform, "ホーム", () => Navigate(RenderHome), Theme.Button, Theme.Text, 14);
            UIKit.Place((RectTransform)home.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(58, 0), new Vector2(70, 34));
            var addrBg = UIKit.Panel(bar.transform, Color.white, "Address");
            UIKit.Stretch(addrBg.rectTransform, 138, 7, 12, 7);
            address = UIKit.Label(addrBg.transform, "", 15, Dim, TextAnchor.MiddleLeft);
            UIKit.Stretch(address.rectTransform, 12, 0, 12, 0);
            address.textWrappingMode = TextWrappingModes.NoWrap;
            address.overflowMode = TextOverflowModes.Ellipsis;

            var area = UIKit.Rect("Page", w.Content);
            UIKit.Stretch(area, 0, 48, 0, 0);
            area.gameObject.AddComponent<Image>().color = Color.white;
            content = UIKit.ScrollList(area, out scroll, 10, 28);

            Navigate(RenderHome, false);
        }

        // ================= 画面の移動 =================

        void Navigate(Action view, bool push = true)
        {
            if (push && current != null) history.Push(current);
            current = view;
            Redraw();
        }

        void Back()
        {
            if (history.Count == 0) return;
            current = history.Pop();
            Redraw();
        }

        void Redraw()
        {
            UIKit.Clear(content);
            current();
            scroll.verticalNormalizedPosition = 1;
        }

        public void ShowPage(string id, string query) => Navigate(() => RenderPage(id, query));

        void Search(string query)
        {
            query = (query ?? "").Trim();
            if (query.Length == 0) return;
            recent.Remove(query);
            recent.Insert(0, query);
            if (recent.Count > 6) recent.RemoveAt(recent.Count - 1);
            Navigate(() => RenderResults(query));
        }

        static string Fill(string s, string query) =>
            TextResolver.Resolve(s ?? "").Replace("{QUERY}", query ?? "");

        // ================= ホーム =================

        void RenderHome()
        {
            address.text = "https://lumen-search.example/";
            win.SetTitle("Lumen Search");

            Spacer(24);
            var logo = UIKit.Label(content, "Lumen Search", 46, Theme.Accent, TextAnchor.MiddleCenter);
            UIKit.Size(logo, 70);
            SearchBox("");

            if (Content.Web.trending.Count > 0)
            {
                Spacer(8);
                var t = UIKit.Label(content, "急上昇ワード", 15, Dim);
                var grid = UIKit.Rect("Trending", content);
                var g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(230, 34);
                g.spacing = new Vector2(8, 6);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 4;
                int rank = 1;
                foreach (var word in Content.Web.trending)
                {
                    var w = TextResolver.Resolve(word);
                    var chip = UIKit.Button(grid, $"{rank++}  {w}", () => Search(w), new Color(0.94f, 0.95f, 0.97f), Theme.Text, 14);
                    chip.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
                }
            }

            Spacer(14);
            Heading("ニュース");
            var feed = Content.Web.pages.Where(p => p.tags.Contains("feed") && Visible(p))
                                        .OrderByDescending(p => p.date).ToList();
            foreach (var p in feed) ResultBlock(p, null, false);

            if (recent.Count > 0)
            {
                Spacer(10);
                Heading("最近の検索");
                foreach (var q in recent)
                {
                    var qq = q;
                    LinkText("・" + q, () => Search(qq), 17);
                }
            }
        }

        // ================= 検索結果 =================

        void RenderResults(string query)
        {
            address.text = "https://lumen-search.example/search?q=" + query;
            win.SetTitle(query + " - Lumen Search");
            SearchBox(query);

            var results = FindResults(query);
            bool fallback = results.Count == 0;
            if (fallback)
                results = Content.Web.fallback.Select(Content.GetPage).Where(p => p != null).ToList();

            // 本物らしく「約 ○○ 件（0.3 秒）」と出す（件数は検索語から決まる）
            int seed = Math.Abs(query.GetHashCode());
            long fake = fallback ? 1200 + seed % 90000 : results.Count * 1000 + seed % 9000;
            var info = UIKit.Label(content, $"約 {fake:N0} 件（0.{2 + seed % 6} 秒）", 14, Dim);
            UIKit.Size(info, 26);

            if (fallback)
            {
                var none = UIKit.Label(content, $"「{query}」について、くわしい情報は見つかりませんでした。\n日記やメモ、メッセージに出てくる名前・数字・ことばで調べてみましょう。", 15, Dim);
                none.lineSpacing = 6;
                Spacer(6);
            }
            foreach (var p in results) ResultBlock(p, query, true);
        }

        List<WebPage> FindResults(string query)
        {
            var q = Norm(query);
            var found = new List<WebPage>();
            if (q.Length == 0) return found;
            int day = gm.State.day;
            foreach (var s in Content.Web.searches)
            {
                if (s.minDay > 0 && day < s.minDay) continue;
                if (!gm.RequireOk(s.requireFlag)) continue;
                bool hit = s.keywords.Any(kw =>
                {
                    var k = Norm(TextResolver.Resolve(kw));
                    return k.Length > 0 && (q == k || (k.Length >= 2 && q.Contains(k)));
                });
                if (!hit) continue;
                foreach (var id in s.results)
                {
                    var p = Content.GetPage(id);
                    if (p != null && Visible(p) && !found.Contains(p)) found.Add(p);
                }
            }
            return found;
        }

        /// <summary>全角・半角、大文字・小文字、空白の違いを無視して比べるための正規化</summary>
        static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            var sb = new StringBuilder(s.Length);
            foreach (var c in s) if (!char.IsWhiteSpace(c)) sb.Append(c);
            return sb.ToString();
        }

        bool Visible(WebPage p)
        {
            int day = gm.State.day;
            if (p.minDay > 0 && day < p.minDay) return false;
            if (p.maxDay > 0 && day > p.maxDay) return false;
            if (gm.EndingCount < p.minEndings) return false;
            return gm.RequireOk(p.requireFlag) && gm.ForbidOk(p.forbidFlag);
        }

        // ================= ページ =================

        struct SiteLook { public Color bar, barText, accent; public string nav; }

        static SiteLook LookOf(string style)
        {
            switch (style)
            {
                case "news": return new SiteLook { bar = new Color(0.09f, 0.15f, 0.27f), barText = Color.white, accent = new Color(0.75f, 0.15f, 0.15f), nav = "トップ　　社会　　地域　　政治　　経済　　スポーツ　　エンタメ　　天気" };
                case "tech": return new SiteLook { bar = new Color(0.11f, 0.11f, 0.13f), barText = new Color(1f, 0.62f, 0.20f), accent = new Color(1f, 0.55f, 0.15f), nav = "ニュース　　レビュー　　インタビュー　　ガジェット　　ランキング" };
                case "blog": return new SiteLook { bar = new Color(0.93f, 0.90f, 0.85f), barText = new Color(0.30f, 0.26f, 0.22f), accent = new Color(0.55f, 0.45f, 0.35f), nav = "記事一覧　　プロフィール" };
                case "bbs": return new SiteLook { bar = new Color(0.80f, 0.86f, 0.74f), barText = new Color(0.10f, 0.30f, 0.10f), accent = new Color(0.10f, 0.45f, 0.10f), nav = "板一覧　　新着スレッド　　検索" };
                case "sns": return new SiteLook { bar = new Color(0.18f, 0.52f, 0.85f), barText = Color.white, accent = new Color(0.18f, 0.52f, 0.85f), nav = "ホーム　　話題　　通知　　メッセージ" };
                case "shop": return new SiteLook { bar = new Color(0.88f, 0.28f, 0.28f), barText = Color.white, accent = new Color(0.88f, 0.28f, 0.28f), nav = "さがす　　出品する　　お知らせ　　マイページ" };
                case "qa": return new SiteLook { bar = new Color(0.98f, 0.78f, 0.25f), barText = new Color(0.25f, 0.18f, 0.05f), accent = new Color(0.85f, 0.55f, 0.05f), nav = "質問する　　カテゴリ　　ランキング" };
                case "corp": return new SiteLook { bar = new Color(0.20f, 0.33f, 0.52f), barText = Color.white, accent = new Color(0.20f, 0.33f, 0.52f), nav = "製品　　サポート　　会社情報　　（このサイトはアーカイブです）" };
                case "weather": return new SiteLook { bar = new Color(0.22f, 0.55f, 0.78f), barText = Color.white, accent = new Color(0.22f, 0.55f, 0.78f), nav = "今日　　明日　　週間　　雨雲レーダー" };
                case "dict": return new SiteLook { bar = new Color(0.28f, 0.28f, 0.30f), barText = Color.white, accent = new Color(0.28f, 0.28f, 0.30f), nav = "国語　　英和　　類語" };
                default: return new SiteLook { bar = new Color(0.90f, 0.90f, 0.92f), barText = Theme.Text, accent = Theme.Accent, nav = "" };
            }
        }

        void RenderPage(string id, string query)
        {
            var p = Content.GetPage(id);
            if (p == null || !Visible(p))
            {
                address.text = "https://lumen-search.example/404";
                var t = UIKit.Label(content, "404\nページが見つかりません。", 24, Theme.Text, TextAnchor.MiddleCenter);
                UIKit.Size(t, 200);
                return;
            }
            var look = LookOf(p.style);
            address.text = "https://" + Fill(p.url, query);
            win.SetTitle(Fill(p.title, query) + " - " + Fill(p.site, query));

            // サイトのヘッダー（ロゴの帯とメニュー）
            var header = UIKit.Panel(content, look.bar, "SiteHeader");
            UIKit.Size(header, 64);
            var siteName = UIKit.Label(header.transform, Fill(p.site, query), 26, look.barText, TextAnchor.MiddleLeft);
            UIKit.Stretch(siteName.rectTransform, 18, 0, 18, 0);
            siteName.fontStyle = FontStyles.Bold;
            if (!string.IsNullOrEmpty(look.nav))
            {
                var nav = UIKit.Label(content, look.nav, 14, Dim);
                nav.textWrappingMode = TextWrappingModes.NoWrap;
                nav.overflowMode = TextOverflowModes.Ellipsis;
                var rule = UIKit.Panel(content, look.accent, "NavRule");
                UIKit.Size(rule, 2);
            }

            // パンくず・タイトル・日付
            if (!string.IsNullOrEmpty(p.category))
                UIKit.Label(content, "ホーム  ›  " + p.category, 13, Dim);
            var title = UIKit.Label(content, Fill(p.title, query), 30, Theme.Text);
            title.fontStyle = FontStyles.Bold;
            var meta = new List<string>();
            if (!string.IsNullOrEmpty(p.date)) meta.Add(p.date);
            if (!string.IsNullOrEmpty(p.author)) meta.Add(Fill(p.author, query));
            if (meta.Count > 0) UIKit.Label(content, string.Join("　｜　", meta), 14, Dim);

            // 写真
            var photo = UIKit.LoadSprite("Images/" + p.asset);
            if (photo != null)
            {
                var img = UIKit.Panel(content, Color.white, "Photo");
                img.sprite = photo;
                img.preserveAspect = true;
                UIKit.Size(img, Mathf.Min(440, PageWidth * photo.rect.height / photo.rect.width));
                if (!string.IsNullOrEmpty(p.caption))
                    UIKit.Label(content, Fill(p.caption, query), 13, Dim, TextAnchor.UpperCenter);
            }

            // 本文
            var body = UIKit.Label(content, Fill(p.body, query), 18, Theme.Text);
            body.lineSpacing = 10;

            // 記事っぽい飾り（シェアボタン・広告）
            if (p.style == "news" || p.style == "tech")
            {
                var share = UIKit.Label(content, "この記事をシェア　［ つぶやく ］［ リンクをコピー ］", 13, Dim);
            }
            Ad(p.id);

            // 関連リンク
            var links = p.links.Select(Content.GetPage).Where(l => l != null && Visible(l)).ToList();
            if (links.Count > 0)
            {
                Spacer(6);
                Heading(p.style == "news" || p.style == "tech" ? "関連記事" : "関連リンク");
                foreach (var l in links)
                {
                    var lid = l.id;
                    LinkText(Fill(l.title, query), () => ShowPage(lid, query), 17);
                }
            }

            // コメント欄
            if (p.comments.Count > 0)
            {
                Spacer(6);
                Heading($"コメント（{p.comments.Count}）");
                int n = 1;
                foreach (var c in p.comments)
                {
                    var box = UIKit.Panel(content, new Color(0.96f, 0.96f, 0.97f), "Comment");
                    var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
                    v.padding = new RectOffset(14, 14, 8, 10);
                    v.childControlHeight = true;
                    v.childControlWidth = true;
                    v.childForceExpandHeight = false;
                    UIKit.Label(box.transform, $"{n++}. 名無しさん", 13, Dim);
                    UIKit.Label(box.transform, Fill(c, query), 16, Theme.Text);
                }
            }

            // 初めて読んだとき：フラグを立てて、主人公が独り言で整理する
            if (!gm.HasFlag("read_" + p.id))
            {
                gm.SetFlag("read_" + p.id);
                gm.SetFlag(p.setFlag);
                if (p.react.Count > 0) OSRoot.I.Monologue.SayLines(p.react);
            }
        }

        // ================= 部品 =================

        void SearchBox(string text)
        {
            var row = UIKit.Rect("SearchRow", content);
            UIKit.Size(row.gameObject.AddComponent<LayoutElement>(), 50);
            var input = UIKit.Input(row, "キーワードを入力", 19);
            UIKit.Stretch((RectTransform)input.transform, 80, 2, 120, 2);
            input.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.96f);
            input.text = text;
            input.onSubmit.AddListener(Search);
            var go = UIKit.Button(row, "検索", () => Search(input.text), Theme.Accent, Color.white, 17);
            var grt = (RectTransform)go.transform;
            grt.anchorMin = new Vector2(1, 0);
            grt.anchorMax = new Vector2(1, 1);
            grt.pivot = new Vector2(1, 0.5f);
            grt.offsetMin = new Vector2(-110, 2);
            grt.offsetMax = new Vector2(-6, -2);
        }

        void Ad(string seed)
        {
            var ads = Content.Web.ads;
            if (ads.Count == 0) return;
            var text = ads[Math.Abs((seed ?? "").GetHashCode()) % ads.Count];
            var box = UIKit.Panel(content, new Color(0.97f, 0.96f, 0.92f), "Ad");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 8, 10);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            UIKit.Label(box.transform, "広告", 11, Dim);
            UIKit.Label(box.transform, TextResolver.Resolve(text), 16, new Color(0.35f, 0.30f, 0.20f));
        }

        void Heading(string text)
        {
            var h = UIKit.Label(content, text, 20, Theme.Text);
            h.fontStyle = FontStyles.Bold;
            var line = UIKit.Panel(content, new Color(0, 0, 0, 0.12f), "Rule");
            UIKit.Size(line, 1);
        }

        void Spacer(float h) => UIKit.Size(UIKit.Rect("Space", content).gameObject.AddComponent<LayoutElement>(), h);

        void LinkText(string text, Action onClick, int size)
        {
            var row = UIKit.Panel(content, new Color(1, 1, 1, 0), "Link");
            var v = row.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            var t = UIKit.Label(row.transform, text, size, Link);
            t.fontStyle = FontStyles.Underline;
            var click = row.gameObject.AddComponent<ClickHandler>();
            click.Single = () => { OSRoot.I.PlaySound("click", 0.5f); onClick(); };
            click.Double = click.Single;
        }

        /// <summary>検索結果・ニュースの1件（サイト名・URL・タイトル・書き出し）</summary>
        void ResultBlock(WebPage p, string query, bool showUrl)
        {
            var block = UIKit.Panel(content, new Color(1, 1, 1, 0), "Result:" + p.id);
            var v = block.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.padding = new RectOffset(0, 0, 4, 10);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;

            if (showUrl)
            {
                var url = UIKit.Label(block.transform, Fill(p.site, query) + "  ›  " + Fill(p.url, query), 13, UrlGreen);
                url.textWrappingMode = TextWrappingModes.NoWrap;
                url.overflowMode = TextOverflowModes.Ellipsis;
            }
            else if (!string.IsNullOrEmpty(p.category))
            {
                UIKit.Label(block.transform, $"{p.category}　·　{Fill(p.site, query)}", 13, Dim);
            }
            bool read = gm.HasFlag("read_" + p.id);
            var t = UIKit.Label(block.transform, Fill(p.title, query), 21, read ? Visited : Link);
            t.fontStyle = FontStyles.Underline;
            var snippet = Fill(p.body, query).Replace("\n", " ");
            if (snippet.Length > 80) snippet = snippet.Substring(0, 80) + "…";
            UIKit.Label(block.transform, (string.IsNullOrEmpty(p.date) ? "" : p.date + " — ") + snippet, 15, Dim);

            var id = p.id;
            var click = block.gameObject.AddComponent<ClickHandler>();
            click.Single = () => { OSRoot.I.PlaySound("click", 0.5f); ShowPage(id, query); };
            click.Double = click.Single;
        }
    }
}
