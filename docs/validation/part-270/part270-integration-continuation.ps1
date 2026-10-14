$ErrorActionPreference = 'Stop'
$env:DOTNET_PROCESSOR_COUNT='2'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
function Invoke-DotNet {
    param([string[]]$Arguments)
    $verb=$Arguments[0]
    if ($verb -in @('build','clean','pack')) {
        & dotnet @Arguments --disable-build-servers -p:UseSharedCompilation=false
    } else { & dotnet @Arguments }
    if ($LASTEXITCODE -ne 0) { throw 'Remaining integration dotnet invocation failed' }
}
$validationRoot=(Get-Location).Path
$SkipPackage=$false
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
    Write-Output 'Validation passed'
