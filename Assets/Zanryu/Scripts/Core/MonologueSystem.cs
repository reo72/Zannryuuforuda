using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// monologues.json の独り言を、いつ出すかを決める。
    ///   intros    … その日が始まったとき（1日1回）
    ///   reactions … そのファイルを初めて開いたとき（1周で1回）
    ///   hints     … しばらく何もしていないとき（1日1回）／タスクバーの「？ ヒント」ボタン
    /// hints は上から順に条件を見て、最初に当てはまったものを出す。
    /// 「opened_ファイルID」フラグ（開いたら自動で立つ）を条件にすると、進み具合に合わせたヒントになる。
    /// </summary>
    public class MonologueSystem : MonoBehaviour
    {
        GameManager gm;
        float lastActivity;
        readonly Dictionary<string, float> lastShown = new Dictionary<string, float>(); // 繰り返しヒントの最後に出した時刻

        MonologueBox Box => OSRoot.I.Monologue;

        public void Init(GameManager g)
        {
            gm = g;
            gm.DayStarted += OnDayStarted;
            gm.FileOpened += OnFileOpened;
            gm.FlagSet += _ => lastActivity = gm.DayTime;
        }

        public void StopAll()
        {
            StopAllCoroutines();
            Box.Clear();
        }

        bool Match(MonologueEntry e)
        {
            if (!gm.ProfileOk(e.profile)) return false;
            int day = gm.State.day;
            if (e.day > 0 && e.day != day) return false;
            if (e.minDay > 0 && day < e.minDay) return false;
            if (e.maxDay > 0 && day > e.maxDay) return false;
            return gm.RequireOk(e.requireFlag) && gm.ForbidOk(e.forbidFlag);
        }

        bool TryMark(string key)
        {
            if (gm.State.firedEvents.Contains(key)) return false;
            gm.State.firedEvents.Add(key);
            gm.Save();
            return true;
        }

        void OnDayStarted()
        {
            StopAllCoroutines();
            lastActivity = 0;
            lastShown.Clear();
            foreach (var e in Content.Monologues.intros)
                if (Match(e) && TryMark($"mono:{e.id}@{gm.State.day}"))
                    StartCoroutine(Show(e, e.delay > 0 ? e.delay : 1.5f));
        }

        void OnFileOpened(FileEntry f)
        {
            lastActivity = gm.DayTime;
            foreach (var e in Content.Monologues.reactions)
                if (e.file == f.id && Match(e) && TryMark($"mono:{e.id}"))
                    StartCoroutine(Show(e, e.delay > 0 ? e.delay : 0.6f));
        }

        IEnumerator Show(MonologueEntry e, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!gm.InDesktop) yield break;
            Box.SayLines(e.lines, e.speaker);
        }

        void Update()
        {
            if (gm == null || !gm.InDesktop || Box.Busy) return;
            float idle = gm.DayTime - lastActivity;
            if (idle < 5f) return;
            foreach (var h in Content.Monologues.hints)
            {
                if (h.idle <= 0 || idle < h.idle || !Match(h)) continue;
                if (h.repeatEvery > 0)
                {
                    // 何度でも出すヒント（前回から repeatEvery 秒たっていれば再表示）
                    if (lastShown.TryGetValue(h.id, out var t) && gm.DayTime - t < h.repeatEvery) continue;
                    lastShown[h.id] = gm.DayTime;
                }
                else if (!TryMark($"mono:{h.id}@{gm.State.day}")) continue;
                Box.SayLines(h.lines, h.speaker);
                lastActivity = gm.DayTime;
                break;
            }
        }

        /// <summary>「？ ヒント」ボタン：今の状況に合うヒントをすぐ出す（何度でも見られる）</summary>
        public void ShowHintNow()
        {
            if (!gm.InDesktop) return;
            Box.Clear();
            var h = Content.Monologues.hints.FirstOrDefault(Match);
            if (h != null) Box.SayLines(h.lines, h.speaker);
            else Box.Say("今は特に、やることはなさそうだ。\n気になるところを調べてみよう。");
            lastActivity = gm.DayTime;
        }
    }
}
