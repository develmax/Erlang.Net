param(
    [Parameter(Mandatory)][string]$Feed,
    [string]$Configuration = 'Debug',
    [ValidateSet('Stdlib', 'Kernel', 'Boot', 'Syntax', 'Compiler', 'Tools', 'Documentation', 'Mnesia', 'Asn1', 'Security')][string]$Library = 'Stdlib'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$feedPath = (Resolve-Path -LiteralPath $Feed).Path
$work = Join-Path $repository ('artifacts/otp-library-consumer-' + [guid]::NewGuid().ToString('N'))
$consumerProject = switch ($Library) { 'Kernel' { 'Erlang.Otp.Kernel.Library.Tests' } 'Boot' { 'Erlang.Otp.Boot.Library.Tests' } 'Syntax' { 'Erlang.Otp.Syntax.Library.Tests' } 'Compiler' { 'Erlang.Otp.Compiler.Library.Tests' } 'Tools' { 'Erlang.Otp.Tools.Library.Tests' } 'Documentation' { 'Erlang.Otp.Documentation.Library.Tests' } 'Mnesia' { 'Erlang.Otp.Mnesia.Library.Tests' } 'Asn1' { 'Erlang.Otp.Asn1.Library.Tests' } 'Security' { 'Erlang.Otp.Security.Library.Tests' } default { 'HelloOtpLibraryReference' } }
$consumerDirectory = if ($Library -eq 'Stdlib') { 'examples/HelloOtpLibraryReference' } else { "tests/$consumerProject" }
New-Item -ItemType Directory -Path $work | Out-Null
function Invoke-LibraryDotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Original library consumer command failed: $($Arguments -join ' ')" }
}
Push-Location $repository
try {
    $packageLibraries = if ($Library -eq 'Boot') { @('Erts', 'Sasl') } elseif ($Library -eq 'Syntax') { @('SyntaxTools', 'Parsetools') } elseif ($Library -eq 'Tools') { @('Tools', 'RuntimeTools') } elseif ($Library -eq 'Documentation') { @('Eunit', 'Edoc', 'Xmerl') } elseif ($Library -eq 'Security') { @('Crypto', 'PublicKey') } else { @($Library) }
    foreach ($packageLibrary in $packageLibraries) {
        Invoke-LibraryDotNet @('pack', "src/Erlang.Otp.$packageLibrary.Beam", '-c', $Configuration, '-m:1', '--nologo', '-p:UseInstalledPackage=true', "-p:RestoreSources=$feedPath", "-p:RestorePackagesPath=$work/build-cache", '-o', $feedPath)
    }
    if ($Library -in @('Documentation', 'Mnesia', 'Asn1', 'Security')) {
        $verifiedAssets = 0
        foreach ($packageLibrary in $packageLibraries) {
            $directory = "src/Erlang.Otp.$packageLibrary.Beam"
            $provenance = Get-Content -LiteralPath "$directory/provenance.json" -Raw | ConvertFrom-Json
            $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $feedPath "Erlang.Otp.$packageLibrary.Beam.0.1.0.nupkg"))
            try {
                foreach ($file in @($provenance.Files | Where-Object Path -Match '^(src|include|priv|asn1)/')) {
                    $entry = $archive.GetEntry("otp/$($file.Path)")
                    if ($null -eq $entry) { throw "Missing original package asset: $packageLibrary/$($file.Path)" }
                    $input = $entry.Open()
                    $bytes = [IO.MemoryStream]::new()
                    try {
                        $input.CopyTo($bytes)
                        $actual = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes.ToArray()))
                        if ($actual -cne $file.Sha256 -or (Get-FileHash -LiteralPath "$directory/Otp/$($file.Path)" -Algorithm SHA256).Hash -cne $actual) { throw "Original package asset differs: $packageLibrary/$($file.Path)" }
                    } finally { $bytes.Dispose(); $input.Dispose() }
                    $verifiedAssets++
                }
            } finally { $archive.Dispose() }
        }
        $expectedAssets = if ($Library -eq 'Mnesia') { 32 } elseif ($Library -eq 'Asn1') { 25 } elseif ($Library -eq 'Security') { 84 } else { 89 }
        if ($verifiedAssets -ne $expectedAssets) { throw 'Incomplete original source/support package inventory' }
        Write-Output "Original $Library source/support package assets: $verifiedAssets byte-verified"
    }
    Copy-Item -LiteralPath (Get-ChildItem -LiteralPath $consumerDirectory -File | Where-Object Extension -In '.cs', '.csproj').FullName -Destination $work
    if ($Library -in @('Boot', 'Syntax', 'Compiler', 'Tools', 'Documentation', 'Mnesia', 'Asn1', 'Security')) { Copy-Item -LiteralPath (Join-Path $consumerDirectory 'Fixtures') -Destination (Join-Path $work 'Fixtures') -Recurse }
    Invoke-LibraryDotNet @('publish', "$work/$consumerProject.csproj", '-c', $Configuration, '-m:1', '--nologo', '-p:ImportDirectoryBuildProps=false', '-p:ImportDirectoryBuildTargets=false', '-p:UseInstalledPackage=true', "-p:RestoreSources=$feedPath", "-p:RestorePackagesPath=$work/consumer-cache", '-o', "$work/published")
    $dotnetHost = (Get-Command dotnet -CommandType Application | Select-Object -First 1).Source
    $savedPath = $env:PATH
    Push-Location $work
    try {
        $env:PATH = ''
        if ($Library -ne 'Stdlib') {
            & $dotnetHost "$work/published/$consumerProject.dll"
            if ($LASTEXITCODE -ne 0) { throw "Published original $Library consumer failed" }
        } else {
            foreach ($lists in @('Managed', 'Beam')) {
                foreach ($queue in @('Managed', 'Beam')) {
                    & $dotnetHost "$work/published/$consumerProject.dll" $lists $queue
                    if ($LASTEXITCODE -ne 0) { throw "Published original library consumer failed: $lists/$queue" }
                }
            }
        }
    } finally {
        $env:PATH = $savedPath
        Pop-Location
    }
    @{ Passed = $true; Library = $Library; ConsumerRuns = $(if ($Library -eq 'Stdlib') { 4 } else { 1 }); EmptyPath = $true; Directory = $work } | ConvertTo-Json | Set-Content "$work/completion.json"
} finally { Pop-Location }
