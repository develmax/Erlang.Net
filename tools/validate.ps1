param([switch]$SkipPackage, [string]$TestRuntimeVersion)
$ErrorActionPreference = 'Stop'
$validationRoot = Split-Path -Parent $PSScriptRoot
Push-Location $validationRoot
try {
    # Reviewed source-plan/workflow preflight: run before the first build.
    $reviewedPlanContracts = @(
        @{ Plan = 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'; Workflow = '.github/workflows/nif-pending-native.yml'; Pin = 'REVIEWED_SOURCE_PLAN_SHA256' },
        @{ Plan = 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'; Workflow = '.github/workflows/nif-reallocation-native.yml'; Pin = 'REVIEWED_REALLOCATION_PLAN_SHA256' }
    )
    $reviewedPlanUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
    foreach ($contract in $reviewedPlanContracts) {
        $planPath = Join-Path $validationRoot $contract.Plan
        $workflowPath = Join-Path $validationRoot $contract.Workflow
        $planBytes = [System.IO.File]::ReadAllBytes($planPath)
        $planText = $reviewedPlanUtf8.GetString($planBytes)
        if ($planBytes -contains 13 -or ($planBytes.Length -ge 3 -and $planBytes[0] -eq 239 -and $planBytes[1] -eq 187 -and $planBytes[2] -eq 191)) {
            throw "Reviewed source plan requires UTF-8 LF bytes without BOM: $($contract.Plan)"
        }
        $canonicalBytes = $reviewedPlanUtf8.GetBytes($planText.Replace("`r`n", "`n"))
        $planHasher = [System.Security.Cryptography.SHA256]::Create()
        try {
            $canonicalSha = [BitConverter]::ToString($planHasher.ComputeHash($canonicalBytes)).Replace('-', '')
        } finally { $planHasher.Dispose() }
        $rawSha = (Get-FileHash -LiteralPath $planPath -Algorithm SHA256).Hash
        if ($rawSha -cne $canonicalSha) { throw "Reviewed source plan raw/canonical hash differs: $($contract.Plan)" }
        $workflowText = [System.IO.File]::ReadAllText($workflowPath, $reviewedPlanUtf8)
        $pinPattern = '(?m)^[ \t]+' + [regex]::Escape($contract.Pin) + ':[ \t]+([A-Fa-f0-9]{64})[ \t]*\r?$'
        $pins = [regex]::Matches($workflowText, $pinPattern)
        if ($pins.Count -ne 1 -or $pins[0].Groups[1].Value -cne $rawSha) {
            throw "Reviewed source plan/workflow pin mismatch: $($contract.Plan)"
        }
    }
    # End reviewed source-plan/workflow preflight.
    function Invoke-DotNet {
        param([string[]]$Arguments)
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($Arguments -join ' ')" }
    }
    Invoke-DotNet @('build', '-m:1', '--nologo')
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Library.Tests', '--no-build')
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Kernel.Library.Tests', '--no-build', '--', 'artifacts/otp-kernel-values.txt')
    & (Join-Path $PSScriptRoot 'validate-otp-kernel-original.ps1') -ManagedValues 'artifacts/otp-kernel-values.txt'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Boot.Library.Tests', '--no-build', '--', 'artifacts/otp-boot-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-boot-original.ps1') -ManagedDirectory 'artifacts/otp-boot-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Syntax.Library.Tests', '--no-build', '--', 'artifacts/otp-syntax-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-syntax-original.ps1') -ManagedDirectory 'artifacts/otp-syntax-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Compiler.Library.Tests', '--no-build', '--', 'artifacts/otp-compiler-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-compiler-original.ps1') -ManagedDirectory 'artifacts/otp-compiler-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Tools.Library.Tests', '--no-build', '--', 'artifacts/otp-tools-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-tools-original.ps1') -ManagedDirectory 'artifacts/otp-tools-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Documentation.Library.Tests', '--no-build', '--', 'artifacts/otp-documentation-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-documentation-original.ps1') -ManagedDirectory 'artifacts/otp-documentation-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Mnesia.Library.Tests', '--no-build', '--', 'artifacts/otp-mnesia-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-mnesia-original.ps1') -ManagedDirectory 'artifacts/otp-mnesia-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Asn1.Library.Tests', '--no-build', '--', 'artifacts/otp-asn1-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-asn1-original.ps1') -ManagedDirectory 'artifacts/otp-asn1-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Security.Library.Tests', '--no-build', '--', 'artifacts/otp-security-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-security-original.ps1') -ManagedDirectory 'artifacts/otp-security-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Transport.Library.Tests', '--no-build', '--', 'artifacts/otp-transport-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-transport-original.ps1') -ManagedDirectory 'artifacts/otp-transport-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Network.Library.Tests', '--no-build', '--', 'artifacts/otp-network-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-network-original.ps1') -ManagedDirectory 'artifacts/otp-network-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Management.Library.Tests', '--no-build', '--', 'artifacts/otp-management-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-management-original.ps1') -ManagedDirectory 'artifacts/otp-management-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Megaco.Library.Tests', '--no-build', '--', 'artifacts/otp-megaco-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-megaco-original.ps1') -ManagedDirectory 'artifacts/otp-megaco-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.TestAnalysis.Library.Tests', '--no-build', '--', 'artifacts/otp-testanalysis-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-testanalysis-original.ps1') -ManagedDirectory 'artifacts/otp-testanalysis-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Platform.Library.Tests', '--no-build', '--', 'artifacts/otp-platform-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-platform-original.ps1') -ManagedDirectory 'artifacts/otp-platform-managed'
    Invoke-DotNet @('run', '--project', 'tests/Erlang.Otp.Gui.Library.Tests', '--no-build', '--', 'artifacts/otp-gui-managed')
    & (Join-Path $PSScriptRoot 'validate-otp-gui-original.ps1') -ManagedDirectory 'artifacts/otp-gui-managed'
    foreach ($listsBackend in @('Managed', 'Beam')) {
        foreach ($queueBackend in @('Managed', 'Beam')) {
            Invoke-DotNet @('run', '--project', 'examples/HelloOtpLibraryReference', '--no-build', '--', $listsBackend, $queueBackend)
        }
    }
    Invoke-DotNet @('run', '--project', 'tools/Erlang.Style', '--no-build', '--', '--check', '.')
    Invoke-DotNet @('run', '--project', 'tools/Erlang.NifNativeBackend', '--no-build')
    if ($TestRuntimeVersion) {
        if ($TestRuntimeVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid explicit test runtime version' }
        # Native managed-object census has a source/hash-admitted CLR profile.
        # Execute the identical full test assembly on that explicit runtime.
        Invoke-DotNet @('exec', '--fx-version', $TestRuntimeVersion, 'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll', 'artifacts/tests.json', 'docs/supported-mfas.json')
    } else {
        Invoke-DotNet @('run', '--project', 'tests/Erlang.Tests', '--no-build', '--', 'artifacts/tests.json', 'docs/supported-mfas.json')
    }
    & (Join-Path $PSScriptRoot 'validate-dynamic-apply-original.ps1')
    & (Join-Path $PSScriptRoot 'validate-float-store-original.ps1')
    & (Join-Path $PSScriptRoot 'validate-closed-wait-original.ps1')
    & (Join-Path $PSScriptRoot 'validate-frame-original.ps1')
    & (Join-Path $PSScriptRoot 'validate-comparison-original.ps1')
    & (Join-Path $PSScriptRoot 'validate-receive-original.ps1')
    Invoke-DotNet @('run', '--project', 'tools/Erlang.Tool', '--no-build', '--', 'readiness', 'docs', 'artifacts/tests.json', 'artifacts/readiness')
    $missingOracle = Join-Path $validationRoot 'artifacts/nonexistent-oracle/erl'
    if (Test-Path -LiteralPath $missingOracle) { throw 'Negative oracle fixture must not exist' }
    $oracleFailureOutput = & dotnet run --project tools/Erlang.Differential --no-build -- $missingOracle artifacts/oracle-unavailable.json 2>&1
    if ($LASTEXITCODE -ne 2) { throw 'Unavailable oracle did not return infrastructure exit code 2' }
    $oracleFailureOutput | Set-Content artifacts/oracle-unavailable.log
    $oracleFailure = Get-Content artifacts/oracle-unavailable.json -Raw | ConvertFrom-Json
    if ($oracleFailure.Complete -or $oracleFailure.VersionVerified -or $oracleFailure.Executed -ne 0 -or -not $oracleFailure.InfrastructureError) { throw 'Unavailable oracle evidence is incomplete or incorrectly marked successful' }
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
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages')
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Kernel
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Boot
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Syntax
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Compiler
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Tools
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Documentation
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Mnesia
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Asn1
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Security
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Transport
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Network
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Management
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Megaco
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library TestAnalysis
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Platform
        & (Join-Path $PSScriptRoot 'validate-otp-library-package.ps1') -Feed (Join-Path $validationRoot 'artifacts/packages') -Library Gui
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
} finally { Pop-Location }
