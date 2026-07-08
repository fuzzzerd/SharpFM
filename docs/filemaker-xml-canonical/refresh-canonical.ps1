#!/usr/bin/env pwsh
# Re-syncs the vendored FileMaker XML references from upstream and regenerates
# step-catalog.json. Review the diff before committing — upstream version bumps
# may add steps or change canonical shapes, which the audit tests will surface.
#
# Sources (both CC BY 4.0, Andrew Kear / Clockwork Creative Technology):
#   - andykear/FileMaker-XMLsnippet-Claude-Skill   -> skill/
#   - andykear/FileMaker-XML-inspector-open-source -> inspector/
#
# Extraction is content-marker based, not line-number based, so it survives
# upstream edits.

[CmdletBinding()]
param(
    [string]$SkillRepo     = 'https://github.com/andykear/FileMaker-XMLsnippet-Claude-Skill',
    [string]$InspectorRepo = 'https://github.com/andykear/FileMaker-XML-inspector-open-source'
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$tmp  = Join-Path ([System.IO.Path]::GetTempPath()) ("fm-xml-refresh-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

try {
    # ── Skill ────────────────────────────────────────────────────────────
    $skillSrc = Join-Path $tmp 'skill'
    git clone --depth 1 $SkillRepo $skillSrc
    $skillDest = Join-Path $here 'skill'
    New-Item -ItemType Directory -Force -Path (Join-Path $skillDest 'references') | Out-Null
    Copy-Item (Join-Path $skillSrc 'SKILL.md')  (Join-Path $skillDest 'SKILL.md') -Force
    Copy-Item (Join-Path $skillSrc 'README.md') (Join-Path $skillDest 'README.upstream.md') -Force
    Copy-Item (Join-Path $skillSrc 'references/*.md') (Join-Path $skillDest 'references') -Force

    # ── Inspector ────────────────────────────────────────────────────────
    $insSrc = Join-Path $tmp 'inspector'
    git clone --depth 1 $InspectorRepo $insSrc
    # Normalize CRLF -> LF so the content-marker regexes below match reliably.
    $html = (Get-Content (Join-Path $insSrc 'clockwork-inspector.html') -Raw) -replace "`r`n", "`n"
    $insDest = Join-Path $here 'inspector'
    New-Item -ItemType Directory -Force -Path $insDest | Out-Null

    $header = @'
// Extracted verbatim from clockwork-inspector.html.
// Source: andykear/FileMaker-XML-inspector-open-source — Andrew Kear / Clockwork Creative Technology.
// Licensed CC BY 4.0. See ../LICENSE.md.

'@

    # FM_STEP_IDS: from 'const FM_STEP_IDS = {' to the matching '};'
    $catMatch = [regex]::Match($html, '(?s)const FM_STEP_IDS = \{.*?\n\};')
    if (-not $catMatch.Success) { throw 'FM_STEP_IDS block not found in inspector HTML.' }
    Set-Content (Join-Path $insDest 'FM_STEP_IDS.js') ($header + $catMatch.Value) -NoNewline

    # extractStepText: the DDR comment block through the end of the function
    # (closing brace at column 0).
    $fnMatch = [regex]::Match($html, '(?s)// extractStepText\(step\).*?\nfunction extractStepText\(step\) \{.*?\n\}\n')
    if (-not $fnMatch.Success) { throw 'extractStepText function not found in inspector HTML.' }
    Set-Content (Join-Path $insDest 'extractStepText.js') ($header + $fnMatch.Value) -NoNewline

    $catEntries = [regex]::Matches($catMatch.Value, "(?m)^\s*(\d+):\s*\{name:'").Count
    Write-Host "Vendored FM_STEP_IDS.js ($catEntries entries) and extractStepText.js."
    Write-Host 'Done. Review the diff before committing.'
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
