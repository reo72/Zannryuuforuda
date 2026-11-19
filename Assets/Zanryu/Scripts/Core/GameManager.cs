using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// ゲーム全体の進行役。日数・フラグ・ファイル状態・エンド分岐を管理する。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameState State;
        public GlobalSave Global;
        public float DayTime;           // その日の経過秒（ゲーム内時計とタイマーイベントに使う）
        public bool InDesktop;          // デスクトップ操作中か
        public string AccountName = "";

        public HorrorEventSystem Horror { get; private set; }
        public ChatSystem Chat { get; private set; }
        public MonologueSystem Mono { get; private set; }

        public event Action FilesChanged;
        public event Action<string> FlagSet;
        public event Action<FileEntry> FileOpened;
        public event Action DayStarted;

        bool endingRunning;

        public void Init()
        {
            I = this;
            Global = SaveSystem.LoadGlobal();
            State = SaveSystem.LoadState("main") ?? new GameState();
            Horror = gameObject.AddComponent<HorrorEventSystem>();
            Horror.Init(this);
            Chat = gameObject.AddComponent<ChatSystem>();
            Chat.Init(this);
            Mono = gameObject.AddComponent<MonologueSystem>();
            Mono.Init(this);
        }

        void Update()
        {
            if (InDesktop) DayTime += Time.deltaTime;
        }

        public void Save() => SaveSystem.SaveState(State);

        // ================= フラグ =================

        public bool HasFlag(string f) => !string.IsNullOrEmpty(f) && State.flags.Contains(f);

        /// <summary>空なら true。カンマ区切りは全部必要。</summary>
        public bool RequireOk(string flags)
        {
            if (string.IsNullOrEmpty(flags)) return true;
            foreach (var f in flags.Split(',')) if (!HasFlag(f.Trim())) return false;
            return true;
        }

        /// <summary>空なら true。カンマ区切りのどれか1つでもあれば false。</summary>
        public bool ForbidOk(string flags)
        {
            if (string.IsNullOrEmpty(flags)) return true;
            foreach (var f in flags.Split(',')) if (HasFlag(f.Trim())) return false;
            return true;
        }

        public void SetFlag(string f)
        {
            if (string.IsNullOrEmpty(f) || State.flags.Contains(f)) return;
            State.flags.Add(f);
            Save();
            Debug.Log($"[Flag] {f}");
            FlagSet?.Invoke(f);

            // 物語の手がかりを見つけたら「わかったこと」メモに追記したことを知らせる
            if (InDesktop && Content.Config.notes.Any(n => n.requireFlag == f && !string.IsNullOrEmpty(n.text)))
                OSRoot.I.Toast("メモ（わかったこと）\n新しいことを書きとめた");
        }

        /// <summary>例: CountFlags("pretend_") → ミサキのふりをした回数</summary>
        public int CountFlags(string prefix) => State.flags.Count(x => x.StartsWith(prefix, StringComparison.Ordinal));

        public int EndingCount => Global.endings.Count;
        public bool HasSecret(string id) => Global.secrets.Contains(id);

        public void AddSecret(string id)
        {
            if (string.IsNullOrEmpty(id) || Global.secrets.Contains(id)) return;
            Global.secrets.Add(id);
            SaveSystem.SaveGlobal(Global);
            Debug.Log($"[Secret] {id}");
        }

        /// <summary>メインの進行が何日目か（ログイン画面用。いま別のアカウントの状態を持っていても正しく返す）</summary>
        public int MainDay => State.profile == "main" ? State.day : (SaveSystem.LoadState("main")?.day ?? 1);
        public bool HasEnding(string id) => Global.endings.Contains(id);
        public bool HasEndings(List<string> ids) => ids == null || ids.All(HasEnding);

        public bool ProfileOk(string profile)
        {
            var p = string.IsNullOrEmpty(profile) ? "main" : profile;
            return p == "any" || p == State.profile;
        }

        // ================= ファイル =================

        public bool IsAvailable(FileEntry f)
        {
            if (f == null || !ProfileOk(f.profile)) return false;
            int min = Mathf.Max(1, f.minDay);
            int max = f.maxDay <= 0 ? int.MaxValue : f.maxDay;
            if (State.day < min || State.day > max) return false;
            if (EndingCount < f.minEndings) return false;
            if (!string.IsNullOrEmpty(f.requireSecret) && !HasSecret(f.requireSecret)) return false;
            return RequireOk(f.requireFlag) && ForbidOk(f.forbidFlag);
        }

        public List<FileEntry> GetChildren(string parentId, bool showHidden) =>
            Content.Files.Where(f => f.parent == parentId && IsAvailable(f) && (showHidden || !f.hidden)).ToList();

        FileVariant CurrentVariant(FileEntry f)
        {
            FileVariant best = null;
            foreach (var v in f.variants)
            {
                if (v.fromDay > State.day || v.minEndings > EndingCount || !RequireOk(v.requireFlag)) continue;
                if (best == null || v.fromDay >= best.fromDay) best = v;
            }
            return best;
        }

        /// <summary>トークン置換前の表示名</summary>
        public string RawDisplayName(FileEntry f)
        {
            var o = State.nameOverrides.FirstOrDefault(p => p.key == f.id);
            if (o != null) return o.value;
            var v = CurrentVariant(f);
            return v != null && !string.IsNullOrEmpty(v.name) ? v.name : f.name;
        }

        public string DisplayName(FileEntry f) => TextResolver.Resolve(RawDisplayName(f));

        public string GetContent(FileEntry f)
        {
            var v = CurrentVariant(f);
            return TextResolver.Resolve(v != null && !string.IsNullOrEmpty(v.content) ? v.content : f.content);
        }

        public string GetAsset(FileEntry f)
        {
            var v = CurrentVariant(f);
            return v != null && !string.IsNullOrEmpty(v.asset) ? v.asset : f.asset;
        }

        public void Rename(string fileId, string newName)
        {
            var o = State.nameOverrides.FirstOrDefault(p => p.key == fileId);
            if (o == null) State.nameOverrides.Add(new StringPair { key = fileId, value = newName });
            else o.value = newName;
            Save();
            FilesChanged?.Invoke();
        }

        public void SetWallpaper(string asset)
        {
            State.wallpaper = asset;
            Save();
            FilesChanged?.Invoke();
        }

        public void NotifyFilesChanged() => FilesChanged?.Invoke();

        public void OpenFile(FileEntry f)
        {
            if (f == null) return;
            if (!string.IsNullOrEmpty(f.password) && !State.unlocked.Contains(f.id))
            {
                Apps.OpenPassword(f);
                return;
            }
            State.openLogs.Add(new OpenLog { day = State.day, fileId = f.id, time = DayTime });
            Save();
            Apps.Open(f);
            SetFlag("opened_" + f.id); // ヒントの条件に使う（例：opened_diary_1）
            if (f.type != "confirm") SetFlag(f.setFlag);
            FileOpened?.Invoke(f);
        }

        public void Unlock(FileEntry f)
        {
            if (f == null) return;
            if (!State.unlocked.Contains(f.id)) State.unlocked.Add(f.id);
            Save();
            SetFlag("unlocked_" + f.id);
        }

        // ================= 時計 =================

        static int ParseMinutes(string hhmm)
        {
            if (string.IsNullOrEmpty(hhmm)) return 0;
            var p = hhmm.Split(':');
            int.TryParse(p[0], out int h);
            int m = 0;
            if (p.Length > 1) int.TryParse(p[1], out m);
            return h * 60 + m;
        }

        /// <summary>ゲーム内時刻。freezeTime（23:47）の1分だけ長く止まる＝伏線 #4</summary>
        public string ClockText()
        {
            var c = Content.Config;
            int start = ParseMinutes(c.startTime);
            int freeze = ParseMinutes(c.freezeTime) - start;
            if (freeze < 0) freeze += 1440;
            float m = DayTime / Mathf.Max(0.1f, c.secondsPerMinute);
            if (m >= freeze)
            {
                if (m < freeze + c.freezeLength) m = freeze;
                else m -= c.freezeLength;
            }
            int total = (start + (int)m) % 1440;
            return $"{total / 60:00}:{total % 60:00}";
        }

        public string DateText()
        {
            var c = Content.Config;
            return c.dateFormat.Replace("{M}", c.startMonth.ToString())
                               .Replace("{D}", (c.startDay + State.day - 1).ToString());
        }

        // ================= 1日の流れ =================

        public void LoginAs(AccountDef a)
        {
            AccountName = TextResolver.Resolve(a.name);
            var profile = string.IsNullOrEmpty(a.profile) ? "main" : a.profile;
            if (State == null || State.profile != profile)
            {
                Save();
                State = SaveSystem.LoadState(profile) ?? new GameState { profile = profile };
            }
            OSRoot.I.ShowDesktop();
            StartDay();
        }

        void StartDay()
        {
            DayTime = 0;
            InDesktop = true;
            // 前の日に返事しなかった選択肢は「無視した」扱いで消す
            State.waiting.RemoveAll(w => w.day < State.day);
            Save();
            OSRoot.I.PlayAmbient(Content.Config.ambient, 0.6f);
            OSRoot.I.PlayBgm(DayBgm(), 3f);
            Debug.Log($"[Day] {State.profile} / Day {State.day} 開始");
            DayStarted?.Invoke();
        }

        string DayBgm()
        {
            var c = Content.Config;
            if (State.profile == "yui") return c.yuiBgm;
            if (State.profile != "main") return null;
            int i = State.day - 1;
            return i >= 0 && i < c.dayBgm.Count ? c.dayBgm[i] : null;
        }

        void StopDayRoutines()
        {
            InDesktop = false;
            Horror.StopAll();
            Chat.StopAll();
            Mono.StopAll();
            OSRoot.I.PlayAmbient(null, 0);
        }

        public void RequestShutdown()
        {
            if (!InDesktop || endingRunning) return;
            // 演出：決められた日の最初のシャットダウンは、PC に拒否される
            if (State.profile == "main" && State.day == Content.Config.refuseShutdownDay && !HasFlag("shutdown_refused"))
            {
                SetFlag("shutdown_refused");
                return;
            }
            StartCoroutine(ShutdownRoutine());
        }

        IEnumerator ShutdownRoutine()
        {
            StopDayRoutines();
            string msg = "";
            var msgs = Content.Config.shutdownMessages;
            if (State.profile == "main" && msgs.Count > 0)
                msg = TextResolver.Resolve(msgs[Mathf.Clamp(State.day - 1, 0, msgs.Count - 1)]);

            yield return OSRoot.I.ShowShutdown(msg);

            if (State.profile == "main")
            {
                if (State.day >= Content.Config.lastDay)
                {
                    TriggerEnding(EndingResolver.ResolveFinalDay());
                    yield break;
                }
                State.day++;
            }
            Save();
            OSRoot.I.ShowBoot();
        }

        public void TriggerEnding(string id)
        {
            if (endingRunning || string.IsNullOrEmpty(id)) return;
            endingRunning = true;
            StartCoroutine(EndingRoutine(id));
        }

        IEnumerator EndingRoutine(string id)
        {
            StopDayRoutines();
            if (!Global.endings.Contains(id)) Global.endings.Add(id);
            SaveSystem.SaveGlobal(Global);
            Debug.Log($"[Ending] {id}（到達数 {EndingCount}）");

            var def = Content.Config.endings.FirstOrDefault(e => e.id == id)
                      ?? new EndingDef { id = id, title = id, text = "" };
            yield return OSRoot.I.ShowEnding(def);

            // メタ演出：本物のデスクトップにファイルを残す
            if (!string.IsNullOrEmpty(def.realFile))
                RealDesktop.Create(def.realFile, TextResolver.Resolve(def.realText)
                    + (string.IsNullOrEmpty(def.title) ? "" : "\n\n\n――――――――――\n" + def.title));

            // エンドを見たら、その周回のデータは消して最初から
            SaveSystem.DeleteState(State.profile);
            State = SaveSystem.LoadState("main") ?? new GameState();
            endingRunning = false;
            OSRoot.I.ShowTitle(); // END一覧に新しいENDが増えているのを見せる
        }

        public void NewGame()
        {
            SaveSystem.DeleteState("main");
            State = new GameState();
        }

        public void ResetEverything()
        {
            StopAllCoroutines();
            StopDayRoutines();
            SaveSystem.DeleteAll();
            Global = new GlobalSave();
            State = new GameState();
            endingRunning = false;
            OSRoot.I.ShowTitle();
        }
    }
}
