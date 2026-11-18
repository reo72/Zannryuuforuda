using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// メタ演出：プレイヤー本人のPCのデスクトップにテキストファイルを作る。
    ///
    /// 安全のためのルール（Steam で出すときもこのまま守る）
    ///  ・作るのは新しいテキストファイルだけ。プレイヤーのファイルは読まない・変えない・消さない
    ///  ・同じ名前のファイルがすでにあれば作らない（上書きしない）
    ///  ・オプション（ログイン画面右下）で OFF にできる。OFF のときは何もしない
    ///  ・「消されたか」の確認は、ゲームが自分で作ったファイルの有無を見るだけ
    /// ※ エディタで再生しても本当に作られるので、テスト中に増えたファイルは手で消してOK
    /// </summary>
    public static class RealDesktop
    {
        /// <summary>デスクトップの場所（OneDrive に移されている場合もこれで正しい場所が取れる）</summary>
        public static string Folder
        {
            get
            {
                try { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); }
                catch { return null; }
            }
        }

        public static bool Enabled => GameManager.I != null && GameManager.I.Global.metaFiles;

        public static bool Create(string name, string content)
        {
            var gm = GameManager.I;
            if (!Enabled || string.IsNullOrEmpty(name)) return false;
            var dir = Folder;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;

            name = Sanitize(name);
            if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) name += ".txt";
            var path = Path.Combine(dir, name);
            if (File.Exists(path)) return false; // 上書きしない

            try
            {
                // Windows のメモ帳で文字化けしないよう、改行は CRLF・BOM 付き UTF-8
                File.WriteAllText(path, (content ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RealDesktop] 作成できませんでした: {e.Message}");
                return false;
            }

            var g = gm.Global;
            if (!g.realFiles.Contains(name)) g.realFiles.Add(name);
            g.noticedDeleted.Remove(name);
            SaveSystem.SaveGlobal(g);
            Debug.Log($"[RealDesktop] 作成: {path}");
            return true;
        }

        /// <summary>ゲームが作ったファイルのうち、プレイヤーが消したもの（まだ気づいていない分だけ）</summary>
        public static List<string> NewlyDeleted()
        {
            var result = new List<string>();
            var gm = GameManager.I;
            if (!Enabled) return result;
            var dir = Folder;
            if (string.IsNullOrEmpty(dir)) return result;
            var g = gm.Global;
            foreach (var name in g.realFiles)
            {
                if (g.noticedDeleted.Contains(name)) continue;
                if (!File.Exists(Path.Combine(dir, name))) result.Add(name);
            }
            if (result.Count > 0)
            {
                g.noticedDeleted.AddRange(result);
                SaveSystem.SaveGlobal(g);
            }
            return result;
        }

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "");
            return name.Trim();
        }
    }
}
