$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference = 'Stop'
$d = 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ber-selected-result-and-shared-entry-1963-linked'
& pwsh -NoProfile -File "$d/launch-after-split-focus.ps1" > "$d/full-orchestration.stdout.log" 2> "$d/full-orchestration.stderr.log"
$rc = $LASTEXITCODE
if (!$rc){
    & pwsh -NoProfile -File "$d/wait-then-collect-full-evidence.ps1" > "$d/full-evidence-driver.stdout.log" 2> "$d/full-evidence-driver.stderr.log"
    $rc = $LASTEXITCODE
}
$rc | Set-Content "$d/durable.exit"
exit $rc
