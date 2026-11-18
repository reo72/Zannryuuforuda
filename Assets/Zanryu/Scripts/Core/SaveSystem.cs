using System;
using System.IO;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// セーブは Application.persistentDataPath に JSON で保存する。
    /// （Windows: C:/Users/名前/AppData/LocalLow/会社名/製品名/）
    /// </summary>
    public static class SaveSystem
    {
        static string Dir => Application.persistentDataPath;
        static string StatePath(string profile) => Path.Combine(Dir, $"save_{profile}.json");
        static string GlobalPath => Path.Combine(Dir, "global.json");

        public static GameState LoadState(string profile)
        {
            var s = Read<GameState>(StatePath(profile));
            if (s != null) s.profile = profile;
            return s;
        }

        public static void SaveState(GameState s)
        {
            if (s == null) return;
            Write(StatePath(s.profile), JsonUtility.ToJson(s, true));
        }

        public static void DeleteState(string profile)
        {
            var p = StatePath(profile);
            if (File.Exists(p)) File.Delete(p);
        }

        const int GlobalVersion = 2;

        public static GlobalSave LoadGlobal()
        {
            var g = Read<GlobalSave>(GlobalPath) ?? new GlobalSave { version = GlobalVersion };
            if (g.version < GlobalVersion)
            {
                // 音量・文字の速さの項目がなかった頃のセーブは 0 になっているので既定値に戻す
                if (g.volume <= 0) g.volume = 1f;
                if (g.bgmVolume <= 0) g.bgmVolume = 0.7f;
                if (g.seVolume <= 0) g.seVolume = 0.8f;
                if (g.ambientVolume <= 0) g.ambientVolume = 0.8f;
                if (g.textSpeed == 0) g.textSpeed = 1;
                g.version = GlobalVersion;
                SaveGlobal(g);
            }
            return g;
        }
        public static void SaveGlobal(GlobalSave g) => Write(GlobalPath, JsonUtility.ToJson(g, true));

        /// <summary>全セーブ削除（エディタメニューとデバッグ用）</summary>
        public static void DeleteAll()
        {
            if (!Directory.Exists(Dir)) return;
            foreach (var f in Directory.GetFiles(Dir, "save_*.json")) File.Delete(f);
            if (File.Exists(GlobalPath)) File.Delete(GlobalPath);
        }

        static T Read<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
            catch (Exception e) { Debug.LogWarning($"[Save] 読み込み失敗 {path}: {e.Message}"); return null; }
        }

        static void Write(string path, string json)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e) { Debug.LogWarning($"[Save] 書き込み失敗 {path}: {e.Message}"); }
        }
    }
}
