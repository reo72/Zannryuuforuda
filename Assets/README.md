# 残留ファイル（仮）— 基盤プロジェクト

PC画面型の心理ホラーを作るための土台です。
**画像・音声・BGMがひとつもなくても、最初から最後（全エンド）まで通して遊べます。**
素材がまだない場所には「[画像未設定: ○○]」と撮影メモが表示されます。

---

## 1. セットアップ（5分）

1. Unity Hub で **Unity 6（2D または Universal 2D テンプレート）** の新規プロジェクトを作る
2. この ZIP の中の `Assets/Zanryu` フォルダを、プロジェクトの `Assets` フォルダにドラッグする
3. 上のメニューから **「残留ファイル」→「メインシーンを作成」** を選ぶ
4. ▶ 再生ボタンを押す

> 追加パッケージは不要です（uGUI と Input System は Unity 6 に最初から入っています）。
> 文字は Windows の「Yu Gothic UI」などを自動で使います。

---

## 2. 遊び方とテスト方法

起動 → ログイン画面で「misaki」を選ぶ → デスクトップ。スタートメニューの「シャットダウン」で次の日に進みます。

### デバッグキー（エディタと開発ビルドだけで有効）
| キー | 内容 |
|---|---|
| F1 | 状態表示（日数・時刻・フラグ・見たエンド） |
| F2 | 今すぐシャットダウン（次の日へ） |
| F3 | グリッチ演出を試す |
| F4 | カーソル乗っ取りを試す |
| F5 | セーブを全部消して最初から |

### 各エンドへの行き方（テスト用）
| エンド | 条件 |
|---|---|
| END0 電源 | Day1〜3 にタスクマネージャーで `yui.exe` を終了 |
| END2 初期化 | Day4 以降、PC → 「隠しファイル: 表示」→ system → `reset.exe` →「はい」 |
| END3 返品 | 出品者に Day1「データが残っています」→ Day3「消してもいいですか？」→ Day5「あの子って だれですか」→ Day7 にシャットダウン |
| END4 なりきり | ハルに Day2〜5 で毎回ミサキとして返事 → Day7 にシャットダウン |
| END1 残留 | 上のどれにも当てはまらずに Day7 をシャットダウン |
| TRUE END | END1〜4 を全部見ると、ログイン画面の「yui」が選べるようになる |

メニュー **「残留ファイル」→「テスト：END1〜4を見たことにする」** で、トゥルーENDだけをすぐに試せます。

---

## 3. フォルダ構成

```
Assets/Zanryu/
├─ Scripts/
│  ├─ Core/      GameManager（進行・フラグ・エンド）, ChatSystem, SaveSystem, TextResolver, DebugTools
│  ├─ Data/      JSON のデータ型と読み込み
│  ├─ UI/        UIKit（コードで UI を組み立てる道具・色の設定）
│  ├─ OS/        偽OS：画面切り替え, ウィンドウ, デスクトップ, タスクバー, 偽カーソル
│  ├─ Apps/      エクスプローラー, メモ帳, 画像, 音声, チャット, タスクマネージャー, パスワード
│  └─ Horror/    イベント実行, グリッチ演出
├─ Editor/       メニュー「残留ファイル」
└─ Resources/
   ├─ Content/   ★シナリオ（ここを書き換えればゲームが変わる）
   │   ├─ files.json   ファイルとフォルダ
   │   ├─ chats.json   チャットの会話
   │   ├─ events.json  ホラー演出（いつ・何が起きるか）
   │   └─ config.json  全体設定・アカウント・プロセス・エンド文
   ├─ Images/    写真・壁紙（あとで追加）
   ├─ Audio/     効果音・環境音（あとで追加）
   ├─ Icons/     アイコン（あとで追加・なくても動く）
   └─ Fonts/     Main.ttf を置くとそのフォントになる
```

**シナリオはすべて JSON です。コードを触らずに、ファイル・会話・演出・エンドを追加できます。**

---

## 4. シナリオの書き方

