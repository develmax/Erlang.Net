$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/list-values-and-register-projections-1924'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/forwarded-projections-and-choice-helpers-1923'
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
  'ex_radioBox' {
   $recipe="$inputs/wx-retained-constructor-list-1942"
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/observe.eval" -Raw) -extra Release $image.Path "$recipe/stubs" $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   $output=(Get-FileHash $term).Hash
   if($output -cne (Get-FileHash "$recipe/fixture-native/Release.terms").Hash){throw 'Native wx observations differ'}
   $count='[Result]=init:get_plain_arguments(),{ok,[Rows]}=file:consult(Result),io:format("~w",[length(Rows)]),halt().'
   $observations=[int](& $erl +S 1:1 +A 1 -noshell -eval $count -extra $term)
   if($LASTEXITCODE -or $observations -le 0){throw 'Native wx witness cannot be counted'}
  }
  'OCSP-2024-08' {
   $recipe="$inputs/safecurves-existing-residual-1912/guarded-closure-definition-runs"
   do{
     $parent=Get-ChildItem "$p/full-uncached" -Directory -Filter 'shard-*'|ForEach-Object { "$($_.FullName)/otp-corpus.json.generated/$beam" }|Where-Object { Test-Path -LiteralPath $_ }|Select-Object -First 1
     if(!$parent){if((Get-Date) -gt $deadline){throw 'OCSP parent image is unavailable'};Start-Sleep -Seconds 6}
   }while(!$parent)
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native-release.eval" -Raw) -extra "$release/$beam" $parent $image.Path $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   if(!(Select-String "$native/$name.stdout.log" -Pattern '74 candidate matches, 58 round trips, 15 rejected' -Quiet)){throw 'The 74 OCSP cases changed'}
   $observations=74
   $output=(Get-FileHash $term).Hash
  }
  default {throw "No release witness defined for $name"}
 }
 $w[$name]=@{Observations=$observations;Output=$output;Beam=$image.Sha256}
}
@{Compiler=$compiler;Runtime=$runtime;Witnesses=$w}|ConvertTo-Json -Depth 7|Set-Content "$native/receipt.json"

$fixture='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/recursive-list-identity-and-capture-1923/literal-and-captured-tail-frames'
$sample=(New-Item "$native/lifted-list-fixture" -ItemType Directory).FullName
$fixtureBase="$inputs/terminal-record-result-1909/final-fixture-bin"
Copy-Item "$fixtureBase/*" $sample
Copy-Item "$s/corpus-bin/*.dll" $sample -Force
$stem='beam_recursive_list_values_probe'
& "$inputs/sdk/dotnet.exe" "$sample/NewFixture.dll" "$fixture/$stem.erl" "$sample/$stem.beam" > "$sample/emit.stdout.log" 2> "$sample/emit.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$fixture/native.eval" -Raw) -extra "$sample/$stem.beam" "$sample/values.terms" "$sample/stacks.terms" > "$sample/native.stdout.log" 2> "$sample/native.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$values=(Get-FileHash "$sample/values.terms").Hash
$stacks=(Get-FileHash "$sample/stacks.terms").Hash
if($values -cne (Get-FileHash "$fixture/reference-values.terms").Hash -or $stacks -cne (Get-FileHash "$fixture/reference-stacks.terms").Hash){throw 'Linked lifted list fixture differs'}
@{Cases=14;Compiler=$compiler;Runtime=$runtime;Values=$values;Stacks=$stacks}|ConvertTo-Json|Set-Content "$sample/receipt.json"
