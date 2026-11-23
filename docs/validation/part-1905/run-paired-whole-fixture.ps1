$ErrorActionPreference='Stop'
$s='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/two-atom-dispatch-and-owned-result-trims-1905'
$p='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/tuple-alias-and-call-owned-frames-1904'
$sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
$bin='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source/tests/Erlang.Tests/bin/Debug/net10.0'
$testCase='compiler/beam/whole-module-matches-otp'
$deadline=(Get-Date).AddMinutes(18)
while(!(Test-Path "$s/pilot-receipt.json") -or !(Test-Path "$p/full-uncached/timing.json")){
    if((Get-Date) -gt $deadline){throw 'Measured release lanes did not finish'}
    Start-Sleep -Seconds 5
}
$old=Get-Content "$s/focused.json" -Raw | ConvertFrom-Json
$failed=@($old.Tests | Where-Object Status -eq Failed)
if($old.Passed -ne 109 -or $failed.Count -ne 1 -or $failed[0].Name -ne $testCase -or
    $failed[0].Error -notmatch '^System.TimeoutException'){throw 'Unexpected original failure'}
$bindings=Get-Content "$s/focused-identity.json" -Raw | ConvertFrom-Json
foreach($x in @(@{Name='parent';Folder="$p/frozen-payload";Identity=(Get-Content "$p/focused-identity.json" -Raw|ConvertFrom-Json)},
                @{Name='candidate';Folder=$bin;Identity=$bindings})){
    $name=$x.Name
    $compiler="$($x.Folder)/Erlang.Compiler.dll"
    $test="$($x.Folder)/Erlang.Tests.dll"
    if((Get-FileHash -LiteralPath $compiler).Hash -cne $x.Identity.Compiler -or
       (Get-FileHash -LiteralPath $test).Hash -cne $x.Identity.Tests){throw "Unexpected $name payload"}
    $temporary=(New-Item -ItemType Directory "$s/focused-paired-$name-temp").FullName
    $env:TEMP=$temporary
    $env:TMP=$temporary
    $env:DOTNET_PROCESSOR_COUNT=$null
    & $sdk $test --case $testCase --report "$s/focused-paired-$name.json" > "$s/focused-paired-$name.stdout.log" 2> "$s/focused-paired-$name.stderr.log"
    $result=$LASTEXITCODE
    @{ExitCode=$result;Compiler=$x.Identity.Compiler;Tests=$x.Identity.Tests;Test=$testCase} |
        ConvertTo-Json | Set-Content "$s/focused-paired-$name.receipt.json"
}
$parent=Get-Content "$s/focused-paired-parent.json" -Raw | ConvertFrom-Json
$candidate=Get-Content "$s/focused-paired-candidate.json" -Raw | ConvertFrom-Json
if($parent.Passed -ne 1 -or $candidate.Passed -ne 1 -or $candidate.TotalCount -ne 9905 -or $parent.Tests[0].Name -ne $testCase -or
    $candidate.Tests[0].Name -ne $testCase){throw 'Paired whole-module fixture needs investigation'}
$verified=[ordered]@{TotalCount=$old.TotalCount;SelectedCount=$old.SelectedCount;Passed=$old.Passed+$candidate.Passed;Failed=0;
    InitialReportSha256=(Get-FileHash "$s/focused.json").Hash;
    PairedCandidateSha256=(Get-FileHash "$s/focused-paired-candidate.json").Hash;
    PairedParentSha256=(Get-FileHash "$s/focused-paired-parent.json").Hash}
$verified|ConvertTo-Json|Set-Content "$s/focused-verified.json"
& pwsh -NoProfile -File "$s/freeze-after-paired-focus.ps1" > "$s/freeze-paired.stdout.log" 2> "$s/freeze-paired.stderr.log"
if($LASTEXITCODE){exit $LASTEXITCODE}
& pwsh -NoProfile -File "$s/launch-after-paired-focus.ps1" > "$s/full-paired.stdout.log" 2> "$s/full-paired.stderr.log"
exit $LASTEXITCODE