### files.json（ファイル）
```json
{ "id": "diary_3", "parent": "docs", "type": "text", "name": "日記_0414.txt", "minDay": 3,
  "content": "きのう、だれかが「{LAST_FILE_YESTERDAY}」を開いてた。",
  "variants": [ { "fromDay": 5, "content": "日が進むとこの文に変わる" } ] }
```
| 項目 | 意味 |
|---|---|
| `parent` | 親フォルダのID。`desktop` ならデスクトップ、`root` ならPC直下 |
| `type` | `folder` / `text` / `image` / `audio` / `app` / `confirm` |
| `minDay` / `maxDay` | 何日目から何日目まで存在するか |
| `minEndings` | 見たエンド数がこれ以上で出現（**2周目以降の伏線**） |
| `requireFlag` / `forbidFlag` | フラグで出す・消す |
| `password` | パスワードロック |
| `hidden` | 隠しファイル |
| `setFlag` | 開いたときに立つフラグ |
| `asset` | 画像・音声の素材名（Resources/Images or Audio のファイル名、拡張子なし） |
| `variants` | 日数・エンド数・フラグで中身を差し替える |

### 文中で使えるトークン（プレイヤーの行動を日記に書かせる仕組み）
`{PC_NAME}` `{DAY}` `{TIME}` `{DATE}` `{LAST_FILE}` `{LAST_FILE_YESTERDAY}`
`{OPEN_COUNT}` `{OPEN_COUNT_YESTERDAY}` `{OPEN_COUNT_TOTAL}` `{MEMORY_MB}` `{ENDINGS}`

### chats.json（会話）
- `scripts`：何日目に、どの条件で、どのノードから会話が始まるか
- `nodes`：メッセージ本文。`choices` を書くと返信の選択肢になり、選ぶとフラグが立つ
- エンド判定に使うフラグ：`pretend_○`（ミサキのふり）、`seller_○`（出品者への連絡）

### monologues.json（主人公の独り言・RPG風メッセージ）
画面下のメッセージウィンドウに出ます。クリック / Space / Enter で送ります。
- `intros`：その日が始まったときのひとこと
- `reactions`：そのファイルを**初めて**開いたときの反応（`file` にファイルID）
- `hints`：しばらく何もしていないとき（`idle` 秒）に出るヒント。タスクバーの **「？ ヒント」** ボタンでいつでも見られる
- `lines` の1要素が1ページ。`\n` で改行
- ファイルを開くと `opened_ファイルID` というフラグが自動で立つので、ヒントの条件（`requireFlag` / `forbidFlag`）に使うと「次にやること」を案内できる
- `speaker` に名前を書くと名前欄が出る（空なら主人公の独り言）
- events.json の中でも `say`（読み終わるまで待つ）/ `sayAsync`（待たない）で出せる

### events.json（ホラー演出）
```json
{ "id": "d3_rename", "trigger": "fileOpened", "target": "photo_room", "day": 3, "delay": 2.5,
  "actions": [ { "type": "glitch", "f": 0.15 }, { "type": "rename", "a": "photo_room", "b": "みてる.jpg" } ] }
```
- **trigger**：`dayStart` / `fileOpened` / `flag` / `timer`
- **actions**：`flag` `rename` `glitch` `message` `toast` `open` `cursor` `chat` `wallpaper` `ambient` `sound` `closeAll` `unlock` `ending` `wait`
  （引数の詳細は `Scripts/Horror/HorrorEventSystem.cs` の先頭のコメント）

---

## 5. あとで素材を入れる場所

素材は **ファイル名を合わせて置くだけ** で自動的に差し替わります。

| 置き場所 | 現在のシナリオで使っている名前 |
|---|---|
| `Resources/Images/` | room_window, room_window_d2, misaki_room, misaki_room_d4, cafe, kmori_blur, yui_father |
| `Resources/Audio/` | ambient_fan（環境音・ループ）, yui_reverse（逆再生ボイス） |
| `Resources/Icons/` | folder, text, image, audio, confirm, explorer, chat, taskmgr, trash, cursor（任意） |
| `Resources/Fonts/` | Main.ttf（任意） |

何を撮ればいいかは、各画像ファイルの「撮影メモ」（files.json の `content`）に書いてあります。

---

## 6. 今後の拡張ポイント
- 偽ブラウザ（SNS・掲示板）アプリ → `Apps.cs` の `OpenApp` に追加
- 英語対応 → `Content.Load()` で `Content/en/` を読むように分岐
- Steam 実績 → `GameManager.TriggerEnding()` に Steamworks.NET の呼び出しを追加
- 見た目の調整 → `UIKit.cs` の `Theme`
