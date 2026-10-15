# UniGitHistoryViewer

[English](README.md)

UniGitHistoryViewer は、Projectウィンドウで選んだファイルやフォルダのGit履歴を表示するUnityエディタ拡張です。
誰が・いつ・なぜアセットを変更したのかを、Gitクライアントに切り替えずに確認できます。

![Git Historyウィンドウ](https://raw.githubusercontent.com/baobao/UniGitHistoryViewer/master/Docs/images/screenshot-main.png)

- Unityエディタの中だけで動きます。ビルドには何も含まれません。
- パソコンにインストール済みのGitを使って履歴を読みます。Gitは同梱していません。
- 読み取り専用です。コミット、チェックアウトなど、リポジトリを書き換える操作は一切しません。
- 無料のオープンソースです（MITライセンス）。

## 動作要件

- Unity 2022.3 以降
- パソコンにGitがインストールされていること（<https://git-scm.com/>）
- Unityプロジェクト（またはローカルパッケージ）がGitリポジトリの中にあること

## インストール

次のどれか1つの方法でインストールします。パッケージ名（`info.shibuya24.uni-git-history-viewer`）が同じなので、1つのプロジェクトで併用しないでください。

### Unity Asset Store

1. Unity Asset Storeでパッケージを入手します。
2. Unityで **Window > Package Manager** を開きます。
3. **My Assets** から **UniGitHistoryViewer** を選び、**Install** を押します。

### Git URL

1. **Window > Package Manager** を開きます。
2. **+** を押して **Install package from git URL...** を選びます。
3. 次のURLを入力します。

```
https://github.com/baobao/UniGitHistoryViewer.git?path=Assets/UniGitHistoryViewer
```

`Packages/manifest.json` の `dependencies` に次の1行を追加しても同じです。

```json
"info.shibuya24.uni-git-history-viewer": "https://github.com/baobao/UniGitHistoryViewer.git?path=Assets/UniGitHistoryViewer"
```

## 使い方

### ウィンドウを開く

- Projectウィンドウでファイルかフォルダを右クリックし、**Show Git History** を選ぶ
- またはメニューバーの **Tools > UniGitHistoryViewer** を選ぶ

ウィンドウのタイトルは **Git History** です。ツールバーには、対象のアセット、読み込んだコミット数、検索欄、**Max** 欄、再読み込みボタンが並びます。

### 選択したアセットに追従する

ウィンドウを開いている間は、Projectウィンドウで選んだアセットの履歴を表示します。
HierarchyでGameObjectを選んでも、表示対象は変わりません。

### ウィンドウをロックする

ウィンドウのタブにある鍵アイコンを押すと（タブのメニューの **Lock** でも可）、ほかのアセットを選んでも今の表示を保ちます。
もう一度押すとロックが外れ、その時点で選んでいるアセットの履歴に切り替わります。

ツールバーのアセット名を押すと、Projectウィンドウでそのアセットの位置を示します。

### コミット情報を確認する

コミット一覧には **Message**（件名）、**Author**（作者）、**Date**（日時）、**Commit**（短いハッシュ）の列があります。
列幅を変えられるほか、列見出しの右クリックメニューから **Author**、**Date**、**Commit** を非表示にできます。
矢印キーでコミットを移動できます。

コミットを選ぶと、下のペインに次の情報が出ます。

- 件名とコミットメッセージの本文全体
- 作者名とメールアドレス
- 日時（「3 days ago」のような相対表示つき）
- コミットハッシュ全体（マージコミットは **Merge** と表示）
- 変更されたファイル

一覧と詳細の境界をドラッグすると、それぞれの高さを変えられます。

### コミットを検索する

ツールバーの検索欄に入力すると、件名・本文・作者名にその文字列を含むコミットと、ハッシュがその文字列で始まるコミットに絞り込みます。大文字と小文字は区別しません。

### コミットハッシュをコピーする

- 詳細のハッシュの横にある **Copy** を押す
- コミット一覧にフォーカスがある状態で **Ctrl+C**（macOSは **Cmd+C**）を押す
- コミットを右クリックして **Copy Commit Hash**、**Copy Short Hash**、**Copy Message** を選ぶ

### 変更ファイル

詳細には、選んだコミットで変更されたファイルがステータスのバッジつきで並びます。

| バッジ | 意味 |
|---|---|
| A | 追加 |
| M | 変更 |
| D | 削除 |
| R | 名前の変更（`旧パス → 新パス` と表示） |
| C | コピー |

選んでいるアセット（フォルダの場合はその中のファイル）は強調表示されます。
ファイルを押すと、Projectウィンドウでそのファイルの位置を示します。すでに存在しないファイルや、Unityプロジェクトの外にあるファイルはグレーで表示され、押せません。

マージコミットでは、1つ目の親との差分を表示します。

### 名前を変える前の履歴

ファイルを1つ選んだ場合は、名前の変更をさかのぼって履歴を追います（`git log --follow`）。古いファイル名のときのコミットも一覧に出ます。
フォルダの場合は名前の変更を追いません。

### 読み込むコミット数

**Max** で読み込むコミット数を指定します（初期値100、最大10000）。入力後にEnterで反映されます。再読み込みボタンで履歴を読み直します。

### ローカルパッケージ

ローカルパッケージ（`file:` パスでインストールしたもの）や埋め込みパッケージのアセットも、パッケージのフォルダがGitリポジトリの中にあれば同じように選べます。
レジストリやGit URLからインストールしたパッケージは `Library/PackageCache` に置かれ、リポジトリの管理外なので履歴はありません。

## 仕組み

履歴の読み込み中にエディタが固まらないよう、次の読み取り専用のGitコマンドをバックグラウンドのスレッドで実行します。

- `git rev-parse --show-toplevel --show-prefix`
- `git log`（ファイルを1つ選んだ場合は `--follow` つき）
- `git diff-tree --name-status`

Gitは、環境変数 `PATH` に書かれた絶対パスの中から探します。macOSでは代表的なインストール先（`/opt/homebrew/bin`、`/usr/local/bin`、`/usr/bin`）、Windowsでは `C:\Program Files\Git\cmd` も確認します。
ネットワークへの接続やデータの送信は行いません。

## トラブルシューティング

| メッセージ・症状 | 対処 |
|---|---|
| 「Git was not found on this computer.」 | Gitをインストールし、Unityを再起動してください。別の場所にインストールしている場合は `PATH` に追加し、Unity HubとUnityを再起動してください。 |
| 「The selected asset is not inside a Git repository.」 | プロジェクトやパッケージのフォルダがGitで管理されていません。`git init` するか、リポジトリ内のプロジェクトを開いてください。 |
| 「No commits found for this path.」 | そのアセットが一度もコミットされていないか、`.gitignore` で除外されています。コミットすると履歴が出ます。 |
| パッケージのアセットに履歴がない | `Library/PackageCache` のパッケージ（レジストリ・Git URLからのインストール）はリポジトリの管理外です。 |
| フォルダの履歴が名前の変更で途切れる | 名前の変更を追うのはファイル単位だけです。フォルダ内のファイルを選んでください。 |
| 読み込みに時間がかかる | **Max** を減らしてください。大きなリポジトリで長い履歴の名前変更を追うと時間がかかることがあります。 |
| コミットメッセージやパスが文字化けする | コミットがUTF-8で保存されているか確認してください。本ツールはGitにUTF-8での出力を指定しています。 |

## サードパーティソフトウェア

このパッケージにはサードパーティのコードや素材は含まれていません。Gitは同梱しておらず、パソコンにインストール済みのGitを使います。

## ライセンス

[MIT License](LICENSE)

## 作者

ohbashunsuke — <https://ohbashunsuke.com/>
