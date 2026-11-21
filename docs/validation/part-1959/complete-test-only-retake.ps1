$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-projections-and-protected-entry-1959-retake'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-projections-and-protected-entry-1959'
$b='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ber-continuations-and-owned-frame-frontiers-1958'
$work='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
$refs='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$py='C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
Set-Location -LiteralPath $work
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$base=Get-Content "$p/source-freeze.json" -Raw|ConvertFrom-Json
$local="$work/tests/Erlang.Tests/bin/Debug/net10.0"
if((Get-FileHash "$local/Erlang.Compiler.dll").Hash -ne $base.Compiler -or
   (Get-FileHash "$local/Erlang.Runtime.dll").Hash -ne $base.Runtime){throw 'The original linked compiler or runtime changed'}
$env:TEMP=(New-Item "$s/temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT='1'
& $sdk build tests/Erlang.Tests/Erlang.Tests.csproj --no-restore --no-dependencies -m:1 -p:UseSharedCompilation=false -p:ConcurrentBuild=false *> "$s/tests-build.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if((Get-FileHash "$local/Erlang.Compiler.dll").Hash -ne $base.Compiler -or
   (Get-FileHash "$local/Erlang.Runtime.dll").Hash -ne $base.Runtime){throw 'Test-only retake changed production payload'}
@{SourceParent=$base.Source;Compiler=(Get-FileHash "$local/Erlang.Compiler.dll").Hash;Runtime=(Get-FileHash "$local/Erlang.Runtime.dll").Hash;Tests=(Get-FileHash "$local/Erlang.Tests.dll").Hash;Started=Get-Date -Format o} |
    ConvertTo-Json | Set-Content "$s/focused-identity.json"
$env:DOTNET_PROCESSOR_COUNT=$null
& $py "$s/run-fast-splits.py" $s $sdk "$local/Erlang.Tests.dll" > "$s/focused-fast.log" 2> "$s/focused-fast.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$env:DOTNET_PROCESSOR_COUNT='1'
$deadline=(Get-Date).AddMinutes(15)
while(!(Test-Path "$p/blocked-pilot.exit") -or !(Test-Path "$p/linked-runtime.exit")){
 if((Get-Date) -gt $deadline){throw 'Original linked production evidence incomplete'}
 Start-Sleep -Seconds 4
}
if((Get-Content "$p/blocked-pilot.exit" -Raw).Trim() -ne '0' -or
   (Get-Content "$p/linked-runtime.exit" -Raw).Trim() -ne '0'){throw 'Original linked production scope or native behavior failed'}
& $py 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-call-projection-1853/verify-inputs.py' $refs 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118' 'D:/Distr/Work/Codex/Erlang.Net/otp' > "$s/scoped-input-verification.json" 2> "$s/scoped-input-verification.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $py "$s/bind-test-only-evidence.py" $s $p $b $work > "$s/test-only-evidence.stdout.log" 2> "$s/test-only-evidence.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $py 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/compare-cohort.py' "$b/full-uncached" "$s/pilot.json" $refs "$s/pilot-delta" > "$s/pilot-compare.stdout.log" 2> "$s/pilot-compare.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/run-split-focus-and-full.ps1" > "$s/split-orchestration.stdout.log" 2> "$s/split-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/wait-then-collect-full-evidence.ps1" > "$s/full-evidence-driver.stdout.log" 2> "$s/full-evidence-driver.stderr.log"
exit $LASTEXITCODE