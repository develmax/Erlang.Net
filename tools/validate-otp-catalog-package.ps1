param(
    [Parameter(Mandatory)][string]$Feed,
    [string]$Configuration = 'Debug',
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$NativePackage,
    [string]$Output = 'artifacts/library485/cold-catalog'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$feedPath = (Resolve-Path -LiteralPath $Feed).Path
$admissionPath = (Resolve-Path -LiteralPath $Admission).Path
if (-not $NativePackage) {
    $previous = Get-Content artifacts/library485/catalog/latest-run.json -Raw | ConvertFrom-Json
    $NativePackage = Split-Path -Parent $previous.Manifest
}
$nativeSource = (Resolve-Path -LiteralPath $NativePackage).Path
$work = Join-Path ([IO.Path]::GetFullPath($Output)) ([Guid]::NewGuid().ToString('N'))
$consumer = Join-Path $work 'consumer'
$package = Join-Path $work 'driver-package'
$publish = Join-Path $work 'published'
New-Item -ItemType Directory -Path $consumer, $package -Force | Out-Null
Copy-Item -LiteralPath tools/Validation/Erlang.OtpCatalogValidation/Erlang.OtpCatalogValidation.csproj -Destination $consumer
Get-ChildItem tools/Validation/Erlang.OtpCatalogValidation -File -Filter '*.cs' | Copy-Item -Destination $consumer
dotnet publish (Join-Path $consumer 'Erlang.OtpCatalogValidation.csproj') -c $Configuration -m:1 --nologo -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false -p:UseInstalledPackage=true "-p:RestoreSources=$feedPath" "-p:RestorePackagesPath=$work/cache" -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Cold catalog PackageReference publication failed' }
dotnet publish src/Runtime/Erlang.DriverHost -c $Configuration -m:1 --nologo -p:ConcurrentBuild=false -p:RuntimeFrameworkVersion=10.0.11 -p:RollForward=Disable -o $package
if ($LASTEXITCODE -ne 0) { throw 'Cold catalog DriverHost publication failed' }
# Bind the child to the actual installed package runtime, not another producer output.
foreach ($assembly in @('Erlang.Runtime.dll', 'Erlang.Runtime.Bundle.dll', 'Erlang.Terms.dll', 'Erlang.Serialization.dll', 'Erlang.Otp.Stdlib.dll')) {
    Copy-Item -LiteralPath (Join-Path $publish $assembly) -Destination $package
}
foreach ($file in @('erldrv-host-native.dll', 'erldrv-host-abi.json', 'erldrv-host-errno.json', 'erldrv-host-config.json')) {
    Copy-Item -LiteralPath (Join-Path $nativeSource $file) -Destination $package
}
$manifest = Join-Path $package 'erldrv-host-package.json'
$packageFiles = @(Get-ChildItem -LiteralPath $package -File | Sort-Object Name | ForEach-Object {
    [ordered]@{ Path = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
[ordered]@{ Protocol = 1; EntryPoint = 'Erlang.DriverHost.exe'; Files = $packageFiles } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifest -Encoding utf8
$dotnetHost = (Get-Command dotnet -CommandType Application | Select-Object -First 1).Source
$savedPath = $env:PATH
Push-Location $work
try {
    $env:PATH = ''
    & $dotnetHost exec --fx-version 10.0.11 (Join-Path $publish 'Erlang.OtpCatalogValidation.dll') $repository $admissionPath $manifest (Join-Path $work 'results')
    if ($LASTEXITCODE -ne 0) { throw "Cold configured catalog failed: $work" }
} finally {
    $env:PATH = $savedPath
    Pop-Location
}
[ordered]@{ Passed = $true; EmptyPath = $true; IsolatedPackageCache = $true; Directory = $work; RuntimeSha256 = (Get-FileHash -LiteralPath (Join-Path $publish 'Erlang.Runtime.dll') -Algorithm SHA256).Hash } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $work 'completion.json') -Encoding utf8
Write-Host "Cold configured original catalog and176 captures passed: $work"
