# HRAAGUN 仕様・設計メモ

## 最終目的

HRAAGUN の最終目的は「躱せる対空砲」を作ること。

単に命中精度の高い対空砲を作るのではなく、プレイヤーが射線を見て、危険を判断し、機動で外せる余地を持つ兵装にする。

狙い:

- 高レートで圧を出す
- 完全な必中にはしない
- ランダムすぎて理不尽にも見せない
- 射線の揺らぎに「読める遅さ」を持たせる
- プレイヤーが回避行動を取った結果、弾幕を抜けられる状態を作る

このため、HRAAGUN の射線は `CalculateLeadDirection` に完全追従させず、偏差射撃方向を中心にしたバイアスをゆっくり動かす。

## 基本コンセプト

HRAAGUN は `hi rate aagun`、高レート対空砲。

通常の偏差射撃方向に対して、中心から少し外れた射線を持たせる。
その外れ方は瞬間的にランダム変更せず、円周上をゆっくり移動する。

イメージ:

```text
白ライン = CalculateLeadDirection
赤ライン = 白ライン + 現在のバイアス
```

白ラインは「本来当てたい方向」。
赤ラインは「実際に撃つ方向」。

プレイヤーにとっては、白ライン付近に弾が集中しつつも、赤ラインがゆっくり揺れるため、直進すれば危ないが、見て避ける余地がある。

## 配置仕様

M05 ではレールガン配置を基準に HRAAGUN を追加する。

- レールガンの直上 `y + 100` に配置
- 合計 3 基
- SpawnPrefabRegistry に `HR_AAGUN` を登録
- 生成後は `SetActive(true)` を確認
- 敵として `ObjectManager.Instance.RegisterEnemy` に登録

## 対象ファイル

- `Assets/script/mission/Enemy/Weapons/EnemyGunShooter.cs`
- `Assets/prefub/enemy/HR_AAGUN.prefab`
- `Assets/Scenes/M05.unity`
- `Assets/script/mission/Manager/SpawnTableManager.cs`

## 射線デバッグ

`showDebugAimLine` が true のとき、LineRenderer で射線を出す。

- 白ライン: `DebugLeadLine`
  - `CalculateLeadDirection` の結果
- 赤ライン: `DebugAimLine_01` など
  - 各バレルの現在射撃方向
  - `biasDirections[i]` を表示する

赤ラインは `biasMoveDirections` ではなく、現在の射線である `biasDirections` を表示する。

`ApplySpread` は実弾発射直前だけに乗せる。
デバッグ赤ラインには spread を含めない。

## パラメータ

```csharp
public bool useLeadBias;
public List<Vector3> biasDirections;
public List<Vector3> biasMoveDirections;
public float biasDegreeSpeed;
public float biasDegree;
```

意味:

- `useLeadBias`
  - HRAAGUN 用のバイアス射撃を有効化する
- `biasDirections[i]`
  - バレル `i` の現在射線
  - 赤ライン表示と実射撃に使う
- `biasMoveDirections[i]`
  - バレル `i` の現在の移動先射線
  - 表示用赤ラインには使わない
- `biasDegreeSpeed`
  - バイアスが移動先へ向かう角速度
  - 単位は degree/s
- `biasDegree`
  - 白ラインから赤ラインを外す角度

内部状態:

```csharp
biasOffsets[i]
biasMoveOffsets[i]
pendingBiasMoveOffsets[i]
biasAngles[i]
```

内部状態は、ワールド方向ではなく「白ライン基準の相対状態」として持つ。

## バイアスの基本仕様

元の狙い:

```text
CalculateLeadDirection で導出した方向に biasDirection を足す
```

実装上は、単純な `LeadDirection + biasDirection` ではなく、白ラインを中心にした相対オフセットとして扱う。

```text
赤ライン = RotateFromCenter(LeadDirection, biasOffset, biasAngle)
```

この設計にする理由:

- 白ラインが動いても、赤ラインが白ラインとの相対位置を保てる
- バイアスの移動速度を `biasDegreeSpeed` で制御しやすい
- 収束や毎フレーム再抽選のバグを切り分けやすい

## 動きの仕様

通常時:

```text
biasOffset が biasMoveOffset に向かって biasDegreeSpeed で移動する
```

毎フレームやること:

```text
1. CalculateLeadDirection で白ライン center を求める
2. biasOffset / biasAngle を biasDegreeSpeed で更新する
3. center + biasOffset + biasAngle から biasDirections を作る
4. 赤ラインと実弾方向に biasDirections を使う
```

毎フレームやらないこと:

- `biasMoveDirection` の再抽選
- バレルごとの状態初期化
- `i = 0` の処理で `i = 1, 2` の状態を消すこと

## 再抽選仕様

移動先に到達したら、次の `biasMoveDirection` を再抽選する。

再抽選条件:

```text
biasOffset が biasMoveOffset に到達した
```

または、160deg 以上の中心経由中に中心へ到達した。

抽選ルール:

- 抽選前と抽選後は、中心 `LeadDirection` から見て 45deg 以上の相対角を持たせる
- 160deg 以上離れた方向が選ばれた場合、直接反対側へ飛ばさない
- その場合は一度中心を通る

160deg 以上の処理:

```text
現在の biasOffset
↓
中心 LeadDirection
↓
pendingBiasMoveOffset
```

