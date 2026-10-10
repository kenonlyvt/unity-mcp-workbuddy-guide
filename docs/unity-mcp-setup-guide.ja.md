# Unity MCP セットアップガイド（WorkBuddy / Cursor / Claude Code 共通）

> 対応バージョン：Unity 2021.3 LTS ～ 6.x ｜ MCP for Unity（CoplayDev 製、**Unity 公式ではありません**）v10.x
> 本ガイドは実際の Windows 環境での導入経験に基づき、公式ドキュメントに書かれていない「確実に踏む罠」に焦点を当てています。

---

## 0. まず押さえておくこと

- **MCP for Unity は Unity 公式のプラグインではありません。** コミュニティプロジェクト [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)（MIT ライセンス）です。2 つの部品からなります：
  - **Unity 側プラグイン** `com.coplaydev.unity-mcp`（C#、プロジェクトに導入）
  - **Python 側サーバー** `mcp-for-unity`（`uvx` で起動し、AI クライアントはこれ経由で Unity と通信）
- **2 つの転送方式**：
  - `stdio`（既定）：AI クライアントがサーバーを子プロセスとして起動します。**ブリッジは自動起動しません**（手動、またはスクリプトでの起動が必要。第 5 節参照）。
  - `HTTP`：Unity 側でローカル HTTP サーバーを稼働させ、ブリッジを自動起動できますが、追加のポート／設定が必要です。
- 本ガイドでは **WorkBuddy** を AI クライアントとし、**stdio** を例に説明します。他クライアント（Cursor / VS Code / Claude Code）は「クライアント設定」の節で共通です。

---

## 1. 前提環境：Python と uvx（一番つまずきやすい場所）

MCP の Python サーバーは `uvx`（uv のスクリプトランナー）に依存します。インストーラー（`Window > MCP for Unity > Auto-Setup`）は Python と uv/uvx を検出し、見つからないとずっと赤信号です。

### 1.1 Python + uv のインストール

