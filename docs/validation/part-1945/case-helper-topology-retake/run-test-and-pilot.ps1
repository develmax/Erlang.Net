$ErrorActionPreference='Stop'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945'
$work='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
Set-Location -LiteralPath $work
$build=@('--no-restore','--no-dependencies','-m:1','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false')
& $sdk build tests/Erlang.Tests/Erlang.Tests.csproj @build *> "$stage/tests-build.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$bin="$work/tests/Erlang.Tests/bin/Debug/net10.0"
$compiler="$bin/Erlang.Compiler.dll"
$compilerHash=(Get-FileHash $compiler).Hash
$mainCompiler="$work/src/Compiler/Erlang.Compiler/bin/Debug/net10.0/Erlang.Compiler.dll"
if((Get-FileHash $mainCompiler).Hash -cne $compilerHash){throw 'Compiler copied into tests differs'}
$runtimeHash=(Get-FileHash "$bin/Erlang.Runtime.dll").Hash
if((Get-FileHash "$work/src/Runtime/Erlang.Runtime.Core/bin/Debug/net10.0/Erlang.Runtime.dll").Hash -cne $runtimeHash){throw 'Runtime copied into tests differs'}
@{SourceParent=$receipt.Parent.Source;Compiler=$compilerHash;Runtime=$runtimeHash;Tests=(Get-FileHash "$bin/Erlang.Tests.dll").Hash;Started=Get-Date -Format o} |
    ConvertTo-Json | Set-Content "$stage/focused-identity.json"
$catalog=Get-Content "$stage/scope.json" -Raw | ConvertFrom-Json
$arguments=@("$bin/Erlang.Tests.dll",'--report',"$stage/focused-fast.json")
$cases=@(Get-Content "$stage/fast-test-names.txt")
if($cases.Count -ne $catalog.FastCases){throw 'Selected test manifest mismatch'}
foreach($name in $cases){$arguments+='--case';$arguments+=$name}
$env:DOTNET_PROCESSOR_COUNT=$null
(Get-Process -Id $PID).PriorityClass='Normal'
& $sdk @arguments *> "$stage/focused-fast.log"
$result=$LASTEXITCODE
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$env:DOTNET_PROCESSOR_COUNT='1'
if($result){exit $result}
$report=Get-Content "$stage/focused-fast.json" -Raw | ConvertFrom-Json
if($report.TotalCount -ne $catalog.TotalCatalog -or $report.Passed -ne $catalog.FastCases -or $report.Failed){throw 'Focused report coverage differs'}
$toolBuild=@('--no-dependencies','-m:1','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false')
& $sdk build tools/Validation/Erlang.BeamCorpus/Erlang.BeamCorpus.csproj @toolBuild *> "$stage/corpus-tool-build.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
Copy-Item -LiteralPath "$work/tools/Validation/Erlang.BeamCorpus/bin/Debug/net10.0" -Destination "$stage/corpus-bin" -Recurse
if((Get-FileHash "$stage/corpus-bin/Erlang.Compiler.dll").Hash -cne $compilerHash){throw 'Compiler copied into corpus differs'}
if((Get-FileHash "$stage/corpus-bin/Erlang.Runtime.dll").Hash -cne $runtimeHash){throw 'Runtime copied into corpus differs'}
$memory=(Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory
$scopeWorkers=2
if($memory -ge 10 * 1024 * 1024){$scopeWorkers=4}
elseif($memory -ge 6 * 1024 * 1024){$scopeWorkers=3}
@{Workers=$scopeWorkers;AvailableMemoryKiB=$memory;Reason='Bound scoped workers alongside the active full lane'} |
    ConvertTo-Json | Set-Content "$stage/pilot-resource-plan.json"
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/run-planned-pilot.py" $stage 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/terminal-arms-and-comprehension-sources-1943' $sdk 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' $scopeWorkers *> "$stage/pilot.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$deadline=(Get-Date).AddMinutes(40)
while(!(Test-Path 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/response-roots-and-sequential-comprehensions-1944/full-uncached/otp-corpus.json')){
    if((Get-Date) -ge $deadline){throw 'No complete parent corpus for scoped comparison'}
    Start-Sleep -Seconds 5
}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/compare-cohort.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/response-roots-and-sequential-comprehensions-1944/full-uncached' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot-delta"
exit $LASTEXITCODE















