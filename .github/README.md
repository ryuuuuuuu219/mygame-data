# Discord Project Summary Workflow Memo

この `.github` フォルダは、GitHub Actions でプロジェクト要約を作り、Discord webhook へ自動投稿するための設定です。

## 他プロジェクトへ移動して使えるか

同じような構成のプロジェクトなら、`.github` フォルダを移動するだけでも大部分は使えます。

ただし、移動先プロジェクトに合わせて次の確認が必要です。

- GitHub Secrets に OpenAI API key と Discord webhook URL が入っているか
- `.github/scripts/post-project-summary.mjs` の `contextFiles` が移動先の資料パスに合っているか
- `.github/workflows/discord-project-summary.yml` の `paths` が移動先で監視したいファイルに合っているか
- 必要なら GitHub Variables の `OPENAI_MODEL` を移動先で使いたいモデル名に変更する

## OpenAI key と Discord webhook の変更は必要か

移動先リポジトリに同じ名前の Secrets が登録されていない場合は、変更または追加が必要です。

この workflow は次の Secret 名を見ます。

- OpenAI API key:
  - `OPENAI_API_KEY`
  - `OPENAI_API`
  - `OPENAI_KEY`
- Discord webhook:
  - `DISCORD_WEBHOOK_URL`
  - `DISCORD_WEBHOOK`

移動先リポジトリにも、上記のどれかの名前で Secret を登録してあれば、workflow ファイルを変更しなくても動きます。

## Secret をどう変更するか

GitHub の画面で変更する場合:

1. 移動先リポジトリを開く
2. `Settings` を開く
3. `Secrets and variables` → `Actions` を開く
4. `Repository secrets` に OpenAI API key と Discord webhook URL を追加または更新する
5. 推奨名は `OPENAI_API_KEY` と `DISCORD_WEBHOOK_URL`

既存の Secret 名を使いたい場合は、`.github/workflows/discord-project-summary.yml` の `env` を変更します。

例:

```yaml
env:
  OPENAI_API_KEY: ${{ secrets.MY_OPENAI_KEY }}
  DISCORD_WEBHOOK_URL: ${{ secrets.MY_DISCORD_WEBHOOK }}
```

## 要約に使う資料を変更する

要約対象の資料は `.github/scripts/post-project-summary.mjs` の `contextFiles` で指定しています。

移動先に `memo/README.md` や `document/README.md` がない場合は、移動先の `README.md`、設計メモ、タスク一覧などに置き換えてください。

例:

```js
const contextFiles = [
  "README.md",
  "docs/ROADMAP.md",
  "docs/tasks.md",
];
```

存在しないファイルは無視されますが、全部存在しないと要約の材料が少なくなります。

## workflow の発火対象を変更する

自動実行する対象パスは `.github/workflows/discord-project-summary.yml` の `paths` で指定しています。

Unity 以外のプロジェクトへ移す場合は、移動先に合わせて変更してください。

例:

```yaml
paths:
  - "src/**"
  - "docs/**"
  - "README.md"
  - ".github/workflows/discord-project-summary.yml"
  - ".github/scripts/post-project-summary.mjs"
```

手動実行は `workflow_dispatch` があるため、GitHub Actions の画面からいつでも実行できます。

## チュートリアル構成メモを要約に拾わせる

初心者向け導線の検討資料は `memo/原本/◇◇チュートリアル構成.txt` に置いています。

このメモには、実装前に参照したい次の内容があります。

- `preM00` シーン:
  - 操作確認用シーン。
  - 既存の入力可視化 UI を中央に表示する。
  - 操作説明テキストと入力 UI を線で結び、入力確認後に該当説明へ緑色アンダーラインを引く。
  - 全主要入力を一度押したら確認完了とし、`M00` へ進む。
- `M00` シーン:
  - ブリーフィング文は「チュートリアルです」程度の短い説明にする。
  - 操作説明用テキストを HUD に常時オーバーレイする。
  - マルチロックミサイル練習用に、UAV 発着場上空などへ案内テキストを置く。
- 操作説明:
  - 左スティック左右: ロール
  - 左スティック上下: ピッチ
  - 右スティック: 視点移動
  - `R1`: 加速
  - `L1`: 減速
  - `R2` / `L2`: ヨー
  - 左スティック押し込み + 減速: 機動制限解除
  - `△`: 目標切替
  - `○`: ミサイル / 選択兵装発射
  - `×`: 機銃
  - `□`: 兵装切替
- 関連ブレスト:
  - 機銃チュートリアル兼デバッファーとして、座標と機首方向を自身へ誘引する「引力場アドオン」を検討する。
  - マルチロックミサイル練習用には `Assets/script/mission/Manager/UAVStorageLauncher.cs` を使う。
  - 同時撃破まで無限湧き、スコア 0 の UAV 発着場設定を検討する。
  - その前提として、`memo/原本/予定表.txt` にある HP / スコア比改変、つまり HP と撃破スコアの分離を先に確認する。

Discord 要約 workflow にこの内容を直接拾わせたい場合は、`.github/scripts/post-project-summary.mjs` の `contextFiles` に次を追加します。

```js
"memo/原本/◇◇チュートリアル構成.txt",
```