- Python 3.10 以降をインストールし、「Add to PATH」にチェック。
- uv をインストール：`pip install uv`、または[公式 astral.sh/uv インストーラー](https://docs.astral.sh/uv/)を使用。`uvx --version` で確認。

### 1.2 Unity インストーラーに Python を「見せる」（Windows 固有の罠）

Unity の Auto-Setup は Python を硬直的に検出するため、`where python` で見つかる場合でも **「Python Not Found」** と出ることがよくあります。以下 3 か所を揃えます：

1. **レジストリ `InstallPath`**：`HKLM\SOFTWARE\Python\PythonCore\<バージョン>\InstallPath` が実際のパスを指しているか（複数バージョン環境では存在しないディレクトリを指していることがあります）。
2. **システム PATH**：Python のインストールディレクトリと `Scripts` ディレクトリを追加。
3. **ウィザードが硬 coded に探すディレクトリ**：インストーラーは `C:\Users\<あなた>\AppData\Local\Programs\Python` を探します。Python が別の場所（例 `E:\Python\Python313`）にある場合、**ディレクトリジャンクションを作成**してごまかします：

   ```powershell
   # 管理者として実行
   cmd /c mklink /J "C:\Users\Ken\AppData\Local\Programs\Python" "E:\Python\Python313"
   ```

   > `/J`（ジャンクション）を使い、`/D`（シンボリックリンク）は使いません。ジャンクションなら開発者モード／管理者権限なしで通常プロセスがたどれます。

その後 Unity を再起動すれば、ウィザードの Python 検出が緑になります。

---

## 2. AI クライアントでの MCP サーバー設定

WorkBuddy の場合、`~/.workbuddy/mcp.json` を編集（または新規作成）して以下を追加します：

```json
{
  "mcpServers": {
    "unityMCP": {
      "command": "uvx",
      "args": ["--from", "mcpforunityserver", "mcp-for-unity", "--transport", "stdio"]
    }
  }
}
```

- 初回実行時に `uvx` が `mcpforunityserver` を自動ダウンロードします（Unity 側パッケージと同バージョン、例：v10.3.0）。
- 保存して AI クライアントを再起動し、MCP 設定を再読み込みしてハンドシェイクを行います。成功するとサーバーが提供するツール一覧が表示されます（実測 **36 ツール**：シーン参照、GameObject の増減・変更、アセット作成、メニュー実行など）。

> 既に他の MCP サーバー（例：baota-mcp）がある場合は、**同じ `mcpServers` オブジェクトにマージ**してください。既存設定を上書きしないでください。

---

## 3. Unity プロジェクトへの `com.coplaydev.unity-mcp` 導入

### 方法 A：UPM の git 依存（オンライン、公式推奨）

プロジェクトの `Packages/manifest.json` の `dependencies` に追加：

```json
{
  "dependencies": {
    "com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git"
  }
}
```

保存すると Unity が git から自動で取得・コンパイルします。

### 方法 B：組み込みオフラインパッケージ（GFW 下やバージョン固定でより安定）

パッケージを `Packages/com.coplaydev.unity-mcp/`（ `Assets` や `ProjectSettings` と同階層）に展開します。`manifest.json` の変更は**不要**です。Unity は `Packages/` 配下のフォルダを自動でローカルパッケージとして読み込みます。

> 実践上、中国からの GitHub への git 依存はしばしばタイムアウトするため、**方法 B の方が安定**です。v10.3.0 の組み込みパッケージを Unity 6（`6000.3.25f1`）で検証済みです。

導入後、メニューバーに **`Window > MCP for Unity`** が表示され、ショートカットは `Ctrl+Shift+M`（Win/Linux）です。

---

## 4. Unity 6 の取得（中国ネットワークの罠）

中国リージョンの Unity Hub から国際版 Unity 6 をダウンロードすると、ほぼ確実に **CDN 404 / ダウンロード失敗** になります（中国 Hub は国内ミラーを使っており国際版インストーラーを配信できません）。

取り得る手段（いずれか）：

- **プロキシを使う**：ダウンローダーに HTTP プロキシ（例 `127.0.0.1:7897`）を設定し、unity.com から該当バージョンの `UnitySetup64-<バージョン>.exe` と Hub インストーラーを直接ダウンロードして手動インストール。
- **unity.com から直接ダウンロード**：国際版 Hub ＋ Editor インストーラーを取得し、手動でインストール。
- インストール時は **C ドライブの空き容量** に注意：Unity Editor ＋ モジュールは軽く 10GB 以上になります。C ドライブがいっぱいだと「Moving file failed」「Cannot save license」のような妙なエラーが出ます（第 6 節参照）。

> **「Cannot save the license file (Unity_lic.ulf)」** と表示された場合：古いライセンスファイルが権限ロック（管理者しか書き込めない）されているのが原因です。**管理者 CMD** で `C:\ProgramData\Unity\Unity_lic.ulf`（または該当ディレクトリの古いファイル）を削除してから再アクティベートしてください。

---

## 5. ブリッジの自動起動（stdio モード必須）

重要な罠：**stdio モードでは、プロジェクトを開いた時に Unity ブリッジが自動起動しません**（自動起動は HTTP モードのみネイティブ対応）。ブリッジが Stopped のままだと、AI クライアントは接続しても Unity を動かせません。

2 つの方法：

### 5.1 手動（毎回）

`Window > MCP for Unity` を開き、**Start Bridge** をクリックし、状態が `Running` になるのを待ちます。

### 5.2 スクリプトで自動起動（推奨）

本リポジトリの [`scripts/McpBridgeAutoStart.cs`](../../scripts/McpBridgeAutoStart.cs) をプロジェクトの `Assets/Editor/` に置きます。`[InitializeOnLoad]` によりエディター読み込み後に `McpCiBoot.StartStdioForCi()` を自動呼び出します：

```csharp
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
internal static class McpBridgeAutoStart
{
    static McpBridgeAutoStart()
    {
        EditorApplication.delayCall += () =>
        {
            try { MCPForUnity.Editor.McpCiBoot.StartStdioForCi(); }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning("[MCP AutoStart] 启动 stdio 桥接失败: " + e.Message);
            }
        };
    }
}
#endif
```

- `delayCall` で 1 フレーム遅延させ、ドメインリロード中の API 利用不可問題を回避しています。
- **手動起動に戻すには**：このファイルを削除するだけです。

接続されると `~/.unity-mcp/` 配下にステータスファイル（プロジェクト名・Unity バージョン・ポート番号など、例：6400）が生成され、AI クライアントは Unity が公開するツールを列挙できるようになります。

---

## 6. よくあるエラー早見表

| 現象 | 根本原因 | 対処 |
| --- | --- | --- |
| インストーラーの Python 検出が赤 | レジストリ / PATH / 検索ディレクトリが不揃い | 1.2 を参照、ジャンクションを作成 |
| `uvx: command not found` | uv 未インストールまたは PATH 非搭載 | uv を入れるか、インストーラーにパスを教える（Choose UV Install Location） |
| クライアントは繋がるが Unity を動かせない | ブリッジが Stopped（stdio は自動起動しない） | 第 5 節参照、自動起動スクリプトを入れるか手動 Start |
| Unity 6 ダウンロード 404 | 中国 Hub が国際版パッケージを取得できない | プロキシを使う / unity.com から手動ダウンロード |
| 「Moving file failed」/ インストールが進まない | C ドライブが満杯（一時フォルダの問題ではない） | Unity キャッシュ／ゴミ箱を削除するか空きを確保 |
| 「Cannot save license」 | 古い `Unity_lic.ulf` が権限ロック | 管理者 CMD で古いファイルを削除してから再アクティベート |
| ツール数 0 / 同期されない | サーバーとパッケージのバージョン不一致 | `mcpforunityserver` と `com.coplaydev.unity-mcp` のバージョンを揃える |

---

## 7. 応用：AI に MCP 経由でシーンを生成させる

MCP が動けば、AI クライアントは Unity の C# ツールを直接呼び出せます。本リポジトリの [`scripts/LotusPondBuilder.cs`](../../scripts/LotusPondBuilder.cs) がその例です：蓮池（葉・花・水面・浮遊花弁・石・葦）を手続き的に生成し、メニューに `Tools > Lotus Pond > Build All` を公開しています。

チャットでそのまま伝えます：

> 「私の Unity プロジェクトを開き、LotusPondBuilder の Build All を実行して、カメラを中心に向けた蓮池シーンを生成して。」

AI は MCP を通じて Unity 内で該当のメニュー／メソッドを実行します。手動でクリックする必要はありません。これが MCP for Unity の真の価値です：**エディター操作を AI が呼び出し・組み立て可能な能力に変える**こと。

---

## 8. 検証チェックリスト

- [ ] `uvx --version` が動き、Python がインストーラーに認識されている
- [ ] `~/.workbuddy/mcp.json`（または各クライアント設定）に `unityMCP` があり、再起動後にハンドシェイク成功
- [ ] Unity プロジェクトに `com.coplaydev.unity-mcp` が導入済み（git 依存または組み込み）
- [ ] `Window > MCP for Unity` で Server Status = Installed、Bridge = Running
- [ ] AI クライアントが Unity ツールを列挙できる（例：`mcpUnity__*` / `unityMCP__*`）
- [ ] 実際の呼び出し（メニュー実行、GameObject 作成）が成功

以上が揃えば、あなたの AI クライアントは Unity エディターを本当に「支配」しています。楽しんでください 🪷
