# 2-back課題システム（視野外確認ロックタスク付き）引継ぎ仕様書

本ドキュメントは，PC側で動作する2Dアプリ「2-back課題＋視野外確認ロック解除タスク」の実装方針をまとめたものです．HMD側の視野拡張システム（[c:\Users\S2PC\ViewExpansion](c:\Users\S2PC\ViewExpansion)）とは**完全に独立したUnityプロジェクト**であり，アプリ間の通信・ログ同期は不要です．

詳細な実験計画（仮説・評価指標・被験者数・分析計画等）は，堀部青夏「視点移動情報に基づく視野外への視覚的関心方向推定を用いたHMD視野拡張システム」実験計画書を参照してください．本ドキュメントはその実装に必要な要点のみを抜粋しています．

---

## 1. システム全体像

```
+-------------------------------------------------------------+
|              HMD側アプリ（別プロジェクト，起動したまま）      |
|  - 視野外映像の提示（条件Aのみアクティブ／条件Bは非アクティブ）|
|  - Picoシースルー（条件Bのみ ON）                            |
|  - 視線データ・頭部回転角・MLモデル推定の記録（両条件共通）   |
|  - このプロジェクトとは通信しない                            |
+-------------------------------------------------------------+

+-------------------------------------------------------------+
|         PC側2Dアプリ（このプロジェクト，本アプリ）           |
|  - 2-back課題の進行・マウス回答受付                          |
|  - 3回答ごとのロック発生・解除制御                           |
|  - エリア指示表示・色回答パレット（マウス操作）               |
|  - lock_log.csv / twoback_log.csv の出力                    |
+-------------------------------------------------------------+
```

- 条件A（提案手法）／条件B（対抗手法）のどちらであるかは，**実験者が起動画面で選択**する（HMD側の設定と実験者が手動で同期させる．自動連携はしない）．
- 参加者はHMD装着状態でPCモニタに向かい，マウスで本アプリを操作する．

---

## 2. 開発環境

- Unity **2021.3.22f1**（HMD側プロジェクトと合わせる）
- Built-in Render Pipeline
- UI: **Screen Space Canvas**（World Spaceではないため，スケール較正の問題は発生しない）
- TextMeshPro使用．日本語テキスト（教示文・エリア指示・色名）を表示するため，**日本語対応のSDFフォントアセット（例：Noto Sans JP）を明示的に生成・割り当てること**．デフォルトフォント（Liberation Sans SDF）は日本語グリフを持たず文字化けする（HMD側プロジェクトで実際に発生した不具合）．

---

## 3. 画面・状態遷移

```
[起動画面]
  参加者ID入力／条件選択(proposed|baseline)／エリア順序セット選択
        ↓
[練習セッション]（2-back×3回答＋エリア確認×4，ログは本番と分離）
        ↓（実験者が操作方法の理解を確認し，本試行開始を操作）
[本試行]
  2-back課題 ⇄ ロック解除タスク を8エリア分繰り返す
  （2-back 3回答ごとに自動でロック発生）
        ↓（8エリア完了で自動終了）
[終了画面]（ログ保存完了の表示）
```

- 起動画面は必須．ログファイル名に参加者ID・条件・日付を含める仕様のため．
- 練習中のログは本番ログに混在させない（別ファイル，または保存しない）．

---

## 4. 実装するクラス（方針）

HMD側プロジェクトで先行実装した経験を踏まえ，VR依存部分（XR Interaction Toolkit，World Space較正，Accepter連携）を除いた素直なuGUI構成で実装する．

| クラス | 役割 |
|---|---|
| `ExperimentSetupController` | 起動画面：参加者ID・条件・エリア順序セットの入力受付 |
| `TwoBackTaskController` | 2-back課題本体．数字提示（1〜2秒間隔）・マウスボタンでの「同じ／異なる」回答・反応時間計測 |
| `LockTaskController` | ロック解除タスク．`protocolSequence`（エリアID×正解色，8件，参加者ごとに順序が異なる決め打ちシーケンス）に従い進行．8色パレットのマウス回答 |
| `ExperimentSessionController` | 2-back 3回答ごとのロック発生／8エリア完了での本試行終了を管理する状態機械 |
| `ExperimentLogger` | lock_log.csv／twoback_log.csv／session.csv の出力（下記命名規則・スキーマに従う） |

コルーチン内での早期リターンは`return;`ではなく`yield break;`を使うこと（CS1622エラー防止，HMD側プロジェクトの開発ルールを踏襲）．

---

## 5. ロック発生条件

- **時間ベースではなく，2-back 3回答ごとに固定でロック発生**（HMD側の実験用アプリで一度実装した「時間ベース」設計とは異なるので注意）．
- 8エリア分のロックタスクをすべて完了した時点で1条件の本試行を終了する．

---

## 6. エリア順序のカウンターバランス

- 各エリアに貼られた物理カラーカードの色（`areaId → correctColorIndex`）は条件間で不変（実環境の配置が固定のため）．
- **エリアを確認させる順序（8エリアの訪問順）は参加者ごとに異なる固定シーケンス**とし，起動画面でどの順序セットを使うか選択できるようにする（例：`SequenceSet_1`〜`SequenceSet_N`をScriptableObjectまたはJSONで事前定義）．
- ランダム化はしない．すべて実験プロトコルとして決め打ちにする．

---

## 7. ログ設計

### ファイル命名規則
```
{参加者ID}_{condition}_{日付YYYYMMDD}_{ログ種別}.csv
```
例：`P001_proposed_20250901_lock.csv`

