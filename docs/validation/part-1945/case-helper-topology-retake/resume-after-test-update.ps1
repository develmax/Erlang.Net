$ErrorActionPreference='Stop'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945'
$work='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
Set-Location -LiteralPath $work
$proof="$stage/case-helper-topology-retake"
$env:TEMP=(New-Item -ItemType Directory "$proof/temp").FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT='1'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$old=Get-Content "$proof/initial-focused-identity.json" -Raw|ConvertFrom-Json
$build=@('--no-restore','--no-dependencies','-m:1','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false')
$existing="$work/src/Compiler/Erlang.Compiler/bin/Debug/net10.0/Erlang.Compiler.dll"
if((Get-FileHash $existing).Hash -ne $old.Compiler){throw 'Compiler already drifted'}
$layout=(New-Item -ItemType Directory "$proof/layout").FullName
foreach($direction in 'src','tests','tools','examples','benchmarks'){New-Item -ItemType Directory "$layout/$direction"|Out-Null}
$relative='tests/Erlang.Tests/Compiler/Beam/BeamFlatTupleListHelperChecks.cs'
$receipt=Get-Content "$proof/receipt.json" -Raw|ConvertFrom-Json
if((Get-FileHash "$work/$relative").Hash -ne $receipt.After){throw 'Expected fixture correction missing'}
Copy-Item -LiteralPath "$work/$relative" -Destination "$layout/tests"
$style='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/immediate-timeout-and-constructor-alias-1862/adjacency-fixture-retake/frozen-payload/Erlang.Style.dll'
& $sdk $style --check $layout > "$proof/layout-check.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$proof/run-test-and-pilot.ps1" > "$proof/driver.stdout.log" 2> "$proof/driver.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$new=Get-Content "$stage/focused-identity.json" -Raw|ConvertFrom-Json
if($new.Compiler -ne $old.Compiler -or $new.Runtime -ne $old.Runtime -or $new.Tests -eq $old.Tests){throw 'Retake must keep compiler/runtime and rebuild tests'}
& pwsh -NoProfile -File "$stage/run-split-focus-and-full.ps1" > "$stage/split-orchestration.stdout.log" 2> "$stage/split-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$stage/wait-then-collect-full-evidence.ps1" > "$stage/full-evidence-driver.stdout.log" 2> "$stage/full-evidence-driver.stderr.log"
exit $LASTEXITCODE

