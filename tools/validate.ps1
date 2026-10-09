param([switch]$SkipPackage)
$ErrorActionPreference = 'Stop'
$validationRoot = Split-Path -Parent $PSScriptRoot
Push-Location $validationRoot
try {
    function Invoke-DotNet {
        param([string[]]$Arguments)
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($Arguments -join ' ')" }
    }
    Invoke-DotNet @('build', '-m:1', '--nologo')
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Tests', '--no-build', '--', 'artifacts/tests.json', 'docs/supported-mfas.json')
    $validationOutput = & dotnet run --project examples/HelloHybrid --no-build
    if ($LASTEXITCODE -ne 0 -or (($validationOutput -join "`n").Trim() -ne 'Hello World')) { throw 'Hybrid example output mismatch' }
    $generatedFiles = @(Get-ChildItem examples/HelloHybrid/obj/Debug/net10.0/erlang -Recurse -Filter '*.g.cs')
    if ($generatedFiles.Count -lt 2) { throw 'Generated sources missing' }
    $stamps = @{}
    foreach ($file in $generatedFiles) { $stamps[$file.FullName] = $file.LastWriteTimeUtc.Ticks }
    Invoke-DotNet @('build', 'examples/HelloHybrid', '-m:1', '--nologo')
    foreach ($file in $generatedFiles) { if ((Get-Item -LiteralPath $file.FullName).LastWriteTimeUtc.Ticks -ne $stamps[$file.FullName]) { throw "Incremental build rewrote $($file.Name)" } }
    $negativeOutput = & dotnet build examples/HelloHybrid -m:1 --nologo -p:ErlangPreprocessEnabled=false 2>&1
    $negativeExit = $LASTEXITCODE
    $negativeOutput | Set-Content artifacts/preprocessing-disabled.log
    if ($negativeExit -eq 0) { throw 'Build unexpectedly succeeded without preprocessing' }
    Invoke-DotNet @('clean', 'examples/HelloHybrid', '-m:1', '--nologo', '-v:quiet')
    Invoke-DotNet @('build', 'examples/HelloHybrid', '-m:1', '--nologo')
    $rebuiltOutput = & dotnet run --project examples/HelloHybrid --no-build
    if ($LASTEXITCODE -ne 0 -or (($rebuiltOutput -join "`n").Trim() -ne 'Hello World')) { throw 'Clean/rebuild output mismatch' }
    if (-not $SkipPackage) {
        Invoke-DotNet @('pack', 'src/Erlang.CSharp.MSBuild', '-c', 'Debug', '-o', 'artifacts/packages', '-m:1', '--nologo')
        Invoke-DotNet @('pack', 'tools/Erlang.Tool', '-c', 'Debug', '-o', 'artifacts/packages', '-m:1', '--nologo')
        $consumerRoot = Join-Path $validationRoot 'artifacts/package-consumer'
        New-Item -ItemType Directory -Path $consumerRoot -Force | Out-Null
        @'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Erlang.Net.CSharp" Version="0.1.0" /><ErlangSource Include="arithmetic.erl" /></ItemGroup></Project>
'@ | Set-Content (Join-Path $consumerRoot 'Consumer.csproj')
        Copy-Item -LiteralPath examples/HelloHybrid/Program.cs -Destination $consumerRoot
        Copy-Item -LiteralPath examples/HelloHybrid/CSharpFeatures.cs -Destination $consumerRoot
        Copy-Item -LiteralPath examples/HelloHybrid/arithmetic.erl -Destination $consumerRoot
        $feedPath = [System.Security.SecurityElement]::Escape((Join-Path $validationRoot 'artifacts/packages'))
        $consumerHash = (Get-FileHash artifacts/packages/Erlang.Net.CSharp.0.1.0.nupkg -Algorithm SHA256).Hash.Substring(0, 16)
        $toolHash = (Get-FileHash artifacts/packages/Erlang.Net.Tool.0.1.0.nupkg -Algorithm SHA256).Hash.Substring(0, 16)
        $cachePath = "packages-$consumerHash-$toolHash"
        "<configuration><packageSources><clear/><add key=`"local`" value=`"$feedPath`"/></packageSources><config><add key=`"globalPackagesFolder`" value=`"$cachePath`"/></config></configuration>" | Set-Content (Join-Path $consumerRoot 'NuGet.Config')
        Invoke-DotNet @('restore', (Join-Path $consumerRoot 'Consumer.csproj'), '--configfile', (Join-Path $consumerRoot 'NuGet.Config'), '--force')
        Invoke-DotNet @('build', (Join-Path $consumerRoot 'Consumer.csproj'), '-m:1', '--no-restore', '--nologo')
        $packageOutput = & dotnet run --project (Join-Path $consumerRoot 'Consumer.csproj') --no-build
        if ($LASTEXITCODE -ne 0 -or (($packageOutput -join "`n").Trim() -ne 'Hello World')) { throw 'PackageReference consumer output mismatch' }
        if (Test-Path -LiteralPath artifacts/local-tool/erlang.exe) { Invoke-DotNet @('tool', 'uninstall', 'Erlang.Net.Tool', '--tool-path', 'artifacts/local-tool') }
        Invoke-DotNet @('tool', 'install', 'Erlang.Net.Tool', '--version', '0.1.0', '--tool-path', 'artifacts/local-tool', '--configfile', (Join-Path $consumerRoot 'NuGet.Config'))
        & artifacts/local-tool/erlang.exe compile examples/HelloHybrid/arithmetic.erl artifacts/tool-arithmetic.g.cs
        if ($LASTEXITCODE -ne 0) { throw 'Packaged CLI failed' }
    }
    @{ Build='Passed'; Tests='Passed'; HybridOutput='Hello World'; Incremental='Passed'; DisabledPreprocessing='Expected failure'; CleanRebuild='Passed'; PackageConsumer= $(if ($SkipPackage) {'Skipped'} else {'Passed'}); LocalTool= $(if ($SkipPackage) {'Skipped'} else {'Passed'}) } | ConvertTo-Json | Set-Content artifacts/integration-results.json
    Write-Output 'Validation passed'
} finally { Pop-Location }
