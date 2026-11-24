# Draws the user guide's pictures (Docs/images) from the app itself: the Lighthouse scenes (DocsCaptureTests), the
# dialogs (DialogCaptureTests) and the token (HereTokenTests), then copies the ones listed in docs-images.txt.
# Re-run it before a release, and look at what changed: git diff --stat Docs/images
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$capture = Join-Path ([System.IO.Path]::GetTempPath()) ("trizbort-docs-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $capture | Out-Null
$env:TRIZBORT_DOCS_CAPTURE = $capture
$env:TRIZBORT_CAPTURE_OUTPUT = $capture
try {
    dotnet test (Join-Path $root 'src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj') --nologo `
        --filter 'TestCategory=Docs|FullyQualifiedName~DialogCaptureTests|FullyQualifiedName~HereTokenTests.CaptureTheTokenOnTheMap'
    if ($LASTEXITCODE -ne 0) { throw 'The capture tests failed: Docs/images is unchanged.' }

    $images = Join-Path $root 'Docs/images'
    New-Item -ItemType Directory -Force $images | Out-Null
    $missing = @()
    foreach ($line in Get-Content (Join-Path $PSScriptRoot 'docs-images.txt')) {
        if ($line -match '^\s*(#|$)') { continue }
        $picture, $from = $line -split '\s+', 2
        $source = Join-Path $capture $from.Trim()
        if (Test-Path $source) { Copy-Item $source (Join-Path $images $picture) -Force }
        else { $missing += $from.Trim() }
    }
    if ($missing.Count -gt 0) { throw ("Not captured: " + ($missing -join ', ')) }
    Write-Host "Docs/images updated from $capture"
}
finally {
    Remove-Item Env:TRIZBORT_DOCS_CAPTURE, Env:TRIZBORT_CAPTURE_OUTPUT -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $capture -ErrorAction SilentlyContinue
}
