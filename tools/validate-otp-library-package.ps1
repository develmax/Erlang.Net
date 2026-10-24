param(
    [Parameter(Mandatory)][string]$Feed,
    [string]$Configuration = 'Debug',
    [ValidateSet('Stdlib', 'Kernel')][string]$Library = 'Stdlib'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$feedPath = (Resolve-Path -LiteralPath $Feed).Path
$work = Join-Path $repository ('artifacts/otp-library-consumer-' + [guid]::NewGuid().ToString('N'))
$consumerProject = if ($Library -eq 'Kernel') { 'Erlang.Otp.Kernel.Library.Tests' } else { 'HelloOtpLibraryReference' }
$consumerDirectory = if ($Library -eq 'Kernel') { 'tests/Erlang.Otp.Kernel.Library.Tests' } else { 'examples/HelloOtpLibraryReference' }
New-Item -ItemType Directory -Path $work | Out-Null
function Invoke-LibraryDotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Original library consumer command failed: $($Arguments -join ' ')" }
}
Push-Location $repository
try {
    Invoke-LibraryDotNet @('pack', "src/Erlang.Otp.$Library.Beam", '-c', $Configuration, '-m:1', '--nologo', '-p:UseInstalledPackage=true', "-p:RestoreSources=$feedPath", "-p:RestorePackagesPath=$work/build-cache", '-o', $feedPath)
    Copy-Item -LiteralPath (Get-ChildItem -LiteralPath $consumerDirectory -File | Where-Object Extension -In '.cs', '.csproj').FullName -Destination $work
    Invoke-LibraryDotNet @('publish', "$work/$consumerProject.csproj", '-c', $Configuration, '-m:1', '--nologo', '-p:ImportDirectoryBuildProps=false', '-p:ImportDirectoryBuildTargets=false', '-p:UseInstalledPackage=true', "-p:RestoreSources=$feedPath", "-p:RestorePackagesPath=$work/consumer-cache", '-o', "$work/published")
    $dotnetHost = (Get-Command dotnet -CommandType Application | Select-Object -First 1).Source
    $savedPath = $env:PATH
    Push-Location $work
    try {
        $env:PATH = ''
        if ($Library -eq 'Kernel') {
            & $dotnetHost "$work/published/$consumerProject.dll"
            if ($LASTEXITCODE -ne 0) { throw 'Published original kernel consumer failed' }
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
    @{ Passed = $true; Library = $Library; ConsumerRuns = $(if ($Library -eq 'Kernel') { 1 } else { 4 }); EmptyPath = $true; Directory = $work } | ConvertTo-Json | Set-Content "$work/completion.json"
} finally { Pop-Location }
