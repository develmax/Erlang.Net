$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/response-roots-and-sequential-comprehensions-1944'
$recovery=Get-Item "$s/freeze-parent-recovery"
$parent=Get-Content "$p/source-freeze.json" -Raw|ConvertFrom-Json
$hydration=Get-Content "$s/hydration-receipt.json" -Raw|ConvertFrom-Json
if($parent.Source -ne $hydration.Parent.Source -or (Get-FileHash "$p/source.zip").Hash -ne $parent.Source){throw 'Parent source mismatch'}
$freeze=Get-Content "$s/source-freeze.json" -Raw|ConvertFrom-Json
$id=Get-Content "$s/focused-identity.json" -Raw|ConvertFrom-Json
if($freeze.ParentSource -or $id.SourceParent -or $freeze.Source -ne (Get-FileHash "$s/source.zip").Hash){throw 'Unexpected original freeze state'}
foreach($entry in @(@('Compiler','Erlang.Compiler.dll'),@('Runtime','Erlang.Runtime.dll'),@('Tests','Erlang.Tests.dll'))){
 $actual=(Get-FileHash "$s/frozen-payload/$($entry[1])").Hash
 if($freeze.($entry[0]) -ne $actual -or $id.($entry[0]) -ne $actual){throw 'Frozen payload mismatch'}
}
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/freeze-parent-recovery/verify-source.py" $s $p 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source' > "$recovery/verified-count.log"
if($LASTEXITCODE){throw 'Frozen source tree failed verification'}
$freeze.ParentSource=$parent.Source
$id.SourceParent=$parent.Source
$freeze|ConvertTo-Json -Depth 8|Set-Content "$s/source-freeze.json"
$id|ConvertTo-Json -Depth 8|Set-Content "$s/focused-identity.json"
Copy-Item -LiteralPath "$p/assembly-source-verification.json" -Destination $s
@{Parent=$parent.Source;Source=$freeze.Source;Compiler=$freeze.Compiler;Test=$freeze.Tests;Cause='The retake read SourceParent from a test-only receipt. The recovery binds the verified hydration parent and source archive, including the separately recorded layout-normalized harness.'} | ConvertTo-Json | Set-Content "$recovery/receipt.json"
(Get-Content "$p/resume-frozen-launch.ps1" -Raw).Replace($p,$s)|Set-Content "$s/resume-frozen-launch.ps1"
$exe=(Get-Process -Id $PID).Path
$startup=New-CimInstance Win32_ProcessStartup -ClientOnly -Property @{ShowWindow=[uint16]0}
function Launch($name){
 $script="$s/$name.ps1"
 $tk=$null
 $err=$null
 [void][System.Management.Automation.Language.Parser]::ParseFile($script,[ref]$tk,[ref]$err)
 if($err.Count){throw ($err|Out-String)}
 $wp=Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{CommandLine=('"'+$exe+'" -NoProfile -File "'+$script+'"');CurrentDirectory=$s;ProcessStartupInformation=$startup}
 if($wp.ReturnValue){throw "$name launch failed $($wp.ReturnValue)"}
 $wp|Select-Object ProcessId,ReturnValue|ConvertTo-Json|Set-Content "$script.recovery-process.json"
 $wp.ProcessId
}
foreach($file in @('run-census-labels.ps1','finalize-receipts.ps1')){
 Copy-Item -LiteralPath "$s/$file" -Destination $recovery.FullName
 $original=Get-Content "$s/$file" -Raw
 if(!$original.Contains('AddMinutes(45)')){throw 'Unexpected deadline'}
 $original.Replace('AddMinutes(45)','AddMinutes(75)')|Set-Content "$s/$file"
}
if(Test-Path "$s/full-uncached/timing.json"){throw 'Raw corpus already recorded'}
foreach($entry in @(@(70964,'finalize-receipts.ps1'),@(61956,'run-finalize-receipts.ps1'),@(73240,'run-census-labels.ps1'))){
 $proc=Get-CimInstance Win32_Process -Filter "ProcessId=$($entry[0])"
 if($proc){
  if($proc.CommandLine -notlike "*$s/$($entry[1])*"){throw 'Watcher PID ownership changed'}
  Stop-Process -Id $entry[0]
 }
}
Launch 'run-census-labels'
Launch 'run-finalize-receipts'
Launch 'resume-frozen-launch'
