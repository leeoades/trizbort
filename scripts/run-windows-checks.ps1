<#
.SYNOPSIS
  Builds and tests both Trizbort solutions on Windows and writes a summary for agents to read.

.DESCRIPTION
  Fixed checks only - this script does not run anything else. Output goes to
  artifacts\windows-checks\<timestamp>\ (git-ignored): summary.txt, build/test logs and TRX files.
  With -Captures it also runs the dialog/map capture tests (env var TRIZBORT_CAPTURE_OUTPUT) and
  writes PNGs under agent-docs\plans\avalonia-parity\screenshots\.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-windows-checks.ps1
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\run-windows-checks.ps1 -Captures
#>
param(
  [switch]$Captures,
  [switch]$SkipLegacy,
  [switch]$SkipNew
)

$ErrorActionPreference = 'Continue'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repo
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$out = Join-Path $repo "artifacts\windows-checks\$stamp"
New-Item -ItemType Directory -Force $out | Out-Null
$summary = Join-Path $out 'summary.txt'

function Log([string]$text) { $text | Tee-Object -FilePath $summary -Append }

function Step([string]$name, [string[]]$dotnetArgs) {
  $logFile = Join-Path $out "$name.log"
  Write-Host "== $name" -ForegroundColor Cyan
  $start = Get-Date
  & dotnet @dotnetArgs *> $logFile
  $code = $LASTEXITCODE
  $secs = [int]((Get-Date) - $start).TotalSeconds
  $tail = Get-Content $logFile | Select-String -Pattern '(error|Passed!|Failed!|Total tests|Build succeeded|warning\(s\)|error\(s\))' | Select-Object -Last 12
  Log "[$name] exit=$code seconds=$secs"
  foreach ($line in $tail) { Log "    $($line.Line.Trim())" }
  return $code
}

Log "Trizbort Windows checks $stamp"
Log "commit: $(git rev-parse --short HEAD) branch: $(git rev-parse --abbrev-ref HEAD)"
Log "dirty files: $((git status --porcelain | Measure-Object).Count)"
Log "os: $([System.Environment]::OSVersion.VersionString)"
Log "sdks: $((dotnet --list-sdks) -join '; ')"
Log "desktop runtimes: $((dotnet --list-runtimes | Select-String 'WindowsDesktop') -join '; ')"
Log ''

if (-not $SkipNew) {
  Step 'build-new' @('build', 'Trizbort.sln', '-c', 'Debug', '-nologo') | Out-Null
  Step 'test-new'  @('test', 'Trizbort.sln', '-c', 'Debug', '--no-build', '-nologo',
                     '--logger', 'trx;LogFilePrefix=new', '--results-directory', $out) | Out-Null
}
if (-not $SkipLegacy) {
  Step 'build-legacy' @('build', 'legacy\Trizbort.Legacy.sln', '-c', 'Debug', '-nologo') | Out-Null
  Step 'test-legacy'  @('test', 'legacy\Trizbort.Legacy.sln', '-c', 'Debug', '--no-build', '-nologo',
                        '--logger', 'trx;LogFilePrefix=legacy', '--results-directory', $out) | Out-Null
}

if ($Captures) {
  $shots = Join-Path $repo 'agent-docs\plans\avalonia-parity\screenshots'
  $env:TRIZBORT_CAPTURE_OUTPUT = Join-Path $shots 'winforms'
  Step 'capture-legacy-dialogs' @('test', 'legacy\Trizbort.Legacy.sln', '-c', 'Debug', '--no-build', '-nologo',
                                  '--filter', 'FullyQualifiedName~DialogCaptureTests') | Out-Null
  $env:TRIZBORT_CAPTURE_OUTPUT = Join-Path $shots 'avalonia'
  Step 'capture-avalonia-dialogs' @('test', 'src\Trizbort.Avalonia.Tests\Trizbort.Avalonia.Tests.csproj', '-c', 'Debug',
                                    '--no-build', '-nologo', '--filter', 'FullyQualifiedName~DialogCaptureTests') | Out-Null
  Remove-Item Env:\TRIZBORT_CAPTURE_OUTPUT
}

Log ''
Log "Results folder: $out"
Write-Host "Done. Summary: $summary" -ForegroundColor Green
