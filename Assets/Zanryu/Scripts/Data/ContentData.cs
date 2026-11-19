using System;
using System.Collections.Generic;

namespace Zanryu
{
    // =====================================================================
    //  JSON（Resources/Content/*.json）から読み込むデータの型。
    //  JsonUtility を使うので、フィールド名と JSON のキー名を一致させること。
    //  書かなかった項目は 0 / null / false になる。
    // =====================================================================

    // ---------------- ファイル（files.json） ----------------

    /// <summary>日数やエンド数に応じてファイルの中身を差し替える「差分」</summary>
    [Serializable]
    public class FileVariant
    {
        public int fromDay;        // この日以降に適用（0 = 常に）
        public int minEndings;     // 見たエンド数がこれ以上なら適用（周回差分）
        public string requireFlag; // このフラグがあるときだけ適用（カンマ区切りで複数可）
        public string name;        // 空なら元の名前のまま
        public string content;     // 空なら元の中身のまま
        public string asset;       // 空なら元の素材のまま
    }

    [Serializable]
    public class FileEntry
    {
        public string id;          // 一意のID
        public string parent;      // 親フォルダのID（"desktop" ならデスクトップに表示、"root" ならPC直下）
        public string type;        // folder / text / image / audio / app / confirm
        public string name;        // 表示名（{PC_NAME} などのトークン可）
        public string content;     // テキスト本文。image/audio では「撮影・録音メモ」として仮表示される
        public string asset;       // Resources/Images or Audio 内の素材名（拡張子なし）
        public string icon;        // Resources/Icons 内のアイコン名（空なら種類ごとの仮アイコン）
        public string app;         // type=app のとき: explorer / chat / taskmgr
        public string password;    // 空でなければロック
        public string requireFlag; // このフラグがないと出現しない（カンマ区切りで複数可）
        public string forbidFlag;  // このフラグがあると消える
        public string setFlag;     // 開いたときに立つフラグ（confirm は「はい」で立つ）
        public string profile;     // main（既定）/ yui / you / any
        public string requireSecret; // 隠しパスワードで解放される要素（2周目以降も残る）
        public int minDay;         // 出現する最初の日（0 = 1日目から）
        public int maxDay;         // 出現する最後の日（0 = 制限なし）
        public int minEndings;     // 見たエンド数がこれ以上で出現（周回で増える伏線用）
        public bool hidden;        // 隠しファイル（エクスプローラーで「隠しファイル表示」をONにすると見える）
        public List<FileVariant> variants = new List<FileVariant>();
    }

    [Serializable]
    public class FileList { public List<FileEntry> files = new List<FileEntry>(); }

    // ---------------- チャット（chats.json） ----------------

    [Serializable]
    public class ChatChoice
    {
        public string text;
        public string setFlag;
        public string next;        // 次のノードID（空なら会話終了）
    }

    [Serializable]
    public class ChatNode
    {
        public string id;
        public string from;        // "them"（相手）/ "me"（自分）
        public string text;
        public float delay;        // このメッセージが届くまでの秒数（0 なら既定値）
        public string setFlag;     // 表示されたときに立つフラグ
        public string next;        // 次のノードID
        public List<ChatChoice> choices = new List<ChatChoice>();
    }

    /// <summary>「何日目に・どの条件で・どのノードから」会話を始めるか</summary>
    [Serializable]
    public class ChatScript
    {
        public int day;            // 0 = 何日目でも（条件を満たした時点で開始）
        public string requireFlag;
        public string forbidFlag;
        public string start;       // 開始ノードID
        public float delay;        // 開始までの秒数
    }

    [Serializable]
    public class ChatThread
    {
        public string id;
        public string contact;     // 相手の表示名
        public string address;     // メールアドレス（トークン可）
        public string subject;     // 件名
        public string profile;     // main（既定）/ yui / any
        public List<ChatScript> scripts = new List<ChatScript>();
        public List<ChatNode> nodes = new List<ChatNode>();

        public ChatNode Node(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            foreach (var n in nodes) if (n.id == nodeId) return n;
            UnityEngine.Debug.LogWarning($"[Chat] ノードが見つかりません: {id}/{nodeId}");
            return null;
        }
    }

    [Serializable]
    public class ChatList { public List<ChatThread> threads = new List<ChatThread>(); }

    // ---------------- ホラーイベント（events.json） ----------------

    [Serializable]
    public class EventAction
    {
        public string type;        // flag / rename / glitch / message / toast / open / cursor / chat / wallpaper / ambient / sound / closeAll / unlock / ending / wait
        public string a;           // 引数1
        public string b;           // 引数2
        public float f;            // 数値引数（秒数・音量など）
    }

