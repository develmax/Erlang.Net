$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-tail-and-wx-frames-1922'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-argument-lifetimes-and-codec-closures-1921-projection-retake'
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
$paths=@('lib/public_key-1.21.7/ebin/AttributeCertificateVersion1-2009.beam','lib/wx-2.7/doc/examples/demo/ex_popupMenu.beam','lib/wx-2.7/doc/examples/demo/ex_graphicsContext.beam','lib/ssh-6.0.6/ebin/ssh_bits.beam')
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
  'ex_popupMenu'{
   $recipe="$inputs/wx-popup-record-and-init-1938"
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native-release.eval" -Raw) -extra $image.Path "$recipe/popup-stubs" $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   $observations=9
  }
  'ex_graphicsContext'{
   $recipe="$inputs/wx-graphics-drawing-and-scaled-state-1939"
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native-release.eval" -Raw) -extra $name $image.Path "$recipe/graphics-stubs" $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   $observations=8
  }
  'ssh_bits'{
   $recipe="$inputs/orphan-atoms-1919"
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/ssh-bits-native.eval" -Raw) -extra $image.Path $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   $observations=34
  }
  'AttributeCertificateVersion1-2009'{
   $recipe="$inputs/safecurves-existing-residual-1912/default-prefix-local-pair-proof"
   do{
     $parent=Get-ChildItem "$p/full-uncached" -Directory -Filter 'shard-*'|ForEach-Object { "$($_.FullName)/otp-corpus.json.generated/$beam" }|Where-Object { Test-Path -LiteralPath $_ }|Select-Object -First 1
     if(!$parent){if((Get-Date) -gt $deadline){throw 'ASN.1 baseline image is unavailable'};Start-Sleep -Seconds 5}
   }while(!$parent)
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native-release.eval" -Raw) -extra "$release/$beam" $parent $image.Path $term > "$native/$name.stdout.log" 2> "$native/$name.stderr.log"
   $observations=26
  }
 }
 if($LASTEXITCODE){exit $LASTEXITCODE}
 $output=(Get-FileHash $term).Hash
 if($name -eq 'AttributeCertificateVersion1-2009'){
   if(!(Select-String "$native/$name.stdout.log" -Pattern '26 candidate matches, 17 successful round trips' -Quiet)){throw 'ASN.1 observations differ'}
 }elseif($name -eq 'ssh_bits'){
   $refOutput="$native/$name.reference.terms"
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/ssh-bits-native.eval" -Raw) -extra "$release/$beam" $refOutput > "$native/$name.ref.stdout.log" 2> "$native/$name.ref.stderr.log"
   if($LASTEXITCODE -or (Get-FileHash $refOutput).Hash -cne $output){throw 'SSH observations differ'}
 }elseif($output -cne (Get-FileHash "$recipe/$name.native.terms").Hash){throw "wx observations differ $name"}
 $w[$name]=@{Observations=$observations;Output=$output;Beam=$image.Sha256}
}
@{Compiler=$compiler;Runtime=$runtime;Witnesses=$w}|ConvertTo-Json -Depth 7|Set-Content "$native/receipt.json"
