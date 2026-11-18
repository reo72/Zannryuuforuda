using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    public static class TaskManagerApp
    {
        public static void Open()
        {
            OSRoot.I.Windows.Open("taskmgr", "タスクマネージャー", new Vector2(620, 440),
                w => w.gameObject.AddComponent<TaskManagerView>().Build(w));
        }
    }

    /// <summary>
    /// 実行中のプロセス一覧。yui.exe（説明欄「家族」）がいる＝伏線 #10。
    /// killFlag があるプロセスは終了でき、そのフラグをイベントで拾ってエンドにつなげる。
    /// </summary>
    public class TaskManagerView : MonoBehaviour
    {
        RectTransform list;
        GameManager gm;

        public void Build(OSWindow w)
        {
            gm = GameManager.I;
            var head = UIKit.Panel(w.Content, Theme.Toolbar, "Header");
            UIKit.Top(head.rectTransform, 34);
            var h1 = UIKit.Label(head.transform, "名前", 14, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(h1.rectTransform, 14, 0, 0, 0);
            var h2 = UIKit.Label(head.transform, "説明", 14, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(h2.rectTransform, 200, 0, 0, 0);
            var h3 = UIKit.Label(head.transform, "メモリ", 14, Theme.Text, TextAnchor.MiddleLeft);
            UIKit.Stretch(h3.rectTransform, 360, 0, 0, 0);

            var area = UIKit.Rect("ListArea", w.Content);
            UIKit.Stretch(area, 0, 34, 0, 0);
            list = UIKit.ScrollList(area, out var sr, 1, 6);
            UIKit.Stretch((RectTransform)sr.transform);
            Refresh();
        }

        void Refresh()
        {
            UIKit.Clear(list);
            foreach (var p in Content.Config.processes)
            {
                if (gm.State.day < Mathf.Max(1, p.minDay) || !gm.RequireOk(p.requireFlag)) continue;
                if (!string.IsNullOrEmpty(p.killFlag) && gm.HasFlag(p.killFlag)) continue;

                var row = UIKit.Panel(list, new Color(1, 1, 1, 0.6f), "Row");
                UIKit.Size(row, 38);
                var n = UIKit.Label(row.transform, p.name, 15, Theme.Text, TextAnchor.MiddleLeft);
                UIKit.Stretch(n.rectTransform, 8, 0, 0, 0);
                var d = UIKit.Label(row.transform, TextResolver.Resolve(p.desc), 15, Theme.Text, TextAnchor.MiddleLeft);
                UIKit.Stretch(d.rectTransform, 194, 0, 0, 0);
                var m = UIKit.Label(row.transform, TextResolver.Resolve(string.IsNullOrEmpty(p.memory) ? "12 MB" : p.memory), 15, Theme.Text, TextAnchor.MiddleLeft);
                UIKit.Stretch(m.rectTransform, 354, 0, 0, 0);

                var proc = p;
                var end = UIKit.Button(row.transform, "タスクの終了", () => Kill(proc), Theme.Button, Theme.Text, 13);
                UIKit.Place((RectTransform)end.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-6, 0), new Vector2(110, 30));
            }
        }

        void Kill(ProcessDef p)
        {
            bool tooLate = p.killMaxDay > 0 && gm.State.day > p.killMaxDay;
            if (string.IsNullOrEmpty(p.killFlag) || tooLate)
            {
                OSRoot.I.MessageBox("タスクマネージャー", $"{p.name} を終了できません。\nアクセスが拒否されました。");
                return;
            }
            gm.SetFlag(p.killFlag);
            Refresh();
        }
    }
}