    [Serializable]
    public class HorrorEvent
    {
        public string id;
        public string trigger;     // dayStart / fileOpened / flag / timer
        public string target;      // fileOpened ならファイルID、flag ならフラグ名
        public float delay;        // 発火までの秒数（timer なら「その日の開始から何秒後か」）
        public int day;            // 0 = 何日目でも
        public int minDay;
        public int maxDay;
        public int minEndings;
        public string requireFlag;
        public string forbidFlag;
        public string profile;     // main（既定）/ yui / any
        public bool repeat;        // true なら何度でも発火（timer 以外）
        public List<EventAction> actions = new List<EventAction>();
    }

    [Serializable]
    public class EventList { public List<HorrorEvent> events = new List<HorrorEvent>(); }

    // ---------------- 独り言（monologues.json） ----------------

    /// <summary>主人公の独り言 1 件分（画面下のメッセージウィンドウに出る）</summary>
    [Serializable]
    public class MonologueEntry
    {
        public string id;
        public string file;        // reactions 用：このファイルを初めて開いたとき
        public int day;            // 0 = 何日目でも
        public int minDay;
        public int maxDay;
        public string requireFlag;
        public string forbidFlag;
        public string profile;     // main（既定）/ yui / any
        public float delay;        // 表示までの秒数
        public float idle;         // hints 用：何もせずにこの秒数たったら表示
        public float repeatEvery;  // hints 用：0 より大きいと、この秒数ごとに何度でも出る（シャットダウンの催促など）
        public string speaker;     // 名前欄（空なら名前なし＝主人公）
        public bool requireMeta;   // メタ演出（実機デスクトップにファイル）が有効なときだけ
        public List<string> lines = new List<string>(); // 1 要素 = 1 ページ
    }

    [Serializable]
    public class MonologueData
    {
        public List<MonologueEntry> intros = new List<MonologueEntry>();    // その日の始まり
        public List<MonologueEntry> reactions = new List<MonologueEntry>(); // ファイルを初めて開いたとき
        public List<MonologueEntry> hints = new List<MonologueEntry>();     // 迷っているとき・ヒントボタン
    }

    // ---------------- 検索ソフト（web.json） ----------------

    /// <summary>Lumen Search で読めるページ（ニュース・ブログ・掲示板など）</summary>
    [Serializable]
    public class WebPage
    {
        public string id;
        public string site;        // サイト名（例：東都新聞）
        public string url;         // 表示用の URL（https:// は自動で付く）
        public string title;
        public string date;        // 並び替えにも使うので「2016/10/04」の形で
        public string body;
        public string asset;       // Resources/Images の写真（記事の上に出る）
        public int minDay;
        public int maxDay;
        public int minEndings;     // 見たエンド数がこれ以上で読めるようになる（周回で真相に近づく）
        public string requireFlag;
        public string forbidFlag;
        public string setFlag;     // 初めて読んだときに立つフラグ（read_ページID は自動）
        public List<string> tags = new List<string>();   // "feed" ならホームのニュース欄に出る
        public List<string> links = new List<string>();  // 関連リンク（ページID）
        public List<string> react = new List<string>();  // 初めて読んだときの独り言
        public string style;       // 見た目：news / tech / blog / bbs / sns / shop / qa / corp / weather / dict / plain
        public string category;    // 記事のカテゴリ（例：社会、スポーツ）
        public string author;      // 書いた人（例：社会部、管理人：とおる）
        public string caption;     // 写真の説明文
        public List<string> comments = new List<string>(); // コメント欄
    }

    /// <summary>どのキーワードで、どのページが検索結果に出るか</summary>
    [Serializable]
    public class WebSearch
    {
        public List<string> keywords = new List<string>(); // どれかを含めば一致（全角半角・大小文字・空白は無視）
        public List<string> results = new List<string>();  // ページID
        public int minDay;
        public string requireFlag;
    }

    [Serializable]
    public class WebData
    {
        public List<WebPage> pages = new List<WebPage>();
        public List<WebSearch> searches = new List<WebSearch>();
        public List<string> trending = new List<string>(); // ホームの「急上昇ワード」
        public List<string> ads = new List<string>();      // ページに出る広告の文
        public List<string> fallback = new List<string>(); // 何も見つからないときに出すページ（{QUERY} が検索語になる）
    }

    // ---------------- 全体設定（config.json） ----------------

