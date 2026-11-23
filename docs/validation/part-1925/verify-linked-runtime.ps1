$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-prefixes-and-copied-registers-1925'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/list-values-and-register-projections-1924'
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
$crmf='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/safecurves-existing-residual-1912/checked-binary-measure-unit'
$image=$images[0]
if($images.Count -ne 1 -or [IO.Path]::GetFileNameWithoutExtension($image.Beam) -ne 'PKIXCRMF-2009'){throw 'CRMF release path changed'}
$old=Get-ChildItem "$p/full-uncached" -Directory -Filter 'shard-*' | ForEach-Object { "$($_.FullName)/otp-corpus.json.generated/$($image.Beam)" } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if(!$old){throw 'Prior CRMF image unavailable'}
$term="$native/PKIXCRMF-2009.terms"
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$crmf/native-release.eval" -Raw) -extra "$release/$($image.Beam)" $old $image.Path $term > "$native/PKIXCRMF-2009.stdout.log" 2> "$native/PKIXCRMF-2009.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if(!(Select-String "$native/PKIXCRMF-2009.stdout.log" -Pattern '57 candidate matches, 39 round trips, 14 rejected' -Quiet)){throw 'The CRMF native scenarios differ'}
$w['PKIXCRMF-2009']=@{Observations=57;Output=(Get-FileHash $term).Hash;Beam=$image.Sha256}
$deadline=(Get-Date).AddMinutes(20)
while(!(Test-Path "$s/pilot-receipt.json")){
 if((Get-Date) -ge $deadline){throw 'Linked wx images unavailable'}
 Start-Sleep -Seconds 5
}
$wx='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-grid-ownership-1943/f-replay'
$root=Split-Path $wx -Parent
$observe=Get-Content "$root/observe-complete.eval" -Raw
$baseline=Get-Content "$wx/native-complete/summary.json" -Raw | ConvertFrom-Json -AsHashtable
foreach($module in 'ex_aui','ex_gl','ex_grid','ex_webview'){
 $beam="$s/pilot.json.generated/lib/wx-2.7/doc/examples/demo/$module.beam"
 if(!(Test-Path $beam)){throw "Scoped generated wx image unavailable $module"}
 $out="$native/$module.terms"
 & $erl +S 1:1 +A 1 -noshell -eval $observe -extra Release $beam "$root/stubs" $out *> "$native/$module.log"
 if($LASTEXITCODE){exit $LASTEXITCODE}
 $expected=$baseline["Otp-$module"]
 if((Get-FileHash $out).Hash -cne $expected.Effect -or (Get-FileHash "$native/$module.log").Hash -cne $expected.Stdout){throw "Linked wx observations differ $module"}
 $w[$module]=@{Observations=4;Output=(Get-FileHash $out).Hash;Beam=(Get-FileHash $beam).Hash}
}
$beam="$s/pilot.json.generated/lib/wx-2.7/doc/examples/demo/ex_grid.beam"
$out="$native/GridEvent.terms"
& $erl +S 1:1 +A 1 -noshell -eval $observe -extra GridEvent $beam "$root/stubs" $out *> "$native/GridEvent.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if((Get-FileHash $out).Hash -cne $baseline['Otp-GridEvent'].Effect){throw 'Linked grid event observations differ'}
$w['ex_grid'].Observations+=4
$prefix='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/string-table-final-use-census-1923/long-open-literal-prefix'
$fixture=(New-Item "$native/fixed-prefix" -ItemType Directory).FullName
Copy-Item "$inputs/terminal-record-result-1909/final-fixture-bin/*" $fixture
Copy-Item "$s/corpus-bin/*.dll" $fixture -Force
if((Get-FileHash "$fixture/Erlang.Compiler.dll").Hash -cne $compiler){throw 'Prefix fixture lost compiler identity'}
$stem='beam_long_binary_prefix_probe'
$beam="$fixture/$stem.beam"
& "$inputs/sdk/dotnet.exe" "$fixture/NewFixture.dll" "$prefix/$stem.erl" $beam *> "$fixture/compile.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$out="$fixture/native.terms"
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$prefix/native.eval" -Raw) -extra $beam $out *> "$fixture/native.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if((Get-FileHash $out).Hash -cne (Get-FileHash "$prefix/reference-native.terms").Hash){throw 'Linked prefix observations differ'}
@{Observations=60;Output=(Get-FileHash $out).Hash;Beam=(Get-FileHash $beam).Hash;Compiler=$compiler}|ConvertTo-Json|Set-Content "$fixture/receipt.json"
@{Compiler=$compiler;Runtime=$runtime;Witnesses=$w}|ConvertTo-Json -Depth 7|Set-Content "$native/receipt.json"
exit 0