$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/label-numbering-and-codec-dispatch-1920'
$erl='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe'
$release='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$deadline=(Get-Date).AddMinutes(40)
while(!(Test-Path "$stage/retain-exact.exit") -or !(Test-Path "$stage/pilot-delta.tsv")){
    if((Get-Date) -gt $deadline){throw 'Compiled local corpus and exact paths incomplete'}
    Start-Sleep -Seconds 5
}
if([int](Get-Content "$stage/retain-exact.exit") -ne 0){throw 'Independent physical capture failed'}
$out=(New-Item -ItemType Directory "$stage/linked-runtime-witnesses").FullName
$exact=Get-Content "$stage/early-raw-linked/receipt.json" -Raw | ConvertFrom-Json
$binding=Get-Content "$stage/focused-identity.json" -Raw | ConvertFrom-Json
if($exact.Compiler -cne $binding.Compiler){throw 'Exact capture used a different compiler'}
$paths=@{}
foreach($line in Get-Content "$stage/pilot-delta.tsv"){
  $fields=$line -split "`t"
  $paths[[IO.Path]::GetFileNameWithoutExtension($fields[0])]=$fields
}
foreach($image in $exact.Image){
  $name=[IO.Path]::GetFileNameWithoutExtension($image.Beam)
  $paths[$name][2]=$image.Path
}
$receipt=[ordered]@{}
$cms='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/safecurves-existing-residual-1912/mixed-sequences-constant-choice-retake'
foreach($iteration in @(
  @{Name='CryptographicMessageSyntaxAlgorithms-2009';Mode='release';Cases=21},
  @{Name='OCSP-2024-08';Mode='ocsp';Cases=10}
)){
   $name=$iteration.Name
   $selected=$paths[$name]
   if(!$selected){
     $existing=Get-ChildItem "$stage/pilot.json.generated" -Recurse -Filter "$name.beam" | Select-Object -First 1
     $prior=Get-ChildItem 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/field-selectors-and-matched-producers-1919/full-uncached' -Recurse -Filter "$name.beam" | Select-Object -First 1
     $reference=Get-ChildItem "$release/lib/public_key-1.21.7/ebin" -Filter "$name.beam"
     if(!$existing -or !$prior -or !$reference){throw "Missing input image $name"}
     $selected=@($reference.FullName,$prior.FullName,$existing.FullName)
   }
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$cms/native-differences.eval" -Raw) -extra $iteration.Mode @selected "$out/$name" > "$out/$name.stdout.log" 2> "$out/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   $expected="$($iteration.Cases) results, [0-9]+ prior matches, $($iteration.Cases) candidate matches"
   if(!(Select-String -LiteralPath "$out/$name.stdout.log" -Pattern $expected -Quiet)){throw "Native ASN.1 outcome differs $name"}
   $receipt[$name]=@{Observations=$iteration.Cases;Output=(Get-FileHash "$out/$name.terms").Hash;Beam=(Get-FileHash $selected[2]).Hash}
}
$wx='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-tuple-constructor-dispatch-1935'
foreach($name in 'wxBitmap','wxImage'){
   $selected=$paths[$name]
   if(!$selected){throw "Missing changed pilot image $name"}
   & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$wx/native-release.eval" -Raw) -extra $name $selected[2] "$wx/stubs" "$out/$name.terms" > "$out/$name.stdout.log" 2> "$out/$name.stderr.log"
   if($LASTEXITCODE){exit $LASTEXITCODE}
   $hash=(Get-FileHash "$out/$name.terms").Hash
   if($hash -cne (Get-FileHash "$wx/$name.native.terms").Hash){throw "Native wx outcome differs $name"}
   $receipt[$name]=@{Output=$hash;Beam=(Get-FileHash $selected[2]).Hash}
}
$name='ssh_bits'
$selected=$paths[$name]
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/variable-width-match-ownership-1919/ssh-native.eval' -Raw) -extra $selected[2] "$out/$name.terms" > "$out/$name.stdout.log" 2> "$out/$name.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$hash=(Get-FileHash "$out/$name.terms").Hash
if($hash -cne (Get-FileHash 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/positioned-binary-constructor-types-1917/ssh-Reference-runtime.terms').Hash){throw 'SSH runtime outcomes differ'}
$receipt[$name]=@{Observations=34;Output=$hash;Beam=(Get-FileHash $selected[2]).Hash}
@{Compiler=$binding.Compiler;Witnesses=$receipt;WxRelease=27} | ConvertTo-Json -Depth 8 | Set-Content "$out/receipt.json"