### `..._lock.csv`（ロックタスク，不正解時も1行ずつ記録）
```
TrialIndex, AreaId, TargetColorIndex, AnsweredColorIndex, Correct, LockOnsetTimestampMs, AnswerTimestampMs, ResponseTimeMs
```

### `..._twoback.csv`（24行＝3回答×8エリア）
```
Timestamp, Digit, TargetDigit, UserAnswer, Correct, ReactionTimeMs, TrialAreaIndex
```

### `..._session.csv`（推奨，必須ではない）
```
SessionStartMs, SessionEndMs, TotalDurationMs
```
H1（3×8試行全体の完了時間）をCSVから再計算しなくて済むようにするための補助ログ．

- 既存の`EvaluationManager.cs`のCSV書き出しパターン（`List<string>`に蓄積 → `File.WriteAllLines`，UTF8）を踏襲してよい．
- 出力先は`Application.persistentDataPath`よりも，実験者が回収しやすい場所（exe横のフォルダ，またはInspectorで指定可能なパス）を推奨．
- 実装上の出力先：`Application.dataPath`の親ディレクトリ直下の`Logs`フォルダ（Editor実行時はプロジェクト直下，ビルド後はexeと同階層）．`.gitignore`で除外済み．

### 各列の詳細（実装ベース）

**`participantID`はCSVの列としては存在しない．ファイル名の先頭部分がそのまま参加者IDである点に注意．**

#### `..._twoback.csv` / `..._practice_twoback.csv`

| 列名 | 内容 |
|---|---|
| Timestamp | 参加者が「Same/Different」をクリックした瞬間の時刻（Unixエポックからのミリ秒，ローカル時刻基準）．数字が表示された瞬間ではなく回答した瞬間の時刻 |
| Digit | このトライアルで表示された数字（1〜9） |
| TargetDigit | 比較対象（NBackDistance個前に表示された数字）．N=1なら1つ前，N=2なら2つ前の数字 |
| UserAnswer | クリックした内容．`Same`または`Different`（タイムアウト仕様は廃止済みのため`NoResponse`は出力されない） |
| Correct | 正誤（True/False）．DigitとTargetDigitが実際に同じかどうかとUserAnswerが一致していればTrue |
| ReactionTimeMs | 数字が表示されてからクリックするまでの反応時間（ミリ秒）．ゲーム内の高精度タイマー基準で，Timestampとは別系統の計測値 |
| TrialAreaIndex | この試行が何番目のエリア訪問ブロックで起きたか（0〜7）．**物理的なエリア番号（AreaId）ではなく，訪問順のインデックス**．練習中は常に`-1` |

#### `..._lock.csv` / `..._practice_lock.csv`

| 列名 | 内容 |
|---|---|
| TrialIndex | 何番目のロック解除タスクか（0〜7，訪問順インデックス）．練習中は常に`0` |
| AreaId | 実際にチェックした物理的なエリア番号（0始まり．画面表示は`AreaId+1`）．物理カラーカードの配置と対応 |
| TargetColorIndex | そのエリアの正解色（`AreaColorConfig`のパレット内インデックス，既定は0=Red,1=Blue,2=Green,3=Yellow,4=Purple,5=Orange,6=Cyan,7=Pink） |
| AnsweredColorIndex | 参加者がクリックした色のパレットインデックス |
| Correct | AnsweredColorIndexとTargetColorIndexが一致すればTrue |
| LockOnsetTimestampMs | ロックが発生し「Please check Area N」が表示された瞬間の時刻（ミリ秒） |
| AnswerTimestampMs | この行のクリックが行われた瞬間の時刻（ミリ秒） |
| ResponseTimeMs | ロック発生からこのクリックまでの経過時間（ミリ秒）．前回の誤答からではなく，常にロック開始時点からの累積時間 |

**正解するまでロックは解除されない仕様のため，誤答すると同じTrialIndex/AreaIdのまま複数行記録される**（AnsweredColorIndexとCorrectだけが変わる）．実際の解除所要時間は，そのTrialIndexで`Correct=True`になっている行の`ResponseTimeMs`を見る．

#### `..._session.csv`（本試行のみ，1行のみ）

| 列名 | 内容 |
|---|---|
| SessionStartMs | 「Start Main Trial」ボタンを押した瞬間の時刻（ミリ秒） |
| SessionEndMs | 8エリア目のロック解除が完了した瞬間の時刻（ミリ秒） |
| TotalDurationMs | SessionEndMs − SessionStartMs．H1（3×8試行全体の完了時間）そのもの |

#### 補足：TrialAreaIndex／TrialIndexと物理エリア番号の対応

`twoback.csv`の`TrialAreaIndex`と`lock.csv`の`TrialIndex`は同じ意味（0〜7の訪問順インデックス）．実際の物理エリア番号に変換したい場合は，その参加者に割り当てた`SequenceSetLibrary`の`areaVisitOrder`配列を参照するか，同じ訪問順インデックスを持つ`lock.csv`の`AreaId`列と突き合わせる．

---

## 8. スコープ外（このプロジェクトでは扱わない）

- 頭部累積回転角（H4，`hmd_log.csv`）の記録：HMD側プロジェクトの担当（別タスク，未実装）．
- NASA-TLX／SSQ等の質問紙：Googleフォーム等，本アプリ外で実施．
- HMD側アプリとの通信・ログ同期：不要（両アプリは完全独立）．
