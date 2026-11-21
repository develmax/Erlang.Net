$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/local-streams-and-shared-ber-1911'
$work='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
Set-Location -LiteralPath $work
$env:TEMP=(New-Item "$stage/test-adapter-temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT='1'
$receipt=Get-Content "$stage/hydration-receipt.json" -Raw | ConvertFrom-Json
$sourceFiles=$receipt.CandidateChangedPaths.PSObject.Properties
$digests=foreach($file in $sourceFiles){
    [pscustomobject]@{Path=$file.Name;Sha256=(Get-FileHash -LiteralPath "$work/$($file.Name)").Hash}
}
$digests | ConvertTo-Json | Set-Content "$stage/source-identities.json"
$build=@('--no-restore','--no-dependencies','-m:1','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false')
$production="$work/src/Compiler/Erlang.Compiler/bin/Debug/net10.0/Erlang.Compiler.dll"
$prior=Get-Content "$stage/reused-compiler-build.json" -Raw|ConvertFrom-Json
if((Get-FileHash $production).Hash -ne $prior.Compiler){throw 'Production compiler changed after the original green build'}
& $sdk build tests/Erlang.Tests/Erlang.Tests.csproj @build *> "$stage/tests-build.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$bin="$work/tests/Erlang.Tests/bin/Debug/net10.0"
$compiler="$bin/Erlang.Compiler.dll"
$compilerHash=(Get-FileHash $compiler).Hash
$mainCompiler="$work/src/Compiler/Erlang.Compiler/bin/Debug/net10.0/Erlang.Compiler.dll"
if((Get-FileHash $mainCompiler).Hash -cne $compilerHash){throw 'Compiler copied into tests differs'}
@{SourceParent=$receipt.Parent.Source;Compiler=$compilerHash;Tests=(Get-FileHash "$bin/Erlang.Tests.dll").Hash;Started=Get-Date -Format o} |
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
$env:DOTNET_PROCESSOR_COUNT='2'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES='1'
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
$timer=[Diagnostics.Stopwatch]::StartNew()
& $sdk "$stage/corpus-bin/Erlang.BeamCorpus.dll" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' --modules-file "$stage/pilot-modules.txt" --jobs 2 *> "$stage/pilot.log"
$result=$LASTEXITCODE
$timer.Stop()
@{ExitCode=$result;Seconds=$timer.Elapsed.TotalSeconds;Count=$catalog.Modules} | ConvertTo-Json | Set-Content "$stage/pilot-receipt.json"
if($result -gt 1 -or !(Test-Path "$stage/pilot.json")){exit $result}
$deadline=(Get-Date).AddMinutes(40)
while(!(Test-Path 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-record-liveness-and-ber-entry-1910/full-uncached/otp-corpus.json')){
    if((Get-Date) -ge $deadline){throw 'No complete parent corpus for scoped comparison'}
    Start-Sleep -Seconds 5
}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/compare-cohort.py' 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-record-liveness-and-ber-entry-1910/full-uncached' "$stage/pilot.json" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/pilot-delta"
exit $LASTEXITCODE










