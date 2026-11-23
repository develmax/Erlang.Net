$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-tuples-and-projected-entry-liveness-1961-linked-retake'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-recursive-entries-and-result-slots-1960-retake'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$deadline=(Get-Date).AddMinutes(35)
while(!(Test-Path "$p/full-uncached/otp-corpus.json") -or !(Test-Path "$s/source-freeze.json") -or !(Test-Path "$s/pilot.json") -or !(Test-Path "$s/pilot-receipt.json")){
    if((Get-Date) -gt $deadline){throw 'Ready scope has not completed'}
    if((Test-Path "$s/focused-verified.log") -and (Test-Path "$s/scope.json")){
        $last=Get-Content "$s/focused-verified.log" -Tail 1
        $expected=(Get-Content -LiteralPath "$s/scope.json" -Raw | ConvertFrom-Json).Cases
        if($last -match '^\d+/\d+ passed$' -and $last -ne "$expected/$expected passed"){throw "Focused gate failed: $last"}
    }
    Start-Sleep -Seconds 10
}
$ready="$p/finalize-receipts.exit"
while(!(Test-Path $ready)){
    if((Get-Date) -gt $deadline){throw 'Parent diagnostic and native gates are incomplete'}
    Start-Sleep -Seconds 5
}
if((Get-Content $ready -Raw).Trim() -ne '0'){throw 'Parent diagnostics require resolution'}
$parentRetention="$p/full-uncached/compare-summary.json"
while(!(Test-Path $parentRetention)){
 if((Get-Date) -gt $deadline){throw 'Parent release retention is unavailable'}
 Start-Sleep -Seconds 5
}
$parentState=Get-Content $parentRetention -Raw|ConvertFrom-Json
if(@($parentState.StatusChanges|Where-Object Before -eq 'Equal').Count){
 throw 'Parent source needs a raw regression retake'
}
$catalog=Get-Content "$s/scope.json" -Raw | ConvertFrom-Json
$focus=Get-Content "$s/focused-verified.json" -Raw | ConvertFrom-Json
$actual=Get-Content "$s/pilot.json" -Raw | ConvertFrom-Json
$prior=Get-Content "$p/full-uncached/otp-corpus.json" -Raw | ConvertFrom-Json
$old=@{}
foreach($m in $prior.Modules){$old[$m.Beam]=$m.Status}
$lost=@($actual.Modules|Where-Object {$old[$_.Beam] -eq 'Equal' -and $_.Status -ne 'Equal'})
if($actual.Modules.Count -ne $catalog.Modules -or $focus.Passed -ne $catalog.Cases -or $focus.Failed -or
    @($actual.Modules|Where-Object {$_.Status -notin 'Different','Equal'}).Count -or $lost.Count){
    $lost|ConvertTo-Json -Depth 5 | Set-Content "$s/scope-lost-previously-equal.json"
    throw 'Full release blocked by scoped coverage or raw byte loss'
}
$freeze=Get-Content "$s/source-freeze.json" -Raw | ConvertFrom-Json
$tool="$s/corpus-bin/Erlang.BeamCorpus.dll"
$compiler="$s/corpus-bin/Erlang.Compiler.dll"
if((Get-FileHash "$s/corpus-bin/Erlang.Runtime.dll").Hash -ne $freeze.Runtime){throw 'Frozen runtime changed'}
if((Get-FileHash $tool).Hash -ne $freeze.CorpusTool -or (Get-FileHash $compiler).Hash -ne $freeze.Compiler){
    throw 'Frozen compiler or profile tool changed'
}
$witness="$s/linked-runtime.exit"
while(!(Test-Path $witness)){
    if((Get-Date) -gt $deadline){throw 'Linked native behavior proof missing'}
    Start-Sleep -Seconds 3
}
if((Get-Content $witness -Raw).Trim() -ne '0'){throw 'Linked native behavior proof failed'}
$retakeWitness="$s/cold-recovery.exit"
while(!(Test-Path $retakeWitness)){
    if((Get-Date) -gt $deadline){throw 'Cold-order retake native proof missing'}
    Start-Sleep -Seconds 3
}
if((Get-Content $retakeWitness -Raw).Trim() -ne '0'){throw 'Cold-order retake native proof failed'}
$retake=Get-Content "$s/cold-recovery.json" -Raw|ConvertFrom-Json
if($retake.Compiler -ne $freeze.Compiler -or $retake.Runtime -ne $freeze.Runtime -or $retake.ActualCases -ne 6){
    throw 'Incorrect cold-order runtime proof identity'
}
$moduleDirectory="$s/balanced-five-full-input"
if(Test-Path "$p/full-uncached/timing.json"){
    $receipt="$s/replanned-five-input/planning-receipt.json"
    if(!(Test-Path -LiteralPath $receipt)){
        & 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/replan-from-parent-times.py" "$p/full-uncached" "$s/replanned-five-input" > "$s/replan.stdout.log" 2> "$s/replan.stderr.log"
        if($LASTEXITCODE){exit $LASTEXITCODE}
    }
    $model=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    if((Get-FileHash "$p/full-uncached/timing.json").Hash -ne $model.Sources.OldFullTimingSha256 -or
        (Get-FileHash "$p/full-uncached/otp-corpus.json").Hash -ne $model.Sources.OldCorpusSha256 -or
        $model.count -ne 1366 -or $model.Workers -ne 5 -or $model.CompileResultsReused){
        throw 'Measured partition source or dimensions differ'
    }
    $reason='Preserved existing baseline partition'
    if(!(Test-Path -LiteralPath "$moduleDirectory/planning-receipt.json") -or $model.ExpectedStragglerReduction -ge 0.03){
        $reason=if(Test-Path -LiteralPath "$moduleDirectory/planning-receipt.json"){'Measured expected improvement'}else{'Only available complete measured partition'}
        $moduleDirectory="$s/replanned-five-input"
    }
    @{ExpectedReduction=$model.ExpectedStragglerReduction;MeasuredParent=$p;Selected=$moduleDirectory;Reason=$reason} |
        ConvertTo-Json | Set-Content "$s/replan-selection.json"
}
$plan=Get-Content "$moduleDirectory/planning-receipt.json" -Raw | ConvertFrom-Json
foreach($part in $plan.Modules){if((Get-FileHash "$moduleDirectory/shard-$($part.Shard)/modules.txt").Hash -ne $part.SHA256){throw 'Measured partition changed'}}
$env:TEMP=(New-Item -ItemType Directory "$s/full-corpus-tmp").FullName
$env:TMP=$env:TEMP
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
@{Started=Get-Date -Format o;SourceZipSha256=$freeze.Source;TestSha256=$freeze.Tests;CompilerSha256=$freeze.Compiler;RuntimeSha256=$freeze.Runtime;CorpusToolSha256=$freeze.CorpusTool;
    Partitions=$plan.Modules;Workers=$plan.Workers;CachedCompilerResults=$false;ScopedCount=$catalog.Modules;ScopedCounts=$actual.Counts} |
    ConvertTo-Json -Depth 9 | Set-Content "$s/full-launch.json"
& 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/run-frozen-corpus.ps1' -RunDirectory "$s/full-uncached" -CorpusBinary $tool -Dotnet $sdk -Release 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' -PublicKeyHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' -SourceHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' -ModulesDirectory $moduleDirectory -Workers $plan.Workers > "$s/launch-full-corpus.stdout.log" 2> "$s/launch-full-corpus.stderr.log"
exit $LASTEXITCODE









