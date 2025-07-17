# DiffLens for Visual Studio

このプロジェクトは、VSCodeのDiffLens拡張機能と同等の機能を持つVisual Studio拡張機能です。

## 概要

DiffLens for Visual Studioは、AIを活用したgit diff コードレビューツールです。以下の機能を提供します：

- **AIコードレビュー**: AWS Bedrockを使用したインテリジェントなコード分析
- **Git統合**: ソリューション内のGitリポジトリとの自動統合
- **直感的なUI**: Visual Studioツールウィンドウでの使いやすいインターフェース
- **多言語対応**: 英語と日本語のインターフェース
- **設定管理**: Visual Studioの設定システムとの統合

## 主要機能

### 1. AIコードレビュー
- AWS Bedrock (Claude)を使用したコードレビュー
- カスタマイズ可能なシステムプロンプトとレビュー観点
- マークダウン形式での詳細なレビュー結果表示

### 2. Git統合
- 自動的なGitリポジトリ検出
- コミット間の差分表示
- ファイル拡張子フィルタリング
- 削除ファイルの除外オプション

### 3. ユーザーインターフェース
- **ツールウィンドウ**: メイン操作用の専用ウィンドウ
- **設定ページ**: Visual Studioの設定ダイアログとの統合
- **コマンド**: Tools メニューからの直接アクセス

## プロジェクト構造

```
DiffLens.VisualStudio/
├── Commands/                  # コマンドハンドラー
│   ├── ReviewCodeCommand.cs
│   ├── PreviewDiffCommand.cs
│   └── OpenSettingsCommand.cs
├── Models/                    # データモデル
│   ├── ReviewConfig.cs
│   ├── ReviewResult.cs
│   └── GitCommit.cs
├── Services/                  # ビジネスロジック
│   ├── ConfigurationService.cs
│   ├── GitService.cs
│   ├── DiffService.cs
│   ├── ReviewService.cs
│   └── LanguageService.cs
├── ToolWindows/              # UI コンポーネント
│   ├── DiffLensWindow.cs
│   ├── DiffLensWindowControl.xaml
│   └── DiffLensWindowControl.xaml.cs
├── Resources/                # リソースファイル
│   ├── icon.ico
│   └── DiffLensWindow.png
├── DiffLensPackage.cs        # メインパッケージクラス
├── DiffLensPackage.vsct      # コマンド定義
└── source.extension.vsixmanifest # 拡張機能マニフェスト
```

## ビルド要件

- Visual Studio 2022 (17.0以上)
- .NET 6.0 Windows
- Visual Studio SDK
- Visual Studio Extension Development ワークロード

## 設定

拡張機能をインストール後、以下の設定が必要です：

### AWS Bedrock設定
- AWS Access Key ID
- AWS Secret Access Key  
- AWS Region
- Model Name (例: anthropic.claude-3-5-sonnet-20241022-v2:0)

### プロンプト設定
- System Prompt: AIに送信するシステムプロンプト
- Review Perspective: レビューの観点・基準

### 差分設定
- Context Lines: 差分表示のコンテキスト行数
- Exclude Deleted Files: 削除ファイルを除外するかどうか
- File Extensions: 対象ファイル拡張子フィルター

## 使用方法

### 1. ツールウィンドウ経由（推奨）
1. `View` > `Other Windows` > `DiffLens` でツールウィンドウを開く
2. 設定を確認・調整
3. コミットを選択
4. `Preview Diff` でプレビュー確認
5. `Run Review` でAIレビュー実行

### 2. メニュー経由
1. `Tools` > `Review Code with AI` でレビュー実行
2. `Tools` > `Preview Git Diff` で差分プレビュー
3. `Tools` > `DiffLens Settings` で設定画面を開く

## 技術仕様

### アーキテクチャ
- **MVVM パターン**: WPFベースのUI設計
- **非同期処理**: async/await による応答性の確保
- **依存性注入**: サービスベースのアーキテクチャ
- **設定管理**: Visual Studio設定ストアとの統合

### 外部依存関係
- **AWS SDK**: Bedrock Runtime APIアクセス用
- **Newtonsoft.Json**: JSON処理用
- **Git**: コマンドライン経由でのGit操作

### 対応環境
- Windows 10/11
- Visual Studio 2022 Community/Professional/Enterprise
- .NET Framework 4.8 または .NET 6.0+

## VSCode版との互換性

この拡張機能は、VSCode版DiffLensと以下の点で完全互換です：

- **同じAIモデル**: 同じAWS Bedrockモデルを使用
- **同じ設定項目**: 設定パラメータが完全に対応
- **同じ出力形式**: レビュー結果のフォーマットが同一
- **同じGit統合**: 同じ差分抽出ロジック

## 開発者向け情報

### ビルド手順
```bash
# Visual Studio Developer Command Prompt で実行
msbuild DiffLens.VisualStudio.sln /p:Configuration=Release
```

### デバッグ実行
1. F5キーでExperimental Instanceを起動
2. ソリューションを開いてテスト
3. ブレークポイントでデバッグ

### VSIX配布
- `bin/Release` フォルダ内の `.vsix` ファイルを配布
- Visual Studio Marketplaceへの公開も可能

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## バージョン履歴

### v1.0.0
- 初回リリース
- AWS Bedrock統合
- 基本的なGit差分機能
- ツールウィンドウUI
- 英語・日本語対応
