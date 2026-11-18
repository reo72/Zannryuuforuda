using System;
using System.Collections.Generic;

namespace Zanryu
{
    [Serializable] public class StringPair { public string key; public string value; }
    [Serializable] public class OpenLog { public int day; public string fileId; public float time; }
    [Serializable] public class ChatLine { public string thread; public string from; public string text; public int day; }
    [Serializable] public class WaitingChoice { public string thread; public string node; public int day; }

    /// <summary>1周分の進行データ（プロファイルごとに save_main.json / save_yui.json に保存）</summary>
    [Serializable]
    public class GameState
    {
        public string profile = "main";
        public int day = 1;
        public string wallpaper;                                         // 空なら config の既定壁紙
        public List<string> flags = new List<string>();
        public List<StringPair> nameOverrides = new List<StringPair>();  // イベントで変えたファイル名
        public List<string> unlocked = new List<string>();               // パスワード解除済みファイル
        public List<OpenLog> openLogs = new List<OpenLog>();             // プレイヤーの行動記録（日記に使う）
        public List<string> firedEvents = new List<string>();
        public List<string> playedChats = new List<string>();
        public List<ChatLine> chatHistory = new List<ChatLine>();
        public List<WaitingChoice> waiting = new List<WaitingChoice>();  // 返信待ちの選択肢
        public List<string> unread = new List<string>();                 // 未読のチャット相手
    }

    /// <summary>周回をまたいで残るデータ（global.json）</summary>
    [Serializable]
    public class GlobalSave
    {
        public List<string> endings = new List<string>();  // 見たエンドのID
        public bool useRealPcName = true;                  // PC名をゲーム内に出すか（オプション）
        public bool metaFiles = true;                      // メタ演出：本物のデスクトップにファイルを作るか
        public bool metaNoticeShown;                       // 最初の起動時の注意書きを見たか
        public List<string> secrets = new List<string>();  // 見つけた隠し要素
        public List<string> realFiles = new List<string>();      // ゲームが作ったファイル名（これ以外は絶対に触らない）
        public List<string> noticedDeleted = new List<string>(); // 消されたことに気づいたファイル
        public float volume = 1f;                          // 全体の音量（0〜1）
        public float bgmVolume = 0.7f;                     // BGM
        public float seVolume = 0.8f;                      // 効果音
        public float ambientVolume = 0.8f;                 // 環境音（ファンの音など）
        public bool fullscreen = true;                     // 画面モード
        public int textSpeed = 1;                          // 文字の速さ 0=ゆっくり 1=ふつう 2=はやい
    }
}
