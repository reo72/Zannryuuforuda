using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    public static class ExplorerApp
    {
        public static void Open(string folderId)
        {
            var w = OSRoot.I.Windows.Open("explorer", "エクスプローラー", new Vector2(780, 540),
                win => win.gameObject.AddComponent<ExplorerView>().Build(win, folderId));
            if (w.TryGetComponent<ExplorerView>(out var view)) view.Navigate(folderId);
        }
    }

    /// <summary>フォルダの中身を一覧表示する。隠しファイル表示の切り替えあり（伏線用）。</summary>
    public class ExplorerView : MonoBehaviour
    {
        OSWindow win;
        RectTransform list;
        TMP_Text pathText, hiddenText;
        string current;
        bool showHidden;
        string selected;
        readonly Stack<string> history = new Stack<string>();
        readonly Dictionary<string, Image> rows = new Dictionary<string, Image>();
        GameManager gm;

        public void Build(OSWindow w, string start)
        {
            win = w;
            gm = GameManager.I;
            var c = w.Content;

            var bar = UIKit.Panel(c, Theme.Toolbar, "Toolbar");
            UIKit.Top(bar.rectTransform, 42);
            var back = UIKit.Button(bar.transform, "← 戻る", Back);
            UIKit.Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(90, 32));
            pathText = UIKit.Label(bar.transform, "", 15, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(pathText.rectTransform, 106, 0, 190, 0);
            var hidden = UIKit.Button(bar.transform, "", ToggleHidden, Theme.Button, Theme.Text, 14);
            UIKit.Place((RectTransform)hidden.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(176, 32));
            hiddenText = hidden.GetComponentInChildren<TMP_Text>();

            var area = UIKit.Rect("ListArea", c);
            UIKit.Stretch(area, 0, 42, 0, 0);
            list = UIKit.ScrollList(area, out var sr, 1, 6);
            UIKit.Stretch((RectTransform)sr.transform);

            gm.FilesChanged += Refresh;
            gm.FlagSet += OnFlag;
            Navigate(start);
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.FilesChanged -= Refresh;
            gm.FlagSet -= OnFlag;
        }

        void OnFlag(string _) => Refresh();

        public void Navigate(string id, bool push = true)
        {
            if (push && current != null && current != id) history.Push(current);
            current = id;
            selected = null;
            Refresh();
        }

        void Back()
        {
            if (history.Count > 0) Navigate(history.Pop(), false);
        }

        void ToggleHidden()
        {
            showHidden = !showHidden;
            Refresh();
        }

        void Refresh()
        {
            if (list == null) return;
            hiddenText.text = showHidden ? "隠しファイル: 表示" : "隠しファイル: 非表示";

            var folder = Content.GetFile(current);
            string title = folder != null ? gm.DisplayName(folder) : "PC";
            win.SetTitle(title + " - エクスプローラー");
            pathText.text = BuildPath(current);

            UIKit.Clear(list);
            rows.Clear();
            var items = gm.GetChildren(current, showHidden);
            if (items.Count == 0)
            {
                var empty = UIKit.Label(list, "このフォルダーは空です。", 15, Theme.TextDim, TextAnchor.MiddleCenter);
                UIKit.Size(empty, 60);
                return;
            }
            foreach (var f in items) AddRow(f);
        }

        void AddRow(FileEntry f)
        {
            var row = UIKit.Panel(list, f.id == selected ? Theme.Select : new Color(1, 1, 1, 0), "Row:" + f.id);
            rows[f.id] = row;
            UIKit.Size(row, 34);
            var icon = UIKit.FileIcon(row.transform, f, 26);
            UIKit.Place(icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(26, 26));

            var name = UIKit.Label(row.transform, gm.DisplayName(f), 16, f.hidden ? Theme.TextDim : Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(name.rectTransform, 44, 0, 170, 0);
            bool locked = !string.IsNullOrEmpty(f.password) && !gm.State.unlocked.Contains(f.id);
            var kind = UIKit.Label(row.transform, KindName(f) + (locked ? "（ロック）" : ""), 14, Theme.TextDim, TextAnchor.MiddleLeft);
            UIKit.Place(kind.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(160, 30));

            var file = f;
            var click = row.gameObject.AddComponent<ClickHandler>();
            // 一覧を作り直すとダブルクリックが効かなくなるので、色だけ変える
            click.Single = () => Select(file.id);
            click.Double = () => gm.OpenFile(file);
        }

        void Select(string id)
        {
            selected = id;
            foreach (var kv in rows)
                if (kv.Value != null) kv.Value.color = kv.Key == id ? Theme.Select : new Color(1, 1, 1, 0);
        }

        static string KindName(FileEntry f)
        {
            switch (f.type)
            {
                case "folder": return "フォルダー";
                case "text": return "テキスト";
                case "image": return "画像";
                case "audio": return "音声";
                case "confirm":
                case "app": return "アプリケーション";
                default: return "ファイル";
            }
        }

        string BuildPath(string id)
        {
            var parts = new List<string>();
            int guard = 0;
            while (!string.IsNullOrEmpty(id) && id != "root" && guard++ < 20)
            {
                var f = Content.GetFile(id);
                if (f == null) break;
                parts.Insert(0, gm.DisplayName(f));
                id = f.parent;
            }
            parts.Insert(0, "PC");
            return string.Join(" > ", parts);
        }
    }
}
