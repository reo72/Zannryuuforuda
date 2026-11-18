using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// チャットの進行（メッセージが届く・選択肢で返信する）を管理する。表示は ChatApp が担当。
    /// 会話は chats.json の scripts（いつ始まるか）と nodes（会話の中身）で定義する。
    /// </summary>
    public class ChatSystem : MonoBehaviour
    {
        public event Action Updated;
        GameManager gm;
        readonly HashSet<string> busy = new HashSet<string>();

        public void Init(GameManager g)
        {
            gm = g;
            gm.DayStarted += OnDayStarted;
            gm.FlagSet += _ => CheckScripts();
        }

        public void StopAll()
        {
            StopAllCoroutines();
            busy.Clear();
        }

        void OnDayStarted()
        {
            StopAll();
            CheckScripts();
            Updated?.Invoke();
        }

        /// <summary>条件を満たした会話を開始する（日の開始時とフラグが立ったときに呼ばれる）</summary>
        void CheckScripts()
        {
            if (gm == null || !gm.InDesktop) return;
            foreach (var t in Content.Threads)
            {
                if (!gm.ProfileOk(t.profile)) continue;
                foreach (var s in t.scripts)
                {
                    if (s.day != 0 && s.day != gm.State.day) continue;
                    if (!gm.RequireOk(s.requireFlag) || !gm.ForbidOk(s.forbidFlag)) continue;
                    string key = $"{t.id}:{s.day}:{s.start}";
                    if (gm.State.playedChats.Contains(key) || IsBusy(t.id)) continue;
                    gm.State.playedChats.Add(key);
                    gm.Save();
                    Play(t.id, s.start, s.delay);
                }
            }
        }

        bool IsBusy(string thread) => busy.Contains(thread) || gm.State.waiting.Any(w => w.thread == thread);

        public void Play(string threadId, string nodeId, float initialDelay = 0)
        {
            if (Content.GetThread(threadId) == null) { Debug.LogWarning($"[Chat] スレッドがありません: {threadId}"); return; }
            busy.Add(threadId); // コルーチン開始前に入れておく（二重開始を防ぐ）
            StartCoroutine(Run(threadId, nodeId, initialDelay));
        }

        IEnumerator Run(string threadId, string nodeId, float initialDelay)
        {
            var t = Content.GetThread(threadId);
            if (initialDelay > 0) yield return new WaitForSeconds(initialDelay);
            var node = t.Node(nodeId);
            while (node != null)
            {
                bool mine = node.from == "me";
                float d = node.delay > 0 ? node.delay : (mine ? 0.4f : 1.6f);
                yield return new WaitForSeconds(d);

                var text = TextResolver.Resolve(node.text);
                AddLine(t, mine ? "me" : "them", text);
                if (!mine) OSRoot.I.Toast($"{t.contact}\n{Shorten(text, 40)}");

                if (node.choices.Count > 0)
                {
                    gm.State.waiting.Add(new WaitingChoice { thread = t.id, node = node.id, day = gm.State.day });
                    gm.Save();
                    Updated?.Invoke();
                    busy.Remove(threadId);
                    gm.SetFlag(node.setFlag);
                    yield break;
                }
                Updated?.Invoke();
                gm.SetFlag(node.setFlag);
                node = t.Node(node.next);
            }
            busy.Remove(threadId);
            CheckScripts(); // 待っていた次の会話があれば始める
        }

        /// <summary>今その相手に返信できる選択肢（なければ null）</summary>
        public List<ChatChoice> GetChoices(string threadId)
        {
            var w = gm.State.waiting.FirstOrDefault(x => x.thread == threadId);
            if (w == null) return null;
            return Content.GetThread(threadId)?.Node(w.node)?.choices;
        }

        public void Choose(string threadId, int index)
        {
            var w = gm.State.waiting.FirstOrDefault(x => x.thread == threadId);
            if (w == null) return;
            var t = Content.GetThread(threadId);
            var node = t?.Node(w.node);
            if (node == null || index < 0 || index >= node.choices.Count) return;
            var c = node.choices[index];

            gm.State.waiting.Remove(w);
            AddLine(t, "me", TextResolver.Resolve(c.text));
            gm.Save();
            Updated?.Invoke();
            if (!string.IsNullOrEmpty(c.next)) Play(threadId, c.next); // 先に Play してからフラグ（イベントの二重起動を防ぐ）
            gm.SetFlag(c.setFlag);
            if (string.IsNullOrEmpty(c.next)) CheckScripts();
        }

        void AddLine(ChatThread t, string from, string text)
        {
            gm.State.chatHistory.Add(new ChatLine { thread = t.id, from = from, text = text, day = gm.State.day, time = gm.ClockText() });
            if (from != "me" && !gm.State.unread.Contains(t.id)) gm.State.unread.Add(t.id);
            gm.Save();
        }

        public void MarkRead(string threadId)
        {
            if (gm.State.unread.Remove(threadId)) gm.Save();
        }

        public void NotifyUpdated() => Updated?.Invoke();

        static string Shorten(string s, int n)
        {
            s = s.Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }
    }
}
