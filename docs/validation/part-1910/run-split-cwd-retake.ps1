$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/wx-record-liveness-and-ber-entry-1910'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/terminal-records-and-private-ber-shapes-1909'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$bin='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source/tests/Erlang.Tests/bin/Debug/net10.0'
$case='compiler/beam/whole-module-matches-otp'
$clock=[Diagnostics.Stopwatch]::StartNew()
while(!(Test-Path "$s/pilot-receipt.json")){
    if($clock.Elapsed.TotalSeconds -gt 1800){throw 'Scoped release lane has not finished'}
    if(Test-Path "$s/focused-fast.json"){
        $f=Get-Content "$s/focused-fast.json" -Raw|ConvertFrom-Json
        if($f.Failed){throw 'Earlier targeted test failed'}
    }
    Start-Sleep -Seconds 5
}
if(!(Test-Path "$s/early-release-control-verified/receipt.json")){throw 'Early check receipt absent'}
Set-Location -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source'
while(!(Test-Path "$p/full-uncached/timing.json")){
    if($clock.Elapsed.TotalSeconds -gt 3500){throw 'Earlier uncached lane has not finished'}
    Start-Sleep -Seconds 5
}
$identity=Get-Content "$s/focused-identity.json" -Raw|ConvertFrom-Json
$test="$bin/Erlang.Tests.dll"
if((Get-FileHash $test).Hash -cne $identity.Tests -or
   (Get-FileHash "$bin/Erlang.Compiler.dll").Hash -cne $identity.Compiler){throw 'Scoped payload changed'}
$env:TEMP=(New-Item "$s/whole-fixture-cwd-temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT=$null
& $sdk $test --case $case --report "$s/focused-whole-fixture.json" > "$s/focused-whole.stdout.log" 2> "$s/focused-whole.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$fast=Get-Content "$s/focused-fast.json" -Raw|ConvertFrom-Json
$single=Get-Content "$s/focused-whole-fixture.json" -Raw|ConvertFrom-Json
$coverage=@($fast.ExactCases)+@($single.ExactCases)
$expected=Get-Content "$s/focused-test-names.txt"
if($fast.TotalCount -ne 9930 -or $single.TotalCount -ne 9930 -or
    $fast.Passed -ne 134 -or $single.Passed -ne 1 -or ($coverage|Sort-Object -Unique).Count -ne 135 -or (Compare-Object $expected $coverage)){
    throw 'Split gate has incorrect coverage'
}
@{TotalCount=9930;SelectedCount=135;Passed=135;Failed=0;FastReportSha256=(Get-FileHash "$s/focused-fast.json").Hash;
  WholeFixtureSha256=(Get-FileHash "$s/focused-whole-fixture.json").Hash}
|ConvertTo-Json|Set-Content "$s/focused-verified.json"
& pwsh -NoProfile -File "$s/freeze-split-focus.ps1" > "$s/freeze-orchestration.stdout.log" 2> "$s/freeze-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/launch-after-split-focus.ps1" > "$s/full-orchestration.stdout.log" 2> "$s/full-orchestration.stderr.log"
exit $LASTEXITCODE

