$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mapped-records-and-prefixed-field-lifetimes-1946'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$bin='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source/tests/Erlang.Tests/bin/Debug/net10.0'
$case='compiler/beam/whole-module-matches-otp'
$catalog=Get-Content "$s/scope.json" -Raw | ConvertFrom-Json
$clock=[Diagnostics.Stopwatch]::StartNew()
while(!(Test-Path "$s/pilot-receipt.json")){
    if($clock.Elapsed.TotalSeconds -gt 1800){throw 'Scoped release lane has not finished'}
    if(Test-Path "$s/focused-fast.json"){
        $f=Get-Content "$s/focused-fast.json" -Raw|ConvertFrom-Json
        if($f.Failed){throw 'Earlier targeted test failed'}
    }
    Start-Sleep -Seconds 5
}
$summary=Get-Content "$s/pilot.json" -Raw|ConvertFrom-Json
$ordsets=@($summary.Modules|Where-Object Beam -eq 'lib\stdlib-8.1\ebin\ordsets.beam')
if($ordsets.Count -ne 1 -or $ordsets[0].Status -ne 'Equal'){throw 'The linked build did not retain raw ordsets'}
$counters=@($summary.Modules|Where-Object Beam -eq 'lib\erts-17.1\ebin\counters.beam')
if($counters.Count -ne 1 -or $counters[0].Status -ne 'Equal'){throw 'The linked build did not retain raw counters'}
$inets=@($summary.Modules|Where-Object Beam -eq 'lib\inets-9.8\ebin\inets_sup.beam')
if($inets.Count -ne 1 -or $inets[0].Status -ne 'Equal'){throw 'The linked build did not retain the exact inets supervisor'}
$head=@($summary.Modules|Where-Object Beam -eq 'lib\inets-9.8\ebin\mod_head.beam')
if($head.Count -ne 1 -or $head[0].Status -ne 'Equal'){throw 'Linked mod_head regressed from isolated file parity'}
$instance=@($summary.Modules|Where-Object Beam -eq 'lib\inets-9.8\ebin\httpd_instance_sup.beam')
if($instance.Count -ne 1 -or $instance[0].Status -ne 'Equal'){throw 'Linked httpd_instance_sup regressed from isolated file parity'}
$required=@(
 'lib\ssh-6.0.6\ebin\ssh_tcpip_forward_client.beam',
 'lib\ssh-6.0.6\ebin\ssh_tcpip_forward_srv.beam',
 'lib\inets-9.8\ebin\mod_trace.beam',
 'lib\public_key-1.21.7\ebin\PKIX1Implicit-2009.beam'
)
foreach($path in $required){
 $entry=@($summary.Modules|Where-Object Beam -eq $path)
 if($entry.Count -ne 1 -or $entry[0].Status -ne 'Equal'){throw "Linked target is not exact: $path"}
}
$required=@(
 'lib\ssh-6.0.6\ebin\ssh_tcpip_forward_client.beam',
 'lib\ssh-6.0.6\ebin\ssh_tcpip_forward_srv.beam',
 'lib\inets-9.8\ebin\mod_trace.beam',
 'lib\public_key-1.21.7\ebin\PKIX1Implicit-2009.beam',
 'lib\kernel-11.0.4\ebin\local_udp.beam',
 'lib\xmerl-2.2\ebin\xmerl_html.beam',
 'lib\inets-9.8\ebin\mod_actions.beam',
 'lib\public_key-1.21.7\ebin\PKIXAttributeCertificate-2009.beam'
)
foreach($path in $required){
 $entry=@($summary.Modules|Where-Object Beam -eq $path)
 if($entry.Count -ne 1 -or $entry[0].Status -ne 'Equal'){throw "Linked target is not exact: $path"}
}
& pwsh -NoProfile -File "$s/verify-scoped-disassembly.ps1" > "$s/scoped-disasm-driver.stdout.log" 2> "$s/scoped-disasm-driver.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/run-early-controls.ps1" > "$s/early.stdout.log" 2> "$s/early.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
while(!(Test-Path "$p/full-uncached/timing.json")){
    if($clock.Elapsed.TotalSeconds -gt 3500){throw 'Earlier uncached lane has not finished'}
    Start-Sleep -Seconds 5
}
$identity=Get-Content "$s/focused-identity.json" -Raw|ConvertFrom-Json
$test="$bin/Erlang.Tests.dll"
if((Get-FileHash $test).Hash -cne $identity.Tests -or
   (Get-FileHash "$bin/Erlang.Compiler.dll").Hash -cne $identity.Compiler){throw 'Scoped payload changed'}
$env:TEMP=(New-Item "$s/whole-fixture-temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$env:DOTNET_PROCESSOR_COUNT=$null
& $sdk $test --case $case --report "$s/focused-whole-fixture.json" > "$s/focused-whole.stdout.log" 2> "$s/focused-whole.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
$fast=Get-Content "$s/focused-fast.json" -Raw|ConvertFrom-Json
$single=Get-Content "$s/focused-whole-fixture.json" -Raw|ConvertFrom-Json
$coverage=@($fast.ExactCases)+@($single.ExactCases)
$expected=Get-Content "$s/focused-test-names.txt"
if($fast.TotalCount -ne $catalog.TotalCatalog -or $single.TotalCount -ne $catalog.TotalCatalog -or
    $fast.Passed -ne $catalog.FastCases -or $single.Passed -ne 1 -or ($coverage|Sort-Object -Unique).Count -ne $catalog.Cases -or (Compare-Object $expected $coverage)){
    throw 'Split gate has incorrect coverage'
}
@{TotalCount=$catalog.TotalCatalog;SelectedCount=$catalog.Cases;Passed=$catalog.Cases;Failed=0;FastReportSha256=(Get-FileHash "$s/focused-fast.json").Hash;
  WholeFixtureSha256=(Get-FileHash "$s/focused-whole-fixture.json").Hash}
|ConvertTo-Json|Set-Content "$s/focused-verified.json"
& pwsh -NoProfile -File "$s/freeze-split-focus.ps1" > "$s/freeze-orchestration.stdout.log" 2> "$s/freeze-orchestration.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/launch-after-split-focus.ps1" > "$s/full-orchestration.stdout.log" 2> "$s/full-orchestration.stderr.log"
exit $LASTEXITCODE









