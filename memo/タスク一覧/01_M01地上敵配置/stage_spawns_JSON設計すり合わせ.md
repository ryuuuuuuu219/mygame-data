# stage_spawns.json 設計すり合わせ

## 目的

M01 地上敵配置に向けて、`Assets/StreamingAssets/stage_spawns.json` の敵スポーン定義を見直す。

現状は `enemyIds`、`isMissionTarget`、`lifetimes` が別々の配列になっており、同じ index を参照する前提になっている。
M01 では AA_GUN、SAM、地上目標、脅威だが破壊必須ではない防空戦力が混在するため、敵ごとの情報をひとまとまりにした形式へ寄せたい。

## 現状形式

```json
{
  "stages": [
    {
      "sceneName": "M01",
      "spawns": [
        {
          "WaveId": 0,
          "enemyIds": [0, 1, 2],
          "isMissionTarget": [true, false, true],
          "lifetimes": [0, 0, 45],
          "triggerTargetWaveId": -1
        }
      ]
    }
  ]
}
```

### 現状形式の意味

| 項目 | 意味 |
| --- | --- |
| `stages` | ステージ定義の配列。 |
| `sceneName` | Unity のシーン名。現在のシーン名と一致する定義が使われる。 |
| `spawns` | Wave 定義の配列。 |
| `WaveId` | Wave 番号。旧形式名。`SpawnTableManager` では `waveId` に正規化される。 |
| `enemyIds` | `SpawnTableManager.enemies` リスト上の敵 index。 |
| `isMissionTarget` | `enemyIds` と同じ順番で、各敵がミッション目標かどうかを表す。 |
| `lifetimes` | `enemyIds` と同じ順番で、各敵の生存時間を表す。`0` 以下なら無制限。 |
| `triggerTargetWaveId` | 指定 Wave の目標が全滅したら開始する旧形式の開始条件。`-1` は条件なし。 |

### 現状形式の問題

- `enemyIds`、`isMissionTarget`、`lifetimes` が index 依存なので、敵の追加・削除・並べ替えで指定がずれやすい。
- M01 のように敵数が多いステージでは、どの敵が AA_GUN、SAM、地上目標なのか JSON だけでは読み取りにくい。
- 「破壊必須のミッション目標」と「攻撃してくるが必須ではない防空戦力」を分けにくい。
- 配置座標、地形へのスナップ、ランダム配置条件、固定 seed などを自然に追加しにくい。

## 推奨形式

敵 1 体ごとの定義を `enemies` 配列にまとめる。

```json
{
  "stages": [
    {
      "sceneName": "M01",
      "randomSeed": 10101,
      "spawns": [
        {
          "waveId": 0,
          "requireClearedWaves": [],
          "enemies": [
            {
              "enemyId": 0,
              "prefabType": "AA_GUN",
              "missionTarget": false,
              "lifetime": 0,
              "placement": {
                "mode": "terrainRandom",
                "count": 3,
                "areaId": "ridge_aa_01",
                "minAltitude": 120,
                "maxAltitude": 260,
                "terrainLayer": "Terrain"
              }
            },
            {
              "enemyId": 1,
              "prefabType": "SAM",
              "missionTarget": true,
              "lifetime": 0,
              "placement": {
                "mode": "fixed",
                "count": 1,
                "position": { "x": 420, "y": 0, "z": -180 },
                "isstoped": true,
                "vector": { "x": 0, "y": 0, "z": 0 },
                "rotate": { "x": 0, "y": 90, "z": 0 },
                "snapToTerrain": true,
                "areaId": "valley_sam_01"
              }
            }
          ]
        }
      ]
    }
  ]
}
```

## 推奨形式の項目説明

### Stage

| 項目 | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `sceneName` | string | 必須 | Unity のシーン名。現在のシーン名と一致するステージ定義を使う。 |
| `randomSeed` | int | 任意 | ステージ内のランダム配置を再現するための seed。M01 では配置ばらつきと対空陣地の場所を固定するために使う。 |
| `spawns` | array | 必須 | Wave 定義の配列。 |

### Wave

| 項目 | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `waveId` | int | 必須 | Wave 番号。実行時の `waveID` として敵に設定される。 |
| `requireClearedWaves` | int array | 任意 | この Wave の開始前にクリア済みである必要がある Wave 番号の配列。空配列なら即時開始。 |
| `enemies` | array | 必須 | この Wave で有効化する敵の定義配列。 |

### Enemy

