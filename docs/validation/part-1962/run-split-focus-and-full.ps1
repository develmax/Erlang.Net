$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-branch-frames-and-proper-list-arms-1962-linked'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-tuples-and-projected-entry-liveness-1961-linked-retake'
$work='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$catalog=Get-Content "$s/scope.json" -Raw | ConvertFrom-Json
$fast=Get-Content "$s/focused-fast.json" -Raw|ConvertFrom-Json
$report=Get-Content "$s/pilot.json" -Raw|ConvertFrom-Json
$parent=Get-Content "$p/full-uncached/otp-corpus.json" -Raw|ConvertFrom-Json
$target=@($report.Modules | Where-Object Beam -eq 'lib\common_test-1.31.2\ebin\ct_default_gl.beam')
if($target.Count -ne 1 -or $target[0].Status -ne 'Equal'){throw 'ct_default_gl whole-image closure still needs diagnosis'}
$gauge=@($report.Modules|Where-Object Beam -like '*\doc\examples\demo\ex_gauge.beam')
if($gauge.Count -ne 1 -or $gauge[0].Status -ne 'Equal'){throw 'Linked ex_gauge whole-image closure still needs diagnosis'}
$previous=@{}
foreach($row in $parent.Modules){$previous[$row.Beam]=$row.Status}
$lost=@($report.Modules|Where-Object {$previous[$_.Beam] -eq 'Equal' -and $_.Status -ne 'Equal'})
$bad=@($report.Modules|Where-Object {$_.Status -notin 'Equal','Different'})
if($lost.Count -or $bad.Count -or $report.Modules.Count -ne $catalog.Modules){
 @{PreviouslyEqual=$lost;Other=$bad} | ConvertTo-Json -Depth 8 | Set-Content "$s/scoped-release-errors.json"
 throw 'Pilot coverage, compilation or exact-image retention failed'
}
& pwsh -NoProfile -File "$s/verify-scoped-disassembly.ps1" > "$s/scoped-disasm-driver.stdout.log" 2> "$s/scoped-disasm-driver.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$bin="$work/tests/Erlang.Tests/bin/Debug/net10.0"
$identity=Get-Content "$s/focused-identity.json" -Raw|ConvertFrom-Json
if((Get-FileHash "$bin/Erlang.Tests.dll").Hash -cne $identity.Tests -or
   (Get-FileHash "$bin/Erlang.Compiler.dll").Hash -cne $identity.Compiler){throw 'Selected test payload changed'}
$env:TEMP=(New-Item -ItemType Directory "$s/whole-fixture-temp").FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT=$null
& $sdk "$bin/Erlang.Tests.dll" --case 'compiler/beam/whole-module-matches-otp' --report "$s/focused-whole-fixture.json" > "$s/focused-whole.stdout.log" 2> "$s/focused-whole.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$single=Get-Content "$s/focused-whole-fixture.json" -Raw|ConvertFrom-Json
$coverage=@($fast.ExactCases)+@($single.ExactCases)
$expected=Get-Content "$s/focused-test-names.txt"
if($fast.TotalCount -ne $catalog.TotalCatalog -or $single.TotalCount -ne $catalog.TotalCatalog -or
   $fast.Passed -ne $catalog.FastCases -or $single.Passed -ne 1 -or
   ($coverage|Sort-Object -Unique).Count -ne $catalog.Cases -or (Compare-Object $expected $coverage)){throw 'Selected test coverage differs'}
@{TotalCount=$catalog.TotalCatalog;SelectedCount=$catalog.Cases;Passed=$catalog.Cases;Failed=0;
  FastReportSha256=(Get-FileHash "$s/focused-fast.json").Hash;WholeFixtureSha256=(Get-FileHash "$s/focused-whole-fixture.json").Hash}
|ConvertTo-Json|Set-Content "$s/focused-verified.json"
Copy-Item -LiteralPath $bin -Destination "$s/frozen-payload" -Recurse
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/freeze-source.py" $s 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-tuples-and-projected-entry-liveness-1961-linked-retake' $s $work > "$s/freeze.log" 2> "$s/freeze.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/bind-source-closure.py" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-tuples-and-projected-entry-liveness-1961-linked-retake' $s $work > "$s/closure.log" 2> "$s/closure.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
Copy-Item -LiteralPath "$p/assembly-source-verification.json" -Destination $s
& pwsh -NoProfile -File "$s/launch-after-split-focus.ps1" > "$s/full-orchestration.stdout.log" 2> "$s/full-orchestration.stderr.log"
exit $LASTEXITCODE