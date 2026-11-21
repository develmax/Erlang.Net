$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-projections-and-protected-entry-1959-retake'
$first=Get-Content "$s/full-launch.json" -Raw|ConvertFrom-Json
$selected=(Get-Content "$s/replan-selection.json" -Raw|ConvertFrom-Json).Selected
foreach($part in $first.Partitions){
 if((Get-FileHash "$selected/shard-$($part.Shard)/modules.txt").Hash -ne $part.SHA256){throw 'Partition changed'}
}
foreach($item in @{Path="$s/source.zip";Hash=$first.SourceZipSha256},@{Path="$s/corpus-bin/Erlang.Compiler.dll";Hash=$first.CompilerSha256},@{Path="$s/corpus-bin/Erlang.BeamCorpus.dll";Hash=$first.CorpusToolSha256},@{Path="$s/corpus-bin/Erlang.Runtime.dll";Hash=$first.RuntimeSha256}){
 if((Get-FileHash -LiteralPath $item.Path).Hash -ne $item.Hash){throw "Frozen file changed: $($item.Path)"}
}
$boot=(Get-CimInstance Win32_OperatingSystem).LastBootUpTime
if($boot -le ([datetime]$first.Started) -or (Test-Path "$s/full-uncached/otp-corpus.json")){throw 'Host interruption not established'}
$save=(New-Item "$s/interrupted-20261120-1354" -ItemType Directory).FullName
Move-Item -LiteralPath "$s/full-uncached" -Destination "$save/full-uncached"
foreach($file in 'full-launch.json','launch-full-corpus.stdout.log','launch-full-corpus.stderr.log'){
 Move-Item -LiteralPath "$s/$file" -Destination "$save/$file"
}
$first.Started=Get-Date -Format o
$first|Add-Member -NotePropertyName PriorInterruptedStart -NotePropertyValue 'interrupted-20261120-1354/full-launch.json'
$first|Add-Member -NotePropertyName HostLastBoot -NotePropertyValue $boot.ToString('o')
$first|ConvertTo-Json -Depth 10|Set-Content "$s/full-launch.json"
$env:TEMP=(New-Item "$s/restarted-corpus-temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
& 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/artifacts/parity-next-local/run-frozen-corpus.ps1' -RunDirectory "$s/full-uncached" -CorpusBinary "$s/corpus-bin/Erlang.BeamCorpus.dll" -Dotnet 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe' -Release 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' -PublicKeyHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated' -SourceHeaders 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full' -ModulesDirectory $selected -Workers 5 > "$s/launch-full-corpus.stdout.log" 2> "$s/launch-full-corpus.stderr.log"
if($LASTEXITCODE){$LASTEXITCODE | Set-Content "$s/restarted.exit";exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/wait-then-collect-full-evidence.ps1" > "$s/restarted-evidence.stdout.log" 2> "$s/restarted-evidence.stderr.log"
$LASTEXITCODE | Set-Content "$s/restarted.exit"
exit $LASTEXITCODE