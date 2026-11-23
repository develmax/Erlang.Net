$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/terminal-records-and-private-ber-shapes-1909'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/closed-atom-tails-catch-and-nested-binaries-1908-contravariant-entry-facts-retake'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$deadline=(Get-Date).AddMinutes(35)
while(!(Test-Path "$p/full-uncached/otp-corpus.json") -or !(Test-Path "$s/source-freeze.json") -or !(Test-Path "$s/pilot.json") -or !(Test-Path "$s/pilot-receipt.json")){
    if((Get-Date) -gt $deadline){throw 'Ready scope has not completed'}
    if(Test-Path "$s/focused-verified.log"){
        $last=Get-Content "$s/focused-verified.log" -Tail 1
        if($last -match '^\d+/129 passed$' -and $last -ne '129/129 passed'){throw "Focused gate failed: $last"}
    }
    Start-Sleep -Seconds 10
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
if((Get-FileHash $tool).Hash -ne $freeze.CorpusTool -or (Get-FileHash $compiler).Hash -ne $freeze.Compiler){
    throw 'Frozen compiler or profile tool changed'
}
$plan=Get-Content "$s/balanced-full-input/planning-receipt.json" -Raw | ConvertFrom-Json
foreach($part in $plan.Modules){if((Get-FileHash "$s/balanced-full-input/shard-$($part.Shard)/modules.txt").Hash -ne $part.SHA256){throw 'Measured partition changed'}}
$env:TEMP=(New-Item -ItemType Directory "$s/recovered-full-corpus-tmp").FullName
$env:TMP=$env:TEMP
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
@{Started=Get-Date -Format o;SourceZipSha256=$freeze.Source;TestSha256=$freeze.Tests;CompilerSha256=$freeze.Compiler;CorpusToolSha256=$freeze.CorpusTool;
    Partitions=$plan.Modules;Workers=4;CachedCompilerResults=$false;ScopedCount=$catalog.Modules;ScopedCounts=$actual.Counts} |
    ConvertTo-Json -Depth 9 | Set-Content "$s/full-launch.json"
& 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/run-frozen-corpus.ps1' -RunDirectory "$s/full-uncached" -CorpusBinary $tool -Dotnet $sdk -Release 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' -PublicKeyHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' -SourceHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' -ModulesDirectory "$s/balanced-full-input" -Workers 4 > "$s/launch-full-corpus.stdout.log" 2> "$s/launch-full-corpus.stderr.log"
exit $LASTEXITCODE


