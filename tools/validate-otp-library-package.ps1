param(
    [Parameter(Mandatory)][string]$Feed,
    [string]$Configuration = 'Debug',
    [ValidateSet('Stdlib', 'Kernel', 'Boot', 'Syntax', 'Compiler', 'Tools')][string]$Library = 'Stdlib'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$feedPath = (Resolve-Path -LiteralPath $Feed).Path
$work = Join-Path $repository ('artifacts/otp-library-consumer-' + [guid]::NewGuid().ToString('N'))
$consumerProject = switch ($Library) { 'Kernel' { 'Erlang.Otp.Kernel.Library.Tests' } 'Boot' { 'Erlang.Otp.Boot.Library.Tests' } 'Syntax' { 'Erlang.Otp.Syntax.Library.Tests' } 'Compiler' { 'Erlang.Otp.Compiler.Library.Tests' } 'Tools' { 'Erlang.Otp.Tools.Library.Tests' } default { 'HelloOtpLibraryReference' } }
$consumerDirectory = if ($Library -eq 'Stdlib') { 'examples/HelloOtpLibraryReference' } else { "tests/$consumerProject" }
New-Item -ItemType Directory -Path $work | Out-Null
function Invoke-LibraryDotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Original library consumer command failed: $($Arguments -join ' ')" }
}
Push-Location $repository
try {
    $packageLibraries = if ($Library -eq 'Boot') { @('Erts', 'Sasl') } elseif ($Library -eq 'Syntax') { @('SyntaxTools', 'Parsetools') } elseif ($Library -eq 'Tools') { @('Tools', 'RuntimeTools') } else { @($Library) }
    foreach ($packageLibrary in $packageLibraries) {
        Invoke-LibraryDotNet @('pack', "src/Erlang.Otp.$packageLibrary.Beam", '-c', $Configuration, '-m:1', '--nologo', '-p:UseInstalledPackage=true', "-p:RestoreSources=$feedPath", "-p:RestorePackagesPath=$work/build-cache", '-o', $feedPath)
    }
    Copy-Item -LiteralPath (Get-ChildItem -LiteralPath $consumerDirectory -File | Where-Object Extension -In '.cs', '.csproj').FullName -Destination $work
    if ($Library -in @('Boot', 'Syntax', 'Compiler', 'Tools')) { Copy-Item -LiteralPath (Join-Path $consumerDirectory 'Fixtures') -Destination (Join-Path $work 'Fixtures') -Recurse }
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
