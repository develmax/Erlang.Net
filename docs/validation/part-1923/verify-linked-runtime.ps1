$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/forwarded-projections-and-choice-helpers-1923'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-tail-and-wx-frames-1922'
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
foreach($image in $images){
 $beam=$image.Beam
 $name=[IO.Path]::GetFileNameWithoutExtension($beam)
 $term="$native/$name.terms"
 switch ($name){
  'ex_sizers'{
   $recipe="$inputs/wx-static-sizer-recipes-1940"
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native.eval" -Raw) -extra Release $image.Path "$recipe/stubs" $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   $observations=28
   $output=(Get-FileHash $term).Hash
   if($output -cne (Get-FileHash "$recipe/fixture-native/Release.terms").Hash){throw "Native wx observation mismatch"}
  }
  'erts_trace_cleaner'{
   $verify='[Image,Out]=init:get_plain_arguments(),{ok,Bytes}=file:read_file(Image),{ok,_}=code:prepare_loading([{erts_trace_cleaner,Image,Bytes}]),ok=file:write_file(Out,io_lib:format("~tp.~n",[{accepted,erlang:system_info(version)}])),halt().'
   & $erl +S 1:1 +A 1 -noshell -eval $verify -extra $image.Path $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   $output=(Get-FileHash $term).Hash
   $observations=0
  }
  {$_ -in 'PKCS-FRAME','PKCS-1'}{
   $recipe=if($name -eq 'PKCS-FRAME'){"$inputs/safecurves-existing-residual-1912/deferred-trivial-generator-entries"}else{"$inputs/safecurves-existing-residual-1912/pkcs-one-captured-closure-decision"}
   do{
     $parent=Get-ChildItem "$p/full-uncached" -Directory -Filter 'shard-*'|ForEach-Object { "$($_.FullName)/otp-corpus.json.generated/$beam" }|Where-Object { Test-Path -LiteralPath $_ }|Select-Object -First 1
     if(!$parent){if((Get-Date) -gt $deadline){throw "Prior image unavailable $name"};Start-Sleep -Seconds 6}
   }while(!$parent)
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native-release.eval" -Raw) -extra "$release/$beam" $parent $image.Path $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   $observations=if($name -eq 'PKCS-FRAME'){45}else{144}
   if(!(Select-String "$native/$name.stdout.log" -Pattern "$observations candidate matches" -Quiet)){throw "Native ASN.1 results differ $name"}
   $output=(Get-FileHash $term).Hash
  }
  default {throw "No release witness defined for $name"}
 }
 $w[$name]=@{Observations=$observations;Output=$output;Beam=$image.Sha256}
}
@{Compiler=$compiler;Runtime=$runtime;Witnesses=$w}|ConvertTo-Json -Depth 7|Set-Content "$native/receipt.json"

$fixture='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/constant-tail-argument-scheduling-1921/fixture'
$fixtureBase="$fixture/bin/Debug/net10.0"
$fixtureAfter=(New-Item "$native/literal-tail-fixture" -ItemType Directory).FullName
Copy-Item "$fixtureBase/*" $fixtureAfter
Copy-Item "$s/corpus-bin/Erlang.Compiler.dll" "$fixtureAfter/Erlang.Compiler.dll" -Force
Copy-Item "$s/corpus-bin/Erlang.Runtime.dll" "$fixtureAfter/Erlang.Runtime.dll" -Force
& "$inputs/sdk/dotnet.exe" "$fixtureAfter/Erlang.Tests.dll" "$fixtureAfter" > "$fixtureAfter/emit.stdout.log" 2> "$fixtureAfter/emit.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$fixture/native.eval" -Raw) -extra "$fixtureAfter/beam_constant_tail_arguments_probe.beam" "$fixtureAfter/outcome.terms" > "$fixtureAfter/native.stdout.log" 2> "$fixtureAfter/native.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$emitted=(Get-FileHash "$fixtureAfter/outcome.terms").Hash
if($emitted -cne (Get-FileHash "$fixture/native/Reference.terms").Hash){throw 'Linked tail-argument fixture observations differ'}
@{Cases=6;Compiler=$compiler;Runtime=$runtime;Outcome=$emitted}|ConvertTo-Json|Set-Content "$fixtureAfter/receipt.json"
