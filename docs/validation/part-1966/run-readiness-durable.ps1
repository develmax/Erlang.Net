$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference='Stop'
$t='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-entry-frames-and-call-argument-liveness-1966-linked-retake'
$w='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/send-frame-and-core-include-provenance-1875'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$tool="$w/readiness-tool/Erlang.Tool.dll"
$report="$w/full-managed/repository-full.json"
$scope="$t/readiness-verified-inputs/docs/module-readiness-scope.json"
$docs=Split-Path $scope
$out="$t/readiness-regenerated"
$timer=[Diagnostics.Stopwatch]::StartNew()
& 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe' $tool readiness $docs $report $out > "$t/readiness.stdout.log" 2> "$t/readiness.stderr.log"
$result=$LASTEXITCODE
$timer.Stop()
@{ExitCode=$result;Seconds=$timer.Elapsed.TotalSeconds;ToolSHA=(Get-FileHash $tool).Hash;PassingReportSHA=(Get-FileHash $report).Hash;ScopeSHA=(Get-FileHash $scope).Hash;PreviousScope=(Get-Content $scope -Raw | ConvertFrom-Json).ComparisonTestReport} | ConvertTo-Json | Set-Content "$t/readiness-receipt.json"
exit $result
