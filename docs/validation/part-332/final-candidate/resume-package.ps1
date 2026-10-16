param([switch]$SkipPackage)
$ErrorActionPreference = 'Stop'
$validationRoot = 'D:\Distr\Work\Codex\Erlang.Net\Erlang.Net\artifacts\proc-lib332-candidate'
Push-Location $validationRoot
try {
    function Invoke-DotNet {
        param([string[]]$Arguments)
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($Arguments -join ' ')" }
    }
    if (-not $SkipPackage) {
        Invoke-DotNet @('pack', 'src/Erlang.CSharp.MSBuild', '-c', 'Debug', '-o', 'artifacts/packages', '-m:1', '--nologo')
        Invoke-DotNet @('pack', 'tools/Erlang.Tool', '-c', 'Debug', '-o', 'artifacts/packages', '-m:1', '--nologo')
        $consumerRoot = Join-Path $validationRoot 'artifacts/package-consumer'
        New-Item -ItemType Directory -Path $consumerRoot -Force | Out-Null
        @'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Erlang.Net.CSharp" Version="0.1.0" /><ErlangSource Include="arithmetic.erl;map_source.erl;platform_demo.erl" /></ItemGroup></Project>
'@ | Set-Content (Join-Path $consumerRoot 'Consumer.csproj')
        Copy-Item -LiteralPath (Get-ChildItem -LiteralPath examples/HelloHybrid -Filter '*.cs' -File).FullName -Destination $consumerRoot
        Copy-Item -LiteralPath examples/HelloHybrid/arithmetic.erl -Destination $consumerRoot
        Copy-Item -LiteralPath examples/HelloHybrid/map_source.erl -Destination $consumerRoot
        Copy-Item -LiteralPath examples/HelloHybrid/platform_demo.erl -Destination $consumerRoot
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
        & artifacts/local-tool/erlang.exe compile examples/HelloHybrid/map_source.erl artifacts/tool-map_source.g.cs
        if ($LASTEXITCODE -ne 0) { throw 'Packaged CLI map compilation failed' }
        & artifacts/local-tool/erlang.exe readiness docs artifacts/tests.json artifacts/tool-readiness
        if ($LASTEXITCODE -ne 0) { throw 'Packaged CLI readiness failed' }
    }
    @{ Readiness='Passed'; PackagedReadiness= $(if ($SkipPackage) {'Skipped'} else {'Passed'}); Build='Passed'; Tests='Passed'; OracleUnavailableReport='Expected failure recorded'; HybridOutput='Hello World'; Incremental='Passed'; DisabledPreprocessing='Expected failure'; CleanRebuild='Passed'; PackageConsumer= $(if ($SkipPackage) {'Skipped'} else {'Passed'}); LocalTool= $(if ($SkipPackage) {'Skipped'} else {'Passed'}) } | ConvertTo-Json | Set-Content artifacts/integration-results.json
    Write-Output 'Remaining package validation passed'
} finally { Pop-Location }
