param([string]$OtpCheckout, [switch]$Verify)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if (!$OtpCheckout) {
    $OtpCheckout = Join-Path $repo '../otp'
}
$baseline = Get-Content (Join-Path $repo 'reference-baseline.json') -Raw | ConvertFrom-Json
$commit = git -C $OtpCheckout rev-parse ($baseline.tag + '^{commit}')
if ($LASTEXITCODE -ne 0 -or $commit -cne $baseline.commit) {
    throw 'OTP checkout does not contain the pinned reference tag.'
}

$stage = (New-Item -ItemType Directory -Force (Join-Path $repo ("artifacts/beam-zlib-native/" + [Guid]::NewGuid().ToString('N')))).FullName
$archive = Join-Path $stage 'zlib.tar'
git -C $OtpCheckout archive --format=tar --output=$archive $commit erts/emulator/zlib
if ($LASTEXITCODE -ne 0) {
    throw 'Could not archive the pinned zlib sources.'
}
tar -xf $archive -C $stage
if ($LASTEXITCODE -ne 0) {
    throw 'Could not extract the pinned zlib sources.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$vcvars = Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
foreach ($variable in (& cmd.exe /d /c ('call "' + $vcvars + '" >nul && set'))) {
    $parts = $variable.Split('=', 2)
    if ($parts.Length -eq 2 -and $parts[0]) {
        [Environment]::SetEnvironmentVariable($parts[0], $parts[1], 'Process')
    }
}

$zlib = Join-Path $stage 'erts/emulator/zlib'
$files = @('adler32.c', 'crc32.c', 'deflate.c', 'trees.c', 'zutil.c') | ForEach-Object { Join-Path $zlib $_ }
$wrapper = Join-Path $PSScriptRoot 'beam_zlib.c'
$name = 'Erlang.Beam.Zlib.dll'
Push-Location $stage
try {
    & cl.exe /nologo /LD /O2 /MT "/I$zlib" $wrapper @files "/Fe:$name" /link /Brepro
    if ($LASTEXITCODE -ne 0) {
        throw 'The pinned zlib native adapter failed to build.'
    }
} finally {
    Pop-Location
}

$destination = Join-Path $repo 'src/Compiler/Erlang.Compiler/runtimes/win-x64/native'
$asset = Join-Path $destination $name
$built = Join-Path $stage $name
if ($Verify) {
    if ((Get-FileHash -LiteralPath $built).Hash -cne (Get-FileHash -LiteralPath $asset).Hash) {
        throw 'Rebuilt zlib adapter differs from the retained asset.'
    }
    Get-FileHash -LiteralPath $asset -Algorithm SHA256
    return
}
New-Item -ItemType Directory -Force $destination | Out-Null
Copy-Item -LiteralPath $built -Destination $destination
Get-FileHash -LiteralPath $asset -Algorithm SHA256