    [Serializable]
    public class AccountDef
    {
        public string id;
        public string name;                 // トークン可（{PC_NAME} など）
        public string profile;              // ログイン先のプロファイル
        public bool selectable;             // false なら灰色で選べない
        public List<string> unlockEndings = new List<string>();     // これを全部見たら選べるようになる
        public List<string> showAfterEndings = new List<string>();  // これを全部見るまで表示されない
    }

    [Serializable]
    public class ProcessDef
    {
        public string name;
        public string desc;
        public string memory;
        public string killFlag;    // 「タスクの終了」で立つフラグ（空なら終了できない）
        public int killMaxDay;     // この日を過ぎると終了できない（0 = 制限なし）
        public int minDay;
        public string requireFlag;
    }

    [Serializable]
    public class EndingDef
    {
        public string id;
        public string title;
        public string text;
        public string hint;        // END一覧に出すヒント（まだ見ていないとき）
        public string bgm;         // エンド画面のBGM（空なら config の endingBgm）
        public string realFile;    // メタ演出：エンド後に本物のデスクトップに作るファイル名（空なら作らない）
        public string realText;    // その中身
    }

    /// <summary>「わかったこと」メモの1行（フラグが立つと追記される）</summary>
    [Serializable]
    public class NoteDef
    {
        public string requireFlag;
        public string text;
    }

    /// <summary>ログインパスワード（日によって変わる）</summary>
    [Serializable]
    public class LoginPassword
    {
        public string account;     // AccountDef の id
        public int fromDay;
        public int toDay;          // 0 = 最後まで
        public string password;    // トークン可（{PC_NAME} など）
        public string hint;        // 「ヒント」ボタンで出る文
        public string hint2;       // 2回間違えたあとに出る、もっと具体的なヒント
    }

    /// <summary>隠しパスワード（入力すると何かが起きる）</summary>
    [Serializable]
    public class SecretPassword
    {
        public string account;     // AccountDef の id（"*" ならどのアカウントでも）
        public List<string> passwords = new List<string>(); // どれか1つに一致すれば発動（大文字小文字は区別しない）
        public int minDay;
        public int maxDay;
        public List<string> requireEndings = new List<string>();
        public string message;     // ログイン画面に出す文
        public bool glitch;        // 画面を乱れさせる
        public string setSecret;   // 2周目以降も残る「隠し要素」フラグ
        public bool login;         // そのアカウントにログインする
    }

    [Serializable]
    public class GameConfig
    {
        public string gameTitle = "残留ファイル";
        public string gameSubtitle = "";
        public string osName = "Lumen OS";
        public int lastDay = 7;
        public string startTime = "22:41";
        public float secondsPerMinute = 5f;
        public string freezeTime = "23:47";
        public float freezeLength = 2f;
        public string dateFormat = "{M}月{D}日";
        public int startMonth = 4;
        public int startDay = 12;
        public string defaultWallpaper = "room_window";
        public string ambient = "ambient_fan";
        public string titleBgm = "bgm_title";
        public string endingBgm = "bgm_ending";
        public string yuiBgm = "bgm_true";
        public List<string> dayBgm = new List<string>(); // 日ごとのBGM（1日目から順に。空なら無音＝環境音だけ）
        public bool useFakeCursor = true;
        public int sellerThreshold = 3;
        public int pretendThreshold = 4;
        public string pcNameFallback = "user";
        public List<string> bootLines = new List<string>();
        public List<AccountDef> accounts = new List<AccountDef>();
        public List<string> shutdownMessages = new List<string>();
        public List<ProcessDef> processes = new List<ProcessDef>();
        public List<EndingDef> endings = new List<EndingDef>();
        public List<LoginPassword> loginPasswords = new List<LoginPassword>();
        public List<string> wrongPasswordMessages = new List<string>();     // 間違えるたびに1つずつ進む。最後まで行くと勝手にログインされる
        public List<SecretPassword> secretPasswords = new List<SecretPassword>();
        public string metaNotice = "";
        public List<NoteDef> notes = new List<NoteDef>();  // 「わかったこと」メモ（{NOTES} で一覧になる）
        public List<string> prologue = new List<string>();  // はじめてPCを起動する前に流れる導入文
        public int refuseShutdownDay;                       // この日、最初のシャットダウンは拒否される（フラグ shutdown_refused が立つ）
        public string earphoneNotice = "";                  // タイトルと導入の最初に出す「イヤホン推奨」
        public string howToPlayTitle = "";
        public string howToPlay = "";                       // 導入文のあとに出る「遊び方」
    }
}