`pendingBiasMoveOffsets[i]` は、中心通過後に向かう最終移動先。
これを持たないと、全バレルの `biasMoveDirections` が `center` に寄って射線が収束する。

## 収束対策

確認された現象:

```text
biasMoveDirections が全バレルでほぼ同じ方向になる
赤ラインが中心へ収束する
```

原因:

- `biasMoveOffsets == Vector3.zero` のとき、`biasMoveDirections` が `center` そのものになる
- 160deg 以上で中心へ寄せたあと、反対側の移動先を保存しないと中心に残る
- 160deg 判定をワールド方向で見ると、中心から見た相対角の仕様とズレる

対策:

- 160deg 判定は中心から見た相対オフセット同士で行う
- 中心経由時は `pendingBiasMoveOffsets` に最終移動先を保持する
- 中心に到達したら pending 側へ出る

## 躱せる対空砲としての設計方針

HRAAGUN では、命中率だけでなく「回避可能性」を調整対象にする。

重要な観点:

- `biasDegree` が小さすぎると必中に近くなる
- `biasDegree` が大きすぎると狙っていない弾に見える
- `biasDegreeSpeed` が速すぎるとランダムで理不尽に見える
- `biasDegreeSpeed` が遅いとプレイヤーが射線を読める
- `fireRate` が高いほど、少しのバイアスでも弾幕圧が出る
- `spreadAngle` は最後のばらけであり、回避可能性の主制御にはしない

現時点の意図としては、HRAAGUN は「ランダム散布」ではなく「ゆっくり揺れる偏差射撃」にする。

プレイヤーが見るべき情報:

- 白ライン方向に撃ってくる危険
- 赤ラインの現在の偏り
- 赤ラインが急に跳ねず、一定速度で動くこと

これにより、プレイヤーは以下の判断ができる。

- 直進すると危ない
- 射線の外へ逃げれば抜けられる
- 赤ラインの動きから次の危険領域を予測できる

## barrelCount 対応

`barrelCount > 1` に対応する。

リスト化対象:

- `biasDirections`
- `biasMoveDirections`
- `debugAimLines`
- `biasOffsets`
- `biasMoveOffsets`
- `pendingBiasMoveOffsets`
- `biasAngles`

注意:

- 個別バレル処理中にリストを縮めない
- `EnsureLeadBias()` では不足分を増やすだけにする
- `EnsureBiasListSize(barrelIndex + 1)` のように呼ぶと、`i = 0` 処理で `i = 1, 2` が消えて毎フレーム再初期化される

このバグが起きると:

```text
i = 0: 状態が保持され、理想的に動く
i = 1, 2: 毎フレーム再初期化されて荒ぶる
```

## 実射撃への反映

実弾の発射方向は、赤ラインと同じ `biasDirections[i]` を使う。

流れ:

```text
CalculateLeadDirection
↓
GetBarrelDirection
↓
ApplyLeadBias
↓
biasDirections[i]
↓
ApplySpread
↓
bulletpool.bulletpull
```

`ApplySpread` は最終射撃方向だけに加える。
デバッグ赤ラインとは一致しない可能性があるが、それは spread による最後のばらけ。

## 代表値

HRAAGUN プレハブ側の想定値:

```text
barrelCount: 3
barrelArcAngle: 0
bulletSpeed: 260
fireRate: 0.05
useLeadBias: true
biasDegreeSpeed: 1
biasDegree: 8
showDebugAimLine: true
debugAimLineLength: 3000
```

## 調整方針

まずは以下を観察する。

- 白ラインは `CalculateLeadDirection` として自然に動くか
- 赤ラインは白ラインとの相対角を保つか
- `biasDegreeSpeed = 1` で赤ラインの相対バイアスがゆっくり動くか
- `biasDegreeSpeed = 0` でバイアス移動だけ止まるか
- 2本目以降のバレルが再初期化されていないか
- 160deg 以上の抽選時に中心へ寄ったあと、反対側へ出るか
- 実弾方向が赤ラインの意図と一致するか
- プレイヤーが見てから回避できるか

「躱せる対空砲」として怪しい場合の調整:

- 理不尽に当たる
  - `biasDegree` を上げる
  - `biasDegreeSpeed` を下げる
  - `bulletSpeed` を下げる
  - `fireRate` を少し下げる
- 当たらなすぎる
  - `biasDegree` を下げる
  - `bulletSpeed` を上げる
  - `fireRate` を上げる
- ランダムに見える
  - `biasDegreeSpeed` を下げる
  - 再抽選頻度を下げる
  - 160deg 中心経由の挙動を確認する
- 射線が収束する
  - `pendingBiasMoveOffsets` が保持されているか確認する
  - `biasMoveDirections` が全バレルで `center` になっていないか確認する

## 現時点の結論

HRAAGUN の中核は、命中補正ではなく「回避可能な危険領域」を作ること。

`CalculateLeadDirection` は白ラインとして正確な危険中心を示す。
`biasDirections` は赤ラインとして現在の実射撃方向を示す。
`biasMoveDirections` は赤ラインが次に向かう先を保持する。

赤ラインが毎フレームランダムに飛ぶ状態は避ける。
赤ラインが中心へ収束したままになる状態も避ける。

理想は、白ラインを中心に赤ラインがゆっくり乱反射し、プレイヤーが弾幕圧を感じながらも、見て躱せる対空砲。
