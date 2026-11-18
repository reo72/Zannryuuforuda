using System.Collections;
using UnityEngine;

namespace Zanryu
{
    /// <summary>
    /// events.json に書いた「いつ・何が起きるか」を実行する。
    ///
    /// trigger（いつ）:
    ///   dayStart    … その日が始まったとき
    ///   fileOpened  … target のファイルを開いたとき
    ///   flag        … target のフラグが立ったとき
    ///   timer       … その日が始まってから delay 秒後
    ///
    /// actions（何が起きるか）: type と引数 a / b / f
    ///   flag      a=フラグ名               フラグを立てる
    ///   rename    a=ファイルID b=新しい名前 ファイル名を変える
    ///   glitch    f=秒数                   画面が乱れる
    ///   message   a=タイトル b=本文        エラーダイアログ
    ///   toast     a=本文                   右下の通知
    ///   say       a=本文 b=名前（省略可）  画面下に独り言を出し、読み終わるまで次のアクションを待つ
    ///   sayAsync  a=本文 b=名前（省略可）  独り言を出す（待たない）
    ///   open      a=ファイルID             ファイルを勝手に開く
    ///   cursor    a=ファイルID f=秒数 b="open" カーソルがデスクトップのアイコンへ勝手に動く（b=open なら開く）
    ///   chat      a=スレッドID b=ノードID  会話を始める
    ///   wallpaper a=画像名                 壁紙を変える
    ///   ambient   a=音名（空で停止） f=音量 環境音を変える
    ///   sound     a=音名                   効果音
    ///   bgm       a=曲名（空で停止） f=フェード秒  BGM を切り替える
    ///   closeAll                           全ウィンドウを閉じる
    ///   unlock    a=ファイルID             ロックを解除する
    ///   ending    a=エンドID               エンドへ
    ///   realFile  a=ファイル名 b=中身      メタ演出：本物のデスクトップにテキストファイルを作る（オプションでOFFなら何もしない）
    ///   wait      f=秒数                   次のアクションまで待つ
    /// </summary>
    public class HorrorEventSystem : MonoBehaviour
    {
        GameManager gm;

        public void Init(GameManager g)
        {
            gm = g;
            gm.DayStarted += () => Fire("dayStart", null);
            gm.FileOpened += f => Fire("fileOpened", f.id);
            gm.FlagSet += f => Fire("flag", f);
        }

        public void StopAll() => StopAllCoroutines();

        void Update()
        {
            if (gm == null || !gm.InDesktop) return;
            foreach (var e in Content.Events)
                if (e.trigger == "timer" && gm.DayTime >= e.delay && CanFire(e)) Execute(e, 0);
        }

        public void Fire(string trigger, string target)
        {
            foreach (var e in Content.Events)
            {
                if (e.trigger != trigger) continue;
                if (!string.IsNullOrEmpty(e.target) && e.target != target) continue;
                if (!CanFire(e)) continue;
                Execute(e, e.delay);
            }
        }

        // timer で day 指定がないものは「毎日1回」、それ以外は「1周で1回」
        string Key(HorrorEvent e) => e.trigger == "timer" && e.day == 0 ? $"{e.id}@{gm.State.day}" : e.id;

        bool CanFire(HorrorEvent e)
        {
            if (!gm.ProfileOk(e.profile)) return false;
            int day = gm.State.day;
            if (e.day > 0 && e.day != day) return false;
            if (e.minDay > 0 && day < e.minDay) return false;
            if (e.maxDay > 0 && day > e.maxDay) return false;
            if (gm.EndingCount < e.minEndings) return false;
            if (!gm.RequireOk(e.requireFlag) || !gm.ForbidOk(e.forbidFlag)) return false;
            bool once = !e.repeat || e.trigger == "timer";
            if (once && gm.State.firedEvents.Contains(Key(e))) return false;
            return true;
        }

        void Execute(HorrorEvent e, float delay)
        {
            if (!e.repeat || e.trigger == "timer")
            {
                gm.State.firedEvents.Add(Key(e));
                gm.Save();
            }
            Debug.Log($"[Event] {e.id}");
            StartCoroutine(Run(e, delay));
        }

        IEnumerator Run(HorrorEvent e, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            foreach (var a in e.actions)
            {
                if (!gm.InDesktop) yield break; // シャットダウン・エンド中は止める
                switch (a.type)
                {
                    case "wait":
                        yield return new WaitForSeconds(a.f);
                        break;
                    case "say":
                        // 独り言を出して、プレイヤーが読み終わるまで待つ
                        yield return OSRoot.I.Monologue.SayAndWait(a.a, a.b);
                        break;
                    case "cursor":
                        var target = OSRoot.I.Desktop != null ? OSRoot.I.Desktop.GetIcon(a.a) : null;
                        yield return OSRoot.I.Cursor.Hijack(target, a.f > 0 ? a.f : 1.5f);
                        if (a.b == "open") gm.OpenFile(Content.GetFile(a.a));
                        break;
                    default:
                        Do(a);
                        break;
                }
            }
        }

        void Do(EventAction a)
        {
            var os = OSRoot.I;
            switch (a.type)
            {
                case "flag": gm.SetFlag(a.a); break;
                case "rename": gm.Rename(a.a, a.b); break;
                case "glitch": os.Glitch.Play(a.f > 0 ? a.f : 0.3f); break;
                case "message": os.MessageBox(TextResolver.Resolve(a.a), TextResolver.Resolve(a.b)); break;
                case "toast": os.Toast(TextResolver.Resolve(a.a)); break;
                case "sayAsync": os.Monologue.Say(a.a, a.b); break;
                case "open": gm.OpenFile(Content.GetFile(a.a)); break;
                case "chat": gm.Chat.Play(a.a, a.b); break;
                case "wallpaper": gm.SetWallpaper(a.a); break;
                case "ambient":
                    if (string.IsNullOrEmpty(a.a)) os.SetAmbientVolume(a.f);
                    else os.PlayAmbient(a.a, a.f);
                    break;
                case "sound": os.PlaySound(a.a, a.f > 0 ? a.f : 1f); break;
                case "closeAll": os.Windows.CloseAll(); break;
                case "unlock": gm.Unlock(Content.GetFile(a.a)); break;
                case "ending": gm.TriggerEnding(a.a); break;
                case "bgm": os.PlayBgm(a.a, a.f > 0 ? a.f : 1.5f); break;
                case "realFile": RealDesktop.Create(TextResolver.Resolve(a.a), TextResolver.Resolve(a.b)); break;
                default: Debug.LogWarning($"[Event] 未対応のアクション: {a.type}"); break;
            }
        }
    }
}