| 項目 | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `enemyId` | int | 必須 | `SpawnTableManager.enemies` リスト上の敵 index。既存の Scene 配置済み GameObject を有効化する場合に使う。 |
| `prefabType` | string | 任意 | 敵種別の識別名。例: `AA_GUN`, `SAM`, `GROUND_TARGET`, `AIRCRAFT`。当面は確認用メモとして使い、将来プレハブ生成式にする時のキーにできる。 |
| `missionTarget` | bool | 必須 | この敵を撃破必須のミッション目標として扱うか。`AugumentStatus.missionObjective` に反映する。 |
| `lifetime` | float | 任意 | 生存時間。`0` 以下なら無制限。既存の `AugumentStatus.lifeTime` に対応。 |
| `placement` | object | 任意 | 配置ルール。固定座標、地形上ランダム、エリア指定などをまとめる。 |

### Placement

| 項目 | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `mode` | string | 必須 | 配置方式。`fixed` は固定座標、`terrainRandom` は地形上のランダム配置。 |
| `count` | int | 任意 | この配置定義から生成・有効化する数。`2` 以上なら同じ敵定義をグループとして複製する。未指定時は `1`。 |
| `position` | object | `fixed` で使用 | 固定配置の座標。`x`, `y`, `z` を持つ。`snapToTerrain` が true の場合、`y` は地形高度で上書きしてよい。 |
| `isstoped` | bool | 任意 | 駐機中など、初速を持たない敵を表す。true の場合は `vector` を `Vector3.zero` として扱う。綴りは実装上 `isstoped` で統一する。 |
| `vector` | object | 任意 | 初期速度ベクトル。主に航空機のスポーン直後の速度を想定する。`x`, `y`, `z` を持つ。 |
| `rotate` | object | 任意 | 初期回転。主に航空機の離陸方向や、地上兵器の向きを指定する。`x`, `y`, `z` を Euler 角として扱う。 |
| `snapToTerrain` | bool | 任意 | true なら地形コライダー上に接地させる。 |
| `areaId` | string | 任意 | 配置エリアの識別名。例: `ridge_aa_01`, `valley_sam_01`。後でエリア定義を別途持たせる前提のラベルとして使える。 |
| `minAltitude` | float | 任意 | 配置可能な最低高度。M01 の AA を一定高度以上に置くために使う。 |
| `maxAltitude` | float | 任意 | 配置可能な最高高度。高所 AA、谷底 SAM などの配置制限に使う。 |
| `terrainLayer` | string | 任意 | Raycast で参照する地形レイヤー名。例: `Terrain`。 |
| `radius` | float | 任意 | ランダム配置時の探索半径や、エリア中心からのばらつき幅として使う。 |

## M01 での使い分け案

### M01 敵配置メモ

- AA: `3 * 12` グループ、合計 36 体。
- SAM: `2 * 9` グループ、合計 18 体。
- すべてミッションターゲットとして扱う。
- AA と SAM を複合した対空陣地は、M02 以降のゲームデザイン候補として残す。

### AA_GUN

- `prefabType`: `AA_GUN`
- `missionTarget`: M01 では `true`
- `placement.mode`: `terrainRandom`
- `placement.count`: 陣地ごとの配置数
- `minAltitude`: 一定高度以上
- `snapToTerrain`: true
- 用途: 山肌・尾根・高台に配置し、低空侵入だけでは安全になりすぎないようにする。

### SAM

- `prefabType`: `SAM`
- `missionTarget`: M01 では `true`
- `placement.mode`: `fixed` または `terrainRandom`
- `placement.count`: 通常は `1`。SAM 群としてまとめる場合は `2` 以上も可
- `areaId`: 谷底エリアを指定
- `snapToTerrain`: true
- 用途: 谷底や進路上の防空陣地として配置し、高高度侵入に圧をかける。

### AIRCRAFT

- `prefabType`: `AIRCRAFT`
- `placement.position`: 初期位置
- `placement.rotate`: 離陸方向または初期進行方向
- `placement.vector`: 初期速度
- `placement.isstoped`: 駐機状態なら `true`
- 用途: 滑走路上の駐機敵、離陸直後の敵、空中スポーン敵を同じ形式で扱う。

### GROUND_TARGET

- `prefabType`: `GROUND_TARGET`
- `missionTarget`: `true`
- 用途: 破壊必須の地上目標。防空戦力とは別にクリア条件として扱う。

## 移行方針

