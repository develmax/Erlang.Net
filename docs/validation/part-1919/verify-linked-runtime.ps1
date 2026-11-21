$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/field-selectors-and-matched-producers-1919'
$erl='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe'
$rel='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$deadline=(Get-Date).AddMinutes(35)
while(!(Test-Path "$stage/retain-exact.exit") -or !(Test-Path "$stage/pilot-receipt.json")){
    if((Get-Date) -gt $deadline){throw 'Compiled pilot and exact outputs are incomplete'}
    Start-Sleep -Seconds 5
}
if([int](Get-Content "$stage/retain-exact.exit") -ne 0){throw 'Exact capture failed'}
$out=(New-Item -ItemType Directory "$stage/linked-runtime-witnesses").FullName
$exact=Get-Content "$stage/early-raw-linked/receipt.json" -Raw | ConvertFrom-Json
$binding=Get-Content "$stage/focused-identity.json" -Raw | ConvertFrom-Json
if($exact.Compiler -cne $binding.Compiler){throw 'Exact capture used a different compiler'}
$receipt=[ordered]@{}
$pk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/safecurves-existing-residual-1912/repeated-required-sequence-prefix'
$path='lib/public_key-1.21.7/ebin/PKIXAlgs-2009.beam'
$new=($exact.Image | Where-Object Beam -eq $path).Path
$priorEntry=Get-ChildItem 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-case-layout-and-shared-receivers-1918/full-uncached/shard-*/otp-corpus.json.generated/lib/public_key-1.21.7/ebin/PKIXAlgs-2009.beam' -File | Select-Object -First 1
if(!$priorEntry){throw 'Prior frozen PKIXAlgs image is missing'}
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$pk/native-differences-retake.eval" -Raw) -extra release "$rel/$path" $priorEntry.FullName $new "$out/PKIXAlgs-2009" > "$out/PKIXAlgs-2009.stdout.log" 2> "$out/PKIXAlgs-2009.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
if(!(Select-String -LiteralPath "$out/PKIXAlgs-2009.stdout.log" -Pattern '^18 results, [0-9]+ prior matches, 18 candidate matches\.$' -Quiet)){throw 'PKIX native outcomes differ'}
$receipt['PKIXAlgs-2009']=@{Observations=18;Output=(Get-FileHash "$out/PKIXAlgs-2009.terms").Hash;Beam=(Get-FileHash $new).Hash;PriorBeam=(Get-FileHash $priorEntry.FullName).Hash}
foreach($group in @(
   @{Root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-shared-object-forwarding-1931';Modules=@('wxMenu','wxListCtrl')},
   @{Root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-image-list-construction-1933';Modules=@('wxImageList','wxBitmap','wxSizerItem')}
)){
    $recipe=$group.Root
    foreach($module in $group.Modules){
        $path="lib/wx-2.7/ebin/$module.beam"
        $record=$exact.Image | Where-Object Beam -eq $path
        $image=if($record){$record.Path}else{"$stage/pilot.json.generated/$path"}
        $dump="$out/$module.terms"
        & $erl +S 1:1 +A 1 -noshell -eval (Get-Content "$recipe/native-release.eval" -Raw) -extra $module $image "$recipe/stubs" $dump > "$out/$module.stdout.log" 2> "$out/$module.stderr.log"
        if($LASTEXITCODE){exit $LASTEXITCODE}
        $digest=(Get-FileHash $dump).Hash
        if($digest -cne (Get-FileHash "$recipe/$module.native.terms").Hash){throw "Native execution differs: $module"}
        $receipt[$module]=@{Output=$digest;Beam=(Get-FileHash $image).Hash;BeamPath=$path}
    }
}
$ssh=Get-ChildItem "$stage/pilot.json.generated" -Recurse -Filter 'ssh_bits.beam' | Select-Object -First 1
if(!$ssh){throw 'Generated SSH bits image is missing'}
$dump="$out/ssh_bits.terms"
& $erl +S 1:1 +A 1 -noshell -eval (Get-Content 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/positioned-binary-constructor-types-1917/ssh-native.eval' -Raw) -extra $ssh.FullName $dump > "$out/ssh_bits.stdout.log" 2> "$out/ssh_bits.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$digest=(Get-FileHash $dump).Hash
if($digest -cne (Get-FileHash 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/positioned-binary-constructor-types-1917/ssh-Reference-runtime.terms').Hash){throw 'SSH release initially reports changed outcomes'}
$receipt['ssh_bits']=@{Observations=34;Output=$digest;Beam=(Get-FileHash $ssh.FullName).Hash}
@{Compiler=$binding.Compiler;Witnesses=$receipt;WxSharedRelease=17;WxImageRelease=25} | ConvertTo-Json -Depth 8 | Set-Content "$out/receipt.json"
