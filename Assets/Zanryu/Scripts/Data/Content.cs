using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// Resources/Content/ の JSON を読み込んで保持する。
    /// シナリオを書き換えたいときは JSON だけ編集すればよい（コードの変更は不要）。
    /// </summary>
    public static class Content
    {
        public static List<FileEntry> Files = new List<FileEntry>();
        public static List<ChatThread> Threads = new List<ChatThread>();
        public static List<HorrorEvent> Events = new List<HorrorEvent>();
        public static GameConfig Config = new GameConfig();
        public static MonologueData Monologues = new MonologueData();
        public static WebData Web = new WebData();
        static readonly Dictionary<string, WebPage> pageById = new Dictionary<string, WebPage>();

        static readonly Dictionary<string, FileEntry> fileById = new Dictionary<string, FileEntry>();
        static readonly Dictionary<string, ChatThread> threadById = new Dictionary<string, ChatThread>();

        public static void Load()
        {
            Files = LoadJson<FileList>("Content/files")?.files ?? new List<FileEntry>();
            Threads = LoadJson<ChatList>("Content/chats")?.threads ?? new List<ChatThread>();
            Events = LoadJson<EventList>("Content/events")?.events ?? new List<HorrorEvent>();
            Config = LoadJson<GameConfig>("Content/config") ?? new GameConfig();
            Monologues = LoadJson<MonologueData>("Content/monologues") ?? new MonologueData();
            Web = LoadJson<WebData>("Content/web") ?? new WebData();
            Normalize();

            fileById.Clear();
            foreach (var f in Files)
            {
                if (string.IsNullOrEmpty(f.id)) { Debug.LogWarning("[Content] id のないファイルがあります: " + f.name); continue; }
                if (fileById.ContainsKey(f.id)) Debug.LogWarning("[Content] ファイルIDが重複しています: " + f.id);
                else fileById[f.id] = f;
            }
            pageById.Clear();
            foreach (var p in Web.pages) if (!string.IsNullOrEmpty(p.id)) pageById[p.id] = p;
            threadById.Clear();
            foreach (var t in Threads)
                if (!string.IsNullOrEmpty(t.id)) threadById[t.id] = t;

            Debug.Log($"[Content] 読み込み完了: ファイル {Files.Count} / チャット {Threads.Count} / イベント {Events.Count} / 独り言 {Monologues.intros.Count + Monologues.reactions.Count + Monologues.hints.Count}");
        }

        public static FileEntry GetFile(string id) =>
            id != null && fileById.TryGetValue(id, out var f) ? f : null;

        public static WebPage GetPage(string id) =>
            id != null && pageById.TryGetValue(id, out var p) ? p : null;

        public static ChatThread GetThread(string id) =>
            id != null && threadById.TryGetValue(id, out var t) ? t : null;

        static T LoadJson<T>(string path) where T : class
        {
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null) { Debug.LogError($"[Content] Resources/{path}.json が見つかりません"); return null; }
            try { return JsonUtility.FromJson<T>(ta.text); }
            catch (Exception e) { Debug.LogError($"[Content] {path}.json の書式エラー: {e.Message}"); return null; }
        }

        // JsonUtility は書かれていないリストを null にすることがあるので補う
        static void Normalize()
        {
            foreach (var f in Files) if (f.variants == null) f.variants = new List<FileVariant>();
            foreach (var t in Threads)
            {
                if (t.scripts == null) t.scripts = new List<ChatScript>();
                if (t.nodes == null) t.nodes = new List<ChatNode>();
                foreach (var n in t.nodes) if (n.choices == null) n.choices = new List<ChatChoice>();
            }
            foreach (var e in Events) if (e.actions == null) e.actions = new List<EventAction>();
            if (Web.pages == null) Web.pages = new List<WebPage>();
            if (Web.searches == null) Web.searches = new List<WebSearch>();
            if (Web.trending == null) Web.trending = new List<string>();
            if (Web.ads == null) Web.ads = new List<string>();
            if (Web.fallback == null) Web.fallback = new List<string>();
            foreach (var p in Web.pages)
            {
                if (p.tags == null) p.tags = new List<string>();
                if (p.links == null) p.links = new List<string>();
                if (p.react == null) p.react = new List<string>();
                if (p.comments == null) p.comments = new List<string>();
            }
            foreach (var s in Web.searches)
            {
                if (s.keywords == null) s.keywords = new List<string>();
                if (s.results == null) s.results = new List<string>();
            }

            var m = Monologues;
            if (m.intros == null) m.intros = new List<MonologueEntry>();
            if (m.reactions == null) m.reactions = new List<MonologueEntry>();
            if (m.hints == null) m.hints = new List<MonologueEntry>();
            foreach (var list in new[] { m.intros, m.reactions, m.hints })
                foreach (var e in list) if (e.lines == null) e.lines = new List<string>();

            var c = Config;
            if (c.bootLines == null) c.bootLines = new List<string>();
            if (c.accounts == null) c.accounts = new List<AccountDef>();
            if (c.shutdownMessages == null) c.shutdownMessages = new List<string>();
            if (c.processes == null) c.processes = new List<ProcessDef>();
            if (c.endings == null) c.endings = new List<EndingDef>();
            if (c.notes == null) c.notes = new List<NoteDef>();
            if (c.dayBgm == null) c.dayBgm = new List<string>();
            if (c.loginPasswords == null) c.loginPasswords = new List<LoginPassword>();
            if (c.wrongPasswordMessages == null) c.wrongPasswordMessages = new List<string>();
            if (c.secretPasswords == null) c.secretPasswords = new List<SecretPassword>();
            foreach (var sp in c.secretPasswords)
            {
                if (sp.passwords == null) sp.passwords = new List<string>();
                if (sp.requireEndings == null) sp.requireEndings = new List<string>();
            }
            foreach (var a in c.accounts)
            {
                if (a.unlockEndings == null) a.unlockEndings = new List<string>();
                if (a.showAfterEndings == null) a.showAfterEndings = new List<string>();
            }
        }
    }
}
