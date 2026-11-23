$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/boolean-dispatch-and-wx-remainder-1927'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/source-entry-failures-and-call-liveness-1926-dispatch-and-folded-mapper-retake'
$release='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$inputs='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118'
$deadline=(Get-Date).AddMinutes(40)
while(!(Test-Path "$s/corpus-bin/Erlang.BeamCorpus.dll") -or !(Test-Path "$s/focused-identity.json")){
 if((Get-Date) -gt $deadline){throw 'Compiler not ready for physical release capture'}
 Start-Sleep -Seconds 5
}
$focus=Get-Content "$s/focused-identity.json" -Raw|ConvertFrom-Json
$out=(New-Item "$s/early-raw-linked" -ItemType Directory).FullName
$env:TEMP=(New-Item "$out/temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT='1'
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES='1'
$env:ERLANG_BEAM_CORPUS_TIMINGS='1'
Copy-Item "$inputs/closed-callbacks-and-generated-defaults-1915/early-raw-linked/tool" "$out/tool" -Recurse
foreach($name in 'Compiler','Runtime'){Copy-Item "$s/corpus-bin/Erlang.$name.dll" "$out/tool/Erlang.$name.dll" -Force}
$compiler=(Get-FileHash "$out/tool/Erlang.Compiler.dll").Hash
$runtime=(Get-FileHash "$out/tool/Erlang.Runtime.dll").Hash
if($compiler -cne $focus.Compiler -or $runtime -cne $focus.Runtime){throw 'Physical capture assembly identities differ'}
$paths=@(Get-Content "$s/exact-new-files.txt")
$paths|Set-Content "$out/modules.txt"
$clock=[Diagnostics.Stopwatch]::StartNew()
& "$inputs/sdk/dotnet.exe" "$out/tool/Erlang.BeamCorpus.dll" $release "$out/otp-corpus.json" "$inputs/public-key-generated" "$inputs/pinned-inputs-full" --modules-file "$out/modules.txt" --jobs 1 > "$out/stdout.log" 2> "$out/stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$images=@(foreach($beam in $paths){
 $path="$out/otp-corpus.json.generated/$beam"
 $raw=(Get-FileHash $path).Hash
 if($raw -cne (Get-FileHash "$release/$beam").Hash){throw "Physical release differs $beam"}
 @{Path=$path;Beam=$beam;Sha256=$raw;Bytes=(Get-Item $path).Length}
})
@{Compiler=$compiler;Runtime=$runtime;Tool=(Get-FileHash "$out/tool/Erlang.BeamCorpus.dll").Hash;Elapsed=$clock.Elapsed.TotalSeconds;Image=$images}|ConvertTo-Json -Depth 5|Set-Content "$out/receipt.json"
0|Set-Content "$s/retain-exact.exit"
$native=(New-Item "$s/linked-runtime-witnesses" -ItemType Directory).FullName
$erl="$release/bin/erl.exe"
$env:ERL_CRASH_DUMP="$native/erl-crash.dump"
$w=[ordered]@{}
$proof="$inputs/remainder-call-frame-1946"
$image=$images[0]
if($images.Count -ne 1 -or [IO.Path]::GetFileNameWithoutExtension($image.Beam) -ne 'ex_grid'){throw 'Grid release path changed'}
$baseline=Get-Content "$proof/native/summary.json" -Raw | ConvertFrom-Json -AsHashtable
$oracle=Get-Content "$proof/observe.eval" -Raw
$term="$native/ex_grid.terms"
& $erl +S 1:1 +A 1 -noshell -eval $oracle -extra Release $image.Path "$proof/stubs" $term *> "$native/ex_grid.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$out=(Get-FileHash $term).Hash
if($out -ne $baseline['Otp-Release'].Effect){throw 'Grid release observations differ'}
$w['ex_grid']=@{Beam=$image.Sha256;Observations=1;Interactions=1289;Output=$out}
$fixture=(New-Item -ItemType Directory "$native/grid-fixtures").FullName
Copy-Item "$inputs/terminal-record-result-1909/final-fixture-bin/*" $fixture
Copy-Item "$s/corpus-bin/*.dll" $fixture -Force
if((Get-FileHash "$fixture/Erlang.Compiler.dll").Hash -ne $compiler){throw 'Grid fixture compiler drift'}
$cases=[ordered]@{}
foreach($part in 'Fixture','Control'){
 $beam="$fixture/$part.beam"
 & "$inputs/sdk/dotnet.exe" "$fixture/NewFixture.dll" "$proof/fixture-native/$part/beam_terminal_remainder_effects.erl" $beam *> "$fixture/$part-compile.log"
 if($LASTEXITCODE){exit $LASTEXITCODE}
 $terms="$fixture/$part.terms"
 & $erl +S 1:1 +A 1 -noshell -eval $oracle -extra $part $beam "$proof/stubs" $terms *> "$fixture/$part.log"
 if($LASTEXITCODE){exit $LASTEXITCODE}
 $actual=(Get-FileHash $terms).Hash
 if($actual -ne $baseline["Otp-$part"].Effect){throw "Grid fixture scenarios differ $part"}
 $cases[$part]=@{Output=$actual;Beam=(Get-FileHash $beam).Hash}
}
@{Compiler=$compiler;Runtime=$runtime;Witnesses=$w;Auxiliary=$cases;Scenarios=11}|ConvertTo-Json -Depth 7|Set-Content "$native/receipt.json"
exit 0