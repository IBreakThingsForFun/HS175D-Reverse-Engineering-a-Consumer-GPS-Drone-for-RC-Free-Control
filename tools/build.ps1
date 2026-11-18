<#
  build.ps1 — portable ARM cross-compile for the drone, no install required.

  Uses Zig as a self-contained C cross-compiler. On first run it downloads Zig
  (one ~70 MB zip) into tools\zig\ and never touches anything else on the system.
  Everything lives in this folder; delete tools\zig\ to remove it completely.

  Usage (from anywhere):
      pwsh tools\build.ps1                 # build ttyhook.so
      pwsh tools\build.ps1 -Push           # build, then adb push to the drone /tmp
      pwsh tools\build.ps1 -Glibc 2.31     # match a different drone glibc (see note)
      pwsh tools\build.ps1 -Src foo.c -Out foo.so   # build something else

  glibc note: the .so must reference a glibc version <= the drone's, or it won't load
  ("version `GLIBC_2.x' not found"). Default 2.28 is safe for this 2021 build. To check
  the drone's exact version, run on it:  strings /lib/libc.so.6 | grep -oE 'GLIBC_2\.[0-9]+' | sort -V | tail -1
#>
param(
    [switch]$Push,
    [string]$Ip    = "192.168.100.1",
    [string]$Glibc = "2.28",
    [string]$Src   = "ttyhook.c",
    [string]$Out   = "ttyhook.so"
)

$ErrorActionPreference = "Stop"
$here   = Split-Path -Parent $MyInvocation.MyCommand.Path
$zigDir = Join-Path $here "zig"
$zigExe = Join-Path $zigDir "zig.exe"
$zigVer = "0.13.0"

if (-not (Test-Path $zigExe)) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $url = "https://ziglang.org/download/$zigVer/zig-windows-x86_64-$zigVer.zip"
    $zip = Join-Path $here "zig.zip"
    Write-Host "Zig not found — downloading $zigVer (portable, ~70 MB)..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $url -OutFile $zip
    Write-Host "Extracting..." -ForegroundColor Cyan
    Expand-Archive -Path $zip -DestinationPath $here -Force
    Move-Item (Join-Path $here "zig-windows-x86_64-$zigVer") $zigDir -Force
    Remove-Item $zip -Force
    Write-Host "Zig ready in $zigDir" -ForegroundColor Green
}

$srcPath = if ([IO.Path]::IsPathRooted($Src)) { $Src } else { Join-Path $here $Src }
$outPath = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $here $Out }
$target  = "arm-linux-gnueabihf.$Glibc"

Write-Host "Compiling $([IO.Path]::GetFileName($srcPath)) -> $([IO.Path]::GetFileName($outPath))  ($target)" -ForegroundColor Cyan
& $zigExe cc -target $target -shared -fPIC -O2 -o $outPath $srcPath -ldl -lpthread
if ($LASTEXITCODE -ne 0) { throw "compile failed (exit $LASTEXITCODE)" }
Write-Host "Built: $outPath" -ForegroundColor Green

if ($Push) {
    Write-Host "Pushing to ${Ip}:5555 /tmp/..." -ForegroundColor Cyan
    & adb connect "${Ip}:5555" | Out-Null
    & adb -s "${Ip}:5555" push $outPath "/tmp/$([IO.Path]::GetFileName($outPath))"
}
