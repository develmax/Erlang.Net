$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-argument-lifetimes-and-codec-closures-1921-projection-retake'
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
if($exact.Compiler -cne $binding.Compiler -or $exact.Runtime -cne $binding.Runtime){throw 'Exact capture used a different compiler or runtime'}
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
$smime='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/safecurves-existing-residual-1912/plain-standalone-comprehension-order'
$name='SecureMimeMessageV3dot1-2009'
$images=$paths[$name]
if(!$images){throw 'S/MIME physical witness missing'}
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$smime/native-differences.eval" -Raw) -extra release @images "$out/$name" > "$out/$name.stdout.log" 2> "$out/$name.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if(!(Select-String -LiteralPath "$out/$name.stdout.log" -Pattern '^26 results, [0-9]+ prior matches, 26 candidate matches\.$' -Quiet)){throw 'S/MIME native outcomes differ'}
$receipt[$name]=@{Observations=26;Output=(Get-FileHash "$out/$name.terms").Hash;Beam=(Get-FileHash $images[2]).Hash}
foreach($wx in @(
 @{Name='wxSizerItem';Root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-factored-sizer-overloads-1936';Cases=24},
 @{Name='wxFont';Root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-font-constructor-overloads-1937';Cases=20}
)){
 $name=$wx.Name
 $root=$wx.Root
 $selected=$paths[$name]
 if(!$selected){throw "Missing changed pilot image $name"}
 $dump="$out/$name.terms"
 & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$root/native-release.eval" -Raw) -extra $name $selected[2] "$root/stubs" $dump > "$out/$name.stdout.log" 2> "$out/$name.stderr.log"
 if($LASTEXITCODE){exit $LASTEXITCODE}
 $hash=(Get-FileHash $dump).Hash
 if($hash -cne (Get-FileHash "$root/$name.native.terms").Hash){throw "Native wx outcome differs $name"}
 $receipt[$name]=@{Observations=$wx.Cases;Output=$hash;Beam=(Get-FileHash $selected[2]).Hash}
}
$name='ex_static'
$selected=$paths[$name]
if(!$selected){throw 'Physical example initializer missing'}
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/typed-rounded-operands-and-captures-1916/actual-wx-init/ex-static-init.eval' -Raw) -extra $selected[2] 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/typed-rounded-operands-and-captures-1916/actual-wx-init/stubs' "$out/$name.terms" > "$out/$name.stdout.log" 2> "$out/$name.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$hash=(Get-FileHash "$out/$name.terms").Hash
if($hash -cne (Get-FileHash 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/typed-rounded-operands-and-captures-1916/actual-wx-init/Reference.terms').Hash){throw 'Actual initializer outcomes differ'}
$receipt[$name]=@{Observations=5;Output=$hash;Beam=(Get-FileHash $selected[2]).Hash}
foreach($ssh in @(
 @{Name='ssh_bits';Recipe='ssh-bits-native.eval';Cases=34},
 @{Name='ssh_sftpd';Recipe='ssh-sftpd-native.eval';Cases=7}
)){
 $name=$ssh.Name
 $selected=$paths[$name]
 if(!$selected){throw "Missing changed SSH image $name"}
 $root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/orphan-atoms-1919'
 $results=@(for($index=0;$index -lt 3;$index++){
    $path="$out/$name-$index.terms"
    & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$root/$($ssh.Recipe)" -Raw) -extra $selected[$index] $path > "$out/$name-$index.stdout.log" 2> "$out/$name-$index.stderr.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    (Get-FileHash $path).Hash
 })
 if(($results|Sort-Object -Unique).Count -ne 1){throw "SSH native outcome differs $name"}
 $receipt[$name]=@{Observations=$ssh.Cases;Output=$results[0];Beam=(Get-FileHash $selected[2]).Hash}
}
@{Compiler=$binding.Compiler;Runtime=$binding.Runtime;Witnesses=$receipt} | ConvertTo-Json -Depth 8 | Set-Content "$out/receipt.json"