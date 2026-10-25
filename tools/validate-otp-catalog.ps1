param(
    [string]$Admission = 'artifacts/library376-official-compiler/official-admission.json',
    [string]$Output = 'artifacts/library485/catalog',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$profile = Get-Content -LiteralPath $Admission -Raw | ConvertFrom-Json
if ($profile.SourceCommit -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5' -or -not $profile.EveryExtractedFileMatchesArchive) { throw 'Unadmitted original OTP headers' }
if ((Get-FileHash -LiteralPath $profile.ExecutionManifestPath -Algorithm SHA256).Hash -cne $profile.ExecutionManifestSha256) { throw 'Original provider inventory changed' }
$headers = Join-Path $profile.InstalledOtpRoot 'erts-17.1/include'
$files = Get-Content -LiteralPath $profile.ExecutionManifestPath -Raw | ConvertFrom-Json
$headerFiles = @($files | Where-Object { $_.Path.StartsWith('erts-17.1/include/', [StringComparison]::Ordinal) })
if ($headerFiles.Count -eq 0) { throw 'Missing original headers' }
function Confirm-CatalogHeaders {
    foreach ($file in $headerFiles) {
        if ((Get-FileHash -LiteralPath (Join-Path $profile.InstalledOtpRoot $file.Path) -Algorithm SHA256).Hash -cne $file.Sha256) { throw "Original header changed: $($file.Path)" }
    }
}
Confirm-CatalogHeaders
$root = [IO.Path]::GetFullPath($Output)
$run = Join-Path $root ([Guid]::NewGuid().ToString('N'))
$native = Join-Path $run 'native'
$package = Join-Path $run 'package'
New-Item -ItemType Directory -Path $native, $package -Force | Out-Null
dotnet build tools/Erlang.OtpCatalogValidation -c Release -m:1 -p:ConcurrentBuild=false --nologo
if ($LASTEXITCODE -ne 0) { throw 'Catalog validation tool build failed' }
dotnet publish src/Erlang.DriverHost -c Release -m:1 -p:ConcurrentBuild=false -p:RuntimeFrameworkVersion=10.0.11 -p:RollForward=Disable -o $package --nologo
if ($LASTEXITCODE -ne 0) { throw 'DriverHost publish failed' }
$pythonArguments = @('-B', 'tools/Erlang.OtpCatalogValidation/build-native.py', '--headers', $headers, '--output', $native)
if ([IO.Path]::GetFileNameWithoutExtension($Python) -eq 'py') { $pythonArguments = @('-3') + $pythonArguments }
& $Python @pythonArguments
if ($LASTEXITCODE -ne 0) { throw 'Ordinary driver services build failed' }
Copy-Item -LiteralPath (Join-Path $native 'erldrv-host-native.dll') -Destination $package
foreach ($file in @('erldrv-host-abi.json', 'erldrv-host-errno.json')) {
    Copy-Item -LiteralPath (Join-Path 'tools/Erlang.OtpCatalogValidation/native' $file) -Destination $package
}
$configuration = [ordered]@{}
foreach ($row in @(@('Abi', 'erldrv-host-abi.json'), @('Errno', 'erldrv-host-errno.json'), @('Bridge', 'erldrv-host-native.dll'))) {
    $configuration[$row[0] + 'File'] = $row[1]
    $configuration[$row[0] + 'Sha256'] = (Get-FileHash -LiteralPath (Join-Path $package $row[1]) -Algorithm SHA256).Hash
}
$configuration | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'erldrv-host-config.json') -Encoding utf8
$manifest = Join-Path $package 'erldrv-host-package.json'
$packageFiles = @(Get-ChildItem -LiteralPath $package -File | Sort-Object Name | ForEach-Object {
    [ordered]@{ Path = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
[ordered]@{ Protocol = 1; EntryPoint = 'Erlang.DriverHost.exe'; Files = $packageFiles } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifest -Encoding utf8
dotnet exec --fx-version 10.0.11 tools/Erlang.OtpCatalogValidation/bin/Release/net10.0/Erlang.OtpCatalogValidation.dll $PWD.Path $Admission $manifest (Join-Path $run 'results')
$status = $LASTEXITCODE
Confirm-CatalogHeaders
if ($status -ne 0) { throw "Configured original catalog failed: $run" }
Copy-Item -LiteralPath (Join-Path $run 'results/comparison.json') -Destination (Join-Path $root 'latest.json')
[ordered]@{ Directory = $run; Manifest = $manifest } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'latest-run.json') -Encoding utf8
Write-Host "Configured original catalog: 1321/1321, native execution and cleanup verified: $run"