いきなり全ステージを新形式へ変えるのではなく、`SpawnTableManager` で新旧両対応にする。

`SpawnTableManager` は JSON 読み込み、Wave 開始条件、Wave クリア判定を担当する。
JSON から読み取った敵定義を実際の GameObject 配置に変換する処理は、別コンポーネントの `SpawnPlacementManager` が担当する。

### コンポーネント分担

| コンポーネント | 役割 |
| --- | --- |
| `SpawnTableManager` | `stage_spawns.json` の読み込み、ステージ選択、Wave 定義保持、Wave 開始・終了判定、新旧形式の振り分け。 |
| `SpawnPlacementManager` | 敵 GameObject の有効化、`prefabType` による prefab 生成、`count` による複製、`position` / `rotate` / `vector` / `isstoped` / `snapToTerrain` の適用、`ObjectManager` への登録。 |
| `SpawnPrefabRegistry` | `prefabType` 名と prefab の対応表。現状は M01/M02 の `SpownTable` に `AA_GUN` と `SAM` を登録する。 |

### JSON項目の接続状況

| 項目 | 状況 | 備考 |
| --- | --- | --- |
| `prefabType` | 接続済み | `SpawnPrefabRegistry` から prefab を取得し、`SpawnPlacementManager` が生成する。 |
| `enemyId` | 接続済み | `prefabType` が未指定または未登録の場合の旧式フォールバックとして使う。 |
| `missionTarget` | 接続済み | `AugumentStatus.missionObjective` と Wave の `aliveTarget` に反映。 |
| `lifetime` | 接続済み | `AugumentStatus.lifeTime` に反映。 |
| `count` | 接続済み | 同じ定義から複数体を生成する。 |
| `position` | 接続済み | 初期座標として反映。 |
| `rotate` | 接続済み | 初期回転として反映。 |
| `isstoped` | 接続済み | true の場合、初速を `Vector3.zero` にする。 |
| `vector` | 接続済み | 初期速度として `Rigidbody` / `AugumentStatus` / `AircraftController` に反映。 |
| `snapToTerrain` | 接続済み | true の場合、上空から Raycast して地形上に置く。 |
| `terrainLayer` | 接続済み | `snapToTerrain` の Raycast 対象 Layer として使う。 |
| `requireClearedWaves` | 接続済み | Wave 開始条件として使う。 |
| `randomSeed` | 接続済み | ステージ内の `terrainRandom` 配置の固定乱数として使う。M01 は `260514`。 |
| `mode` | 接続済み | `terrainRandom` の場合、`position` を中心に `radius` 内でランダム抽選する。 |
| `areaId` | ラベル扱い | 現状は処理には使わず、配置意図を読むための識別名として残す。 |
| `minAltitude` | 接続済み | `terrainRandom` 抽選後、地形上に置いた y が下限未満なら再抽選する。 |
| `maxAltitude` | 接続済み | `terrainRandom` 抽選後、地形上に置いた y が上限超過なら再抽選する。 |
| `radius` | 接続済み | `terrainRandom` の XZ 抽選半径として使う。M01 は全グループ `15`。 |

### M01 新形式生成状況

- M01 は `stage_spawns.json` 上で新形式へ移行済み。
- `randomSeed`: `260514`
- AA_GUN: 12 グループ * 3 体 = 36 体。
- SAM: 9 グループ * 2 体 = 18 体。
- 合計 54 体、すべて `missionTarget: true`。
- M01 シーンの `SpownTable.enemies` は空にし、旧航空機などのシーン配置敵は起動時に `disableSceneEnemiesOnStart` で無効化する。

1. `WaveDefinition.enemies` が存在する場合は新形式で読む。
2. `WaveDefinition.enemies` が空または null の場合は、旧形式の `enemyIds`、`isMissionTarget`、`lifetimes` を読む。
3. 実装上も `ActivateWaveNew` と `ActivateWave_regacy` に分離し、新旧の読み取り処理が混ざらないようにする。
4. M01 から新形式へ移行する。
5. M02 など既存ステージは動作確認が済むまで旧形式のまま残す。

## 最低限の新形式

配置ルールを後回しにする場合でも、まずは以下の形へ移すだけで index ずれを防げる。

```json
{
  "waveId": 0,
  "requireClearedWaves": [],
  "enemies": [
    {
      "enemyId": 0,
      "missionTarget": true,
      "lifetime": 0
    },
    {
      "enemyId": 1,
      "missionTarget": false,
      "lifetime": 0
    }
  ]
}
```
