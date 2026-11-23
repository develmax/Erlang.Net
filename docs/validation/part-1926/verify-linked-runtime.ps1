$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/source-entry-failures-and-call-liveness-1926-dispatch-and-folded-mapper-retake'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-prefixes-and-copied-registers-1925'
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
$cms='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/safecurves-existing-residual-1912/ordered-tagged-fallback-labels'
$image=$images[0]
$name='CryptographicMessageSyntax-2009'
if($images.Count -ne 1 -or [IO.Path]::GetFileNameWithoutExtension($image.Beam) -ne $name){throw 'CMS release path changed'}
$deadline=(Get-Date).AddMinutes(45)
while(!(Test-Path "$p/full-uncached/timing.json")){
 if((Get-Date) -ge $deadline){throw 'Parent actual CMS image not available'}
 Start-Sleep -Seconds 6
}
$old=Get-ChildItem "$p/full-uncached" -Directory -Filter 'shard-*' | ForEach-Object { "$($_.FullName)/otp-corpus.json.generated/$($image.Beam)" } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if(!$old){throw 'Prior CMS image unavailable'}
$term="$native/$name.terms"
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$cms/native-release.eval" -Raw) -extra "$release/$($image.Beam)" $old $image.Path $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if(!(Select-String -Path "$native/$name.stdout.log" -Pattern '70 candidate matches, 48 round trips, 22 rejected' -Quiet)){throw 'The CMS native scenarios differ'}
$w[$name]=@{Observations=70;Output=(Get-FileHash $term).Hash;Beam=$image.Sha256}
foreach($spec in @(
 @{Name='relay';Source='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/near-raw-residual-triage-1925/function-entry-failure-relays-verified-delivery-source';Stem='beam_function_failure_relay_probe';Eval='native-retake.eval';Receipt='behavior-retake';Cases=10},
 @{Name='mapper';Source='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/near-raw-residual-triage-1925/literal-append-in-list-mappers';Stem='beam_mapper_prefix_frame_probe';Eval='native.eval';Receipt='behavior';Cases=28}
)){
    $folder=(New-Item "$native/$($spec.Name)" -ItemType Directory).FullName
    Copy-Item "$inputs/terminal-record-result-1909/final-fixture-bin/*" $folder
    Copy-Item "$s/corpus-bin/*.dll" $folder -Force
    if((Get-FileHash "$folder/Erlang.Compiler.dll").Hash -cne $compiler){throw 'Linked source fixture changed compiler identity'}
    $beam="$folder/$($spec.Stem).beam"
    & "$inputs/sdk/dotnet.exe" "$folder/NewFixture.dll" "$($spec.Source)/$($spec.Stem).erl" $beam *> "$folder/compile.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    $out="$folder/output.terms"
    & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$($spec.Source)/$($spec.Eval)" -Raw) -extra $beam $out *> "$folder/native.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    $actual=(Get-FileHash $out).Hash
    if($actual -cne (Get-FileHash "$($spec.Source)/$($spec.Receipt)/Reference/native.terms").Hash){throw "Linked fixture outcomes differ $($spec.Name)"}
    @{Cases=$spec.Cases;Output=$actual;Beam=(Get-FileHash $beam).Hash;Compiler=$compiler}|ConvertTo-Json|Set-Content "$folder/receipt.json"
}
@{Compiler=$compiler;Runtime=$runtime;Witnesses=$w}|ConvertTo-Json -Depth 7|Set-Content "$native/receipt.json"
exit 0