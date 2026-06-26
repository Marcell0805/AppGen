# Builds portal-data.js, search-index.js, and section HTML shells from portal/data/*.json
param(
    [string]$PortalRoot = (Split-Path $PSScriptRoot -Parent)
)

$utf8 = [System.Text.UTF8Encoding]::new($false)
$dataDir = Join-Path $PortalRoot "data"
$sectionsDir = Join-Path $PortalRoot "sections"
$jsDir = Join-Path $PortalRoot "js"

function Read-Json([string]$path) {
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    return $text | ConvertFrom-Json
}

function Escape-JsString([string]$s) {
    if ($null -eq $s) { return "" }
    return ($s -replace '\\', '\\\\' -replace '"', '\"' -replace "`r", '' -replace "`n", '\n')
}

function Get-SearchText($doc) {
    $parts = [System.Collections.Generic.List[string]]::new()
    if ($doc.title) { $parts.Add($doc.title) }
    if ($doc.summary) { $parts.Add($doc.summary) }
    if ($doc.searchKeywords) { foreach ($k in $doc.searchKeywords) { $parts.Add($k) } }
    if ($doc.tags) { foreach ($t in $doc.tags) { $parts.Add($t) } }
    if ($doc.blocks) {
        foreach ($b in $doc.blocks) {
            if ($b.heading) { $parts.Add($b.heading) }
            if ($b.content) { $parts.Add($b.content) }
            if ($b.bullets) { foreach ($x in $b.bullets) { $parts.Add($x) } }
        }
    }
    return ($parts -join ' ')
}

# Load settings and nav
$settings = Read-Json (Join-Path $dataDir "portal-settings.json")
$nav = Read-Json (Join-Path $dataDir "nav.json")

# Load all section content (exclude portal-settings and nav)
$sections = @{}
$searchEntries = @()

Get-ChildItem $dataDir -Filter "*.json" | ForEach-Object {
    if ($_.Name -in @("portal-settings.json", "nav.json")) { return }
    $doc = Read-Json $_.FullName
    $id = $doc.id
    if (-not $id) { return }
    $sections[$id] = $doc

    # Section-level index entry
    $searchEntries += [ordered]@{
        id = $id
        title = $doc.title
        section = $doc.title
        url = "sections/$id.html"
        text = Get-SearchText $doc
        tags = @($doc.tags)
        status = $doc.status
    }

    # Block-level index entries
    if ($doc.blocks) {
        foreach ($b in $doc.blocks) {
            $blockId = if ($b.id) { "$id-$($b.id)" } else { "$id-block" }
            $blockText = @()
            if ($b.heading) { $blockText += $b.heading }
            if ($b.content) { $blockText += $b.content }
            if ($b.bullets) { $blockText += $b.bullets }
            $searchEntries += [ordered]@{
                id = $blockId
                title = if ($b.heading) { $b.heading } else { $doc.title }
                section = $doc.title
                url = "sections/$id.html#$($b.id)"
                text = ($blockText -join ' ')
                tags = @($doc.tags)
                status = $doc.status
            }
        }
    }
}

# Serialize sections object (hashtable keys need manual JSON object)
$sectionParts = @()
foreach ($key in ($sections.Keys | Sort-Object)) {
    $sectionJson = $sections[$key] | ConvertTo-Json -Depth 20 -Compress
    $sectionParts += "`"$key`":$sectionJson"
}
$sectionsJsObject = '{' + ($sectionParts -join ',') + '}'

$settingsJson = ($settings | ConvertTo-Json -Depth 20 -Compress)
$navJson = ($nav | ConvertTo-Json -Depth 20 -Compress)
$searchJson = ($searchEntries | ConvertTo-Json -Depth 10 -Compress)

$portalDataJs = @"
window.DELTACORE_PORTAL = {
  settings: $settingsJson,
  nav: $navJson,
  sections: $sectionsJsObject
};
"@

$searchIndexJs = @"
window.DELTACORE_SEARCH_INDEX = $searchJson;
"@

[IO.File]::WriteAllText((Join-Path $jsDir "portal-data.js"), $portalDataJs, $utf8)
[IO.File]::WriteAllText((Join-Path $jsDir "search-index.js"), $searchIndexJs, $utf8)

# Section HTML template
$sectionTemplate = @'
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>{{TITLE}} — DeltaCore Engineering Portal</title>
  <link rel="stylesheet" href="../css/portal.css">
  <style>html:not(.auth-ok) body > :not(#auth-gate) { display: none; }</style>
  <script src="../js/auth.js"></script>
</head>
<body data-nav-scope="section" data-section-id="{{ID}}">
  <div id="portal-toolbar"></div>
  <div class="page">
    <aside class="sidebar" data-portal-sidebar></aside>
    <main class="main" id="section-content">
      <p class="loading">Loading…</p>
    </main>
  </div>
  <footer class="site-footer no-print">
    <a href="https://github.com/Marcell0805/DeltaCore/blob/master/portal/ENGINEERING_PORTAL.md" target="_blank" rel="noopener">Maintainer docs</a>
  </footer>
  <script src="../js/vendor/fuse.min.js"></script>
  <script src="../js/portal-data.js"></script>
  <script src="../js/search-index.js"></script>
  <script src="../js/portal.js"></script>
  <script src="../js/search.js"></script>
</body>
</html>
'@

foreach ($key in $sections.Keys) {
    $doc = $sections[$key]
    $html = $sectionTemplate -replace '\{\{ID\}\}', $doc.id -replace '\{\{TITLE\}\}', $doc.title
    [IO.File]::WriteAllText((Join-Path $sectionsDir "$($doc.id).html"), $html, $utf8)
}

Write-Host "Built portal-data.js ($($sections.Count) sections)"
Write-Host "Built search-index.js ($($searchEntries.Count) entries)"
Write-Host "Generated $($sections.Count) section HTML files"
