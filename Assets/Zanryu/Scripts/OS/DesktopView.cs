using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zanryu
{
    /// <summary>壁紙とデスクトップアイコン。parent が "desktop" のファイルがここに並ぶ。</summary>
    public class DesktopView : MonoBehaviour
    {
        Image wallpaper;
        TMP_Text wallpaperNote;
        RectTransform iconRoot;
        readonly Dictionary<string, RectTransform> icons = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Image> iconBgs = new Dictionary<string, Image>();
        string selected;
        GameManager gm;

        const float CellW = 104, CellH = 112;

        public void Build()
        {
            gm = GameManager.I;
            var rt = (RectTransform)transform;
            wallpaper = gameObject.AddComponent<Image>();
            var click = gameObject.AddComponent<ClickHandler>();
            click.Single = () => Select(null);

            // 壁紙素材がまだないときの仮表示
            wallpaperNote = UIKit.Label(rt, "", 14, new Color(1, 1, 1, 0.25f), TextAnchor.LowerRight);
            UIKit.Stretch(wallpaperNote.rectTransform, 0, 0, 20, 60);

            iconRoot = UIKit.Stretch(UIKit.Rect("Icons", rt), 14, 14, 14, 60);

            gm.FilesChanged += Refresh;
            gm.FlagSet += OnFlag;
            Refresh();
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.FilesChanged -= Refresh;
            gm.FlagSet -= OnFlag;
        }

        void OnFlag(string _) => Refresh();

        public RectTransform GetIcon(string fileId) =>
            fileId != null && icons.TryGetValue(fileId, out var r) && r != null ? r : null;

        public void Refresh()
        {
            if (iconRoot == null) return;

            // 壁紙
            var wp = string.IsNullOrEmpty(gm.State.wallpaper) ? Content.Config.defaultWallpaper : gm.State.wallpaper;
            var sp = UIKit.LoadSprite("Images/" + wp);
            if (sp != null)
            {
                wallpaper.sprite = sp;
                wallpaper.color = Color.white;
                wallpaperNote.text = "";
            }
            else
            {
                wallpaper.sprite = null;
                wallpaper.color = Theme.Desktop;
                wallpaperNote.text = $"[壁紙未設定: {wp}]";
            }

            // アイコン（縦に並べて、あふれたら次の列）
            UIKit.Clear(iconRoot);
            icons.Clear();
            iconBgs.Clear();
            var items = gm.GetChildren("desktop", false);
            const int perCol = 8; // 1080p 基準で1列8個
            for (int i = 0; i < items.Count; i++)
            {
                var f = items[i];
                int col = i / perCol, row = i % perCol;
                var cell = UIKit.Panel(iconRoot, new Color(1, 1, 1, 0), "Icon:" + f.id);
                UIKit.Place(cell.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(col * CellW, -row * CellH), new Vector2(CellW - 6, CellH - 6));

                var icon = UIKit.FileIcon(cell.transform, f, 56);
                UIKit.Place(icon, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(56, 56));

                var label = UIKit.Label(cell.transform, gm.DisplayName(f), 14, Color.white, TextAnchor.UpperCenter);
                UIKit.Stretch(label.rectTransform, 2, 66, 2, 0);
                // 壁紙の上でも読めるように黒いふちを付ける
                label.outlineWidth = 0.2f;
                label.outlineColor = new Color32(0, 0, 0, 230);

                var file = f;
                var click = cell.gameObject.AddComponent<ClickHandler>();
                click.Single = () => Select(file.id);
                click.Double = () => { Select(file.id); gm.OpenFile(file); };

                icons[f.id] = cell.rectTransform;
                iconBgs[f.id] = cell;
            }
            Select(selected);
        }

        void Select(string id)
        {
            selected = id;
            foreach (var kv in iconBgs)
                if (kv.Value != null) kv.Value.color = kv.Key == id ? Theme.Select : new Color(1, 1, 1, 0);
        }
    }
}
