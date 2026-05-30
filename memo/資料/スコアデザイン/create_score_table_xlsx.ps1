param(
    [string]$OutputPath = "memo\資料\スコアデザイン\スコア表.xlsx"
)

$ErrorActionPreference = "Stop"

function XmlEscape([object]$value) {
    if ($null -eq $value) { return "" }
    return [System.Security.SecurityElement]::Escape([string]$value)
}

function ColName([int]$index) {
    $name = ""
    while ($index -gt 0) {
        $index--
        $name = [char](65 + ($index % 26)) + $name
        $index = [math]::Floor($index / 26)
    }
    return $name
}

function CellXml([int]$row, [int]$col, [object]$value, [int]$style = 0) {
    $ref = "$(ColName $col)$row"
    $styleAttr = if ($style -gt 0) { " s=`"$style`"" } else { "" }
    if ($null -eq $value -or $value -eq "") {
        return "<c r=`"$ref`"$styleAttr/>"
    }
    if ($value -is [int] -or $value -is [long] -or $value -is [double] -or $value -is [decimal]) {
        return "<c r=`"$ref`"$styleAttr><v>$value</v></c>"
    }
    return "<c r=`"$ref`" t=`"inlineStr`"$styleAttr><is><t>$(XmlEscape $value)</t></is></c>"
}

function SheetXml([array]$rows, [int[]]$widths) {
    $cols = ""
    for ($i = 0; $i -lt $widths.Count; $i++) {
        $n = $i + 1
        $cols += "<col min=`"$n`" max=`"$n`" width=`"$($widths[$i])`" customWidth=`"1`"/>"
    }

    $sheetData = ""
    for ($r = 0; $r -lt $rows.Count; $r++) {
        $rowNo = $r + 1
        $row = $rows[$r]
        $height = if ($rowNo -eq 1) { " ht=`"24`" customHeight=`"1`"" } else { "" }
        $sheetData += "<row r=`"$rowNo`"$height>"
        for ($c = 0; $c -lt $row.Count; $c++) {
            $style = 0
            if ($rowNo -eq 1) { $style = 1 }
            elseif ($rowNo -eq 2 -and $row[0] -eq "項目") { $style = 1 }
            elseif ($c -gt 0 -and ($row[$c] -is [int] -or $row[$c] -is [long] -or $row[$c] -is [double] -or $row[$c] -is [decimal])) { $style = 2 }
            $sheetData += CellXml $rowNo ($c + 1) $row[$c] $style
        }
        $sheetData += "</row>"
    }

    return @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>
  <cols>$cols</cols>
  <sheetData>$sheetData</sheetData>
</worksheet>
"@
}

$summaryRows = @(
    @("項目", "内容"),
    @("敵撃破スコア", "敵 maxhp * 1"),
    @("リザルト最終スコア", "タイムペナルティ前スコア - タイムペナルティ"),
    @("クリアボーナス", "2000 + 500 * ステージID"),
    @("ドロップ武装ポイント", "floor(100 + finalScore / 100)"),
    @("武装ステータス値", "min + 性能割当Pt * step。上下限で Clamp"),
    @("参照元", "stage_spawns.json / AugumentStatus.cs / Result.cs / WeaponStorage.cs")
)

$stageRows = @(
    @("ステージ", "クリアボーナス", "ターゲットのみ", "殲滅", "ターゲット時ペナルティ前", "殲滅時ペナルティ前", "内訳"),
    @("M01", 2500, 600, 6000, 3100, 8500, "target: LASM x3 / non-target: AA_GUN x36, SAM x18"),
    @("MVP", "selectedstage依存", 600, 6000, "selectedstage依存", "selectedstage依存", "M01と同配置"),
    @("M02", 3000, 2000, "6,500 + 条件付きUAV最大 4,500", 5000, "9,500 + 条件付きUAV最大 4,500", "UAV_STORAGE target x2 / non-target x1 / fighter最大x45"),
    @("M03", 3500, 6000, 6000, 9500, 9500, "ace_m03 x4, fighter_m03_special x1"),
    @("M04", 4000, 5000, 8700, 9000, 12700, "AIR_BATTLESHIP core / JAMMER / 追加SAM x20 / 追加AA_GUN x16"),
    @("M05", 4500, 7600, 7600, 12100, 12100, "Railgun x4, HR_AAGUN x12")
)

$enemyRows = @(
    @("敵/部位", "HP", "撃破スコア", "備考"),
    @("AA_GUN", 100, 100, ""),
    @("SAM", 100, 100, ""),
    @("LASM", 200, 200, ""),
    @("fighter / Ace", 100, 100, ""),
    @("ace_m03 / ACE_M03", 1000, 1000, ""),
    @("fighter_m03_special / AceM03Special", 2000, 2000, ""),
    @("UAV_STORAGE", 1000, 1000, ""),
    @("TRIGGER_EMPTY", 0, 0, "通常撃破対象ではないため 0 扱い"),
    @("AIR_BATTLESHIP core", 5000, 5000, ""),
    @("AIR_BATTLESHIP 追加 SAM 砲台", 100, 100, ""),
    @("AIR_BATTLESHIP 追加 AA_GUN 砲台", 100, 100, ""),
    @("JAMMER", 100, 100, ""),
    @("Railgun", 1000, 1000, ""),
    @("HR_AAGUN", 300, 300, "AA-2")
)

$weaponRows = @(
    @("武装", "合計最大Pt", "ステータス", "初期値", "最大値", "1ptあたり", "個別最大Pt", "上限到達finalScore目安"),
    @("GUN", 2091, "発射レート", 4, 60, "+0.1", 560, 199100),
    @("GUN", 2091, "射程", 450, 1000, "+10", 55, 199100),
    @("GUN", 2091, "威力", 3, 40, "+0.05", 740, 199100),
    @("GUN", 2091, "当たり判定サイズ", 1, 8, "+0.05", 140, 199100),
    @("GUN", 2091, "弾数", 120, 2000, "+5", 376, 199100),
    @("GUN", 2091, "初速", 100, 1200, "+5", 220, 199100),
    @("MSL", 1880, "初速", 100, 600, "+5", 100, 178000),
    @("MSL", 1880, "威力", 10, 200, "+5", 38, 178000),
    @("MSL", 1880, "最高速", 100, 800, "+5", 140, 178000),
    @("MSL", 1880, "加速度", 0, 100, "+1", 100, 178000),
    @("MSL", 1880, "誘導力", 45, 360, "+1", 315, 178000),
    @("MSL", 1880, "誘導象限", 45, 180, "+1", 135, 178000),
    @("MSL", 1880, "飛翔時間", 6, 30, "+0.5", 48, 178000),
    @("MSL", 1880, "射程（ロック可能距離）", 700, 2000, "+10", 130, 178000),
    @("MSL", 1880, "比例航法定数", 1, 10, "+0.1", 90, 178000),
    @("MSL", 1880, "弾数", 20, 180, "+0.25", 640, 178000),
    @("MSL", 1880, "装填時間", 5, 0.5, "-0.1", 45, 178000),
    @("MSL", 1880, "誘導目標の固定", 0.01, 1, "+0.01", 99, 178000),
    @("UGB", 1454, "加害範囲", 10, 500, "+1", 490, 135400),
    @("UGB", 1454, "威力", 100, 3000, "+10", 290, 135400),
    @("UGB", 1454, "炸裂範囲", 10, 500, "+1", 490, 135400),
    @("UGB", 1454, "弾数", 4, 50, "+0.25", 184, 135400),
    @("AAM", 1483, "射程（ロック可能距離）", 1500, 5000, "+5", 700, 138300),
    @("AAM", 1483, "飛翔時間", 5, 30, "+0.5", 50, 138300),
    @("AAM", 1483, "マルチロック数", 1, 10, "+0.2", 45, 138300),
    @("AAM", 1483, "弾数", 8, 180, "+0.25", 688, 138300)
)

$pointRows = @(
    @("項目", "式/値", "説明"),
    @("タイムペナルティ前スコア", "クリアボーナス + 撃破スコア", "タイム減点前の基礎スコア"),
    @("クリアボーナス", "2000 + 500 * ステージID", "M01=2500, M02=3000, M03=3500, M04=4000, M05=4500"),
    @("タイムペナルティ", "clamp(経過時間 - 180, 0, タイムペナルティ前スコア)", "180秒までは減点なし。スコア未満に丸める"),
    @("最終スコア", "タイムペナルティ前スコア - タイムペナルティ", "リザルトとハイスコアに使う値"),
    @("性能割り当てポイント", "floor(100 + 最終スコア / 100)", "WeaponStorage.GenerateDrop でドロップ武装の総割当Ptになる"),
    @("最低性能割り当てポイント", 100, "最終スコアが 0 でも 100pt"),
    @("例: M01ターゲットのみ 180秒以内", "floor(100 + 3100 / 100) = 131", "LASM x3撃破、時間減点なし"),
    @("例: M01殲滅 180秒以内", "floor(100 + 8500 / 100) = 185", "M01全敵撃破、時間減点なし"),
    @("例: M05殲滅 180秒以内", "floor(100 + 12100 / 100) = 221", "M05全敵撃破、時間減点なし")
)

$memoRows = @(
    @("メモ"),
    @("タイムペナルティ前スコア = クリアボーナス + 撃破スコア。"),
    @("HP 100 の通常敵は 1体 100 点。クリアボーナスとの差が読みやすくなる。"),
    @("M01/MVP は非ターゲットの AA_GUN と SAM 合計で 5,400 点。LASM強化後はターゲット分 600 点。"),
    @("M02 は UAV_STORAGE の fighter 生成をどこまでスコア対象にするかで殲滅スコアが変わる。全出撃なら +4,500 点。"),
    @("M04 は AIR_BATTLESHIP の自動追加砲台を含めると非ターゲット分が 3,700 点になる。"),
    @("タイムペナルティ = clamp(経過時間 - 180, 0, タイムペナルティ前スコア)。"),
    @("性能割当Pt = 100 + 最終スコア / 100。ドロップ種別は GUN/MSL/UGB/AAM からランダム。ポイント配分も対象ステータスへランダム。")
)

$sheets = @(
    @{ Name = "概要"; Rows = $summaryRows; Widths = @(24, 90) },
    @{ Name = "ステージ別スコア"; Rows = $stageRows; Widths = @(12, 16, 16, 28, 22, 32, 70) },
    @{ Name = "敵スコア"; Rows = $enemyRows; Widths = @(34, 12, 16, 42) },
    @{ Name = "武装上限"; Rows = $weaponRows; Widths = @(12, 14, 30, 12, 12, 14, 14, 24) },
    @{ Name = "性能Pt導出"; Rows = $pointRows; Widths = @(28, 58, 70) },
    @{ Name = "メモ"; Rows = $memoRows; Widths = @(120) }
)

$outFull = [System.IO.Path]::GetFullPath($OutputPath)
$outDir = Split-Path -Parent $outFull
$tempDir = Join-Path $outDir "xlsx_build"
if (Test-Path -LiteralPath $tempDir) { Remove-Item -LiteralPath $tempDir -Recurse -Force }
New-Item -ItemType Directory -Path $tempDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tempDir "_rels") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tempDir "xl") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tempDir "xl\_rels") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tempDir "xl\worksheets") | Out-Null

$contentTypes = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
"@
for ($i = 1; $i -le $sheets.Count; $i++) {
    $contentTypes += "  <Override PartName=`"/xl/worksheets/sheet$i.xml`" ContentType=`"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml`"/>`n"
}
$contentTypes += "</Types>"
Set-Content -LiteralPath (Join-Path $tempDir "[Content_Types].xml") -Value $contentTypes -Encoding UTF8

Set-Content -LiteralPath (Join-Path $tempDir "_rels\.rels") -Value @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
"@ -Encoding UTF8

$workbookSheets = ""
$workbookRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
"@
for ($i = 1; $i -le $sheets.Count; $i++) {
    $sheetName = XmlEscape $sheets[$i - 1].Name
    $workbookSheets += "    <sheet name=`"$sheetName`" sheetId=`"$i`" r:id=`"rId$i`"/>`n"
    $workbookRels += "  <Relationship Id=`"rId$i`" Type=`"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet`" Target=`"worksheets/sheet$i.xml`"/>`n"
}
$styleRelId = $sheets.Count + 1
$workbookRels += "  <Relationship Id=`"rId$styleRelId`" Type=`"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles`" Target=`"styles.xml`"/>`n</Relationships>"

Set-Content -LiteralPath (Join-Path $tempDir "xl\workbook.xml") -Value @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
$workbookSheets  </sheets>
</workbook>
"@ -Encoding UTF8
Set-Content -LiteralPath (Join-Path $tempDir "xl\_rels\workbook.xml.rels") -Value $workbookRels -Encoding UTF8

Set-Content -LiteralPath (Join-Path $tempDir "xl\styles.xml") -Value @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="2">
    <font><sz val="11"/><name val="Yu Gothic"/></font>
    <font><b/><color rgb="FFFFFFFF"/><sz val="11"/><name val="Yu Gothic"/></font>
  </fonts>
  <fills count="3">
    <fill><patternFill patternType="none"/></fill>
    <fill><patternFill patternType="gray125"/></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FF1F4E78"/><bgColor indexed="64"/></patternFill></fill>
  </fills>
  <borders count="2">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border><left style="thin"><color rgb="FFD9E2F3"/></left><right style="thin"><color rgb="FFD9E2F3"/></right><top style="thin"><color rgb="FFD9E2F3"/></top><bottom style="thin"><color rgb="FFD9E2F3"/></bottom><diagonal/></border>
  </borders>
  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
  <cellXfs count="3">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1"><alignment vertical="top" wrapText="1"/></xf>
    <xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"><alignment vertical="center" wrapText="1"/></xf>
    <xf numFmtId="3" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1"><alignment vertical="top"/></xf>
  </cellXfs>
</styleSheet>
"@ -Encoding UTF8

for ($i = 1; $i -le $sheets.Count; $i++) {
    $xml = SheetXml $sheets[$i - 1].Rows $sheets[$i - 1].Widths
    Set-Content -LiteralPath (Join-Path $tempDir "xl\worksheets\sheet$i.xml") -Value $xml -Encoding UTF8
}

if (Test-Path -LiteralPath $outFull) { Remove-Item -LiteralPath $outFull -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $outFull)
Remove-Item -LiteralPath $tempDir -Recurse -Force
Write-Output $outFull





