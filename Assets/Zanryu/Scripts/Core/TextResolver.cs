using System;
using System.Linq;

namespace Zanryu
{
    /// <summary>
    /// テキスト中のトークンを置き換える。プレイヤーの行動を日記に書かせる仕組みの中心。
    ///
    ///  {PC_NAME}              プレイヤーのPCのユーザー名（オプションでOFFにできる）
    ///  {DAY}                  何日目か
    ///  {TIME} / {DATE}        ゲーム内の時刻・日付
    ///  {LAST_FILE}            今日最後に開いたファイル名
    ///  {LAST_FILE_YESTERDAY}  昨日最後に開いたファイル名
    ///  {OPEN_COUNT}           今日開いたファイル数
    ///  {OPEN_COUNT_YESTERDAY} 昨日開いたファイル数
    ///  {OPEN_COUNT_TOTAL}     今までに開いたファイル数
    ///  {MEMORY_MB}            /sys/memory の容量（日ごとに増える）
    ///  {ENDINGS}              見たエンド数
    ///  {OPENED_LIST_TODAY}    今日開いたファイル名の一覧
    ///  {NOTES}                「わかったこと」メモ（config.json の notes）
    /// </summary>
    public static class TextResolver
    {
        public static string PcName
        {
            get
            {
                var fallback = Content.Config.pcNameFallback;
                var gm = GameManager.I;
                if (gm != null && gm.Global != null && !gm.Global.useRealPcName) return fallback;
                try
                {
                    var n = Environment.UserName;
                    return string.IsNullOrEmpty(n) ? fallback : n;
                }
                catch { return fallback; }
            }
        }

        public static string Resolve(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            if (s.IndexOf('{') < 0) return s;
            var gm = GameManager.I;
            if (gm == null || gm.State == null) return s.Replace("{PC_NAME}", PcName);

            int day = gm.State.day;
            s = s.Replace("{PC_NAME}", PcName)
                 .Replace("{DAY}", day.ToString())
                 .Replace("{ENDINGS}", gm.EndingCount.ToString());

            if (s.Contains("{TIME}")) s = s.Replace("{TIME}", gm.ClockText());
            if (s.Contains("{DATE}")) s = s.Replace("{DATE}", gm.DateText());
            if (s.Contains("{LAST_FILE_YESTERDAY}")) s = s.Replace("{LAST_FILE_YESTERDAY}", LastFileName(day - 1));
            if (s.Contains("{LAST_FILE}")) s = s.Replace("{LAST_FILE}", LastFileName(day));
            if (s.Contains("{OPEN_COUNT_YESTERDAY}")) s = s.Replace("{OPEN_COUNT_YESTERDAY}", OpenCount(day - 1).ToString());
            if (s.Contains("{OPEN_COUNT_TOTAL}")) s = s.Replace("{OPEN_COUNT_TOTAL}", gm.State.openLogs.Count(IsDocument).ToString());
            if (s.Contains("{OPEN_COUNT}")) s = s.Replace("{OPEN_COUNT}", OpenCount(day).ToString());
            if (s.Contains("{NOTES}")) s = s.Replace("{NOTES}", Notes());
            if (s.Contains("{OPENED_LIST_TODAY}")) s = s.Replace("{OPENED_LIST_TODAY}", OpenedList(day));
            if (s.Contains("{MEMORY_MB}")) s = s.Replace("{MEMORY_MB}", (128 + day * 37 + gm.EndingCount * 11).ToString());
            return s;
        }

        // 「ファイルを開いた」に数えるのは中身のあるファイルだけ（フォルダやアプリは除く）
        static bool IsDocument(OpenLog l)
        {
            var f = Content.GetFile(l.fileId);
            return f != null && (f.type == "text" || f.type == "image" || f.type == "audio");
        }

        /// <summary>立っているフラグに対応する「わかったこと」を、見つけた順ではなく物語の順に並べる</summary>
        static string Notes()
        {
            var gm = GameManager.I;
            var lines = Content.Config.notes.Where(n => gm.RequireOk(n.requireFlag) && !string.IsNullOrEmpty(n.requireFlag))
                                            .Select(n => n.text.Replace("{PC_NAME}", PcName)).ToList();
            return lines.Count == 0 ? "（まだ何もわからない）" : string.Join("\n\n", lines);
        }

        static string OpenedList(int day)
        {
            var names = GameManager.I.State.openLogs.Where(l => l.day == day && IsDocument(l))
                .Select(l => Content.GetFile(l.fileId)).Where(f => f != null)
                .Select(f => GameManager.I.RawDisplayName(f).Replace("{PC_NAME}", PcName)).Distinct().ToList();
            return names.Count == 0 ? "（なにも）" : "・" + string.Join("\n・", names);
        }

        static int OpenCount(int day) =>
            GameManager.I.State.openLogs.Count(l => l.day == day && IsDocument(l));

        static string LastFileName(int day)
        {
            var log = GameManager.I.State.openLogs.LastOrDefault(l => l.day == day && IsDocument(l));
            if (log == null) return "なにも";
            var f = Content.GetFile(log.fileId);
            // 名前の中に {LAST_FILE} 系が入っていると無限に置換されるので、PC名だけ置換する
            return f == null ? "なにも" : GameManager.I.RawDisplayName(f).Replace("{PC_NAME}", PcName);
        }
    }
}
