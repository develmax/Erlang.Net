$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "0"
$ErrorActionPreference="Stop"
$d=$PSScriptRoot
& pwsh -NoProfile -File "$d/launch-after-split-focus.ps1" > "$d/full-orchestration.stdout.log" 2> "$d/full-orchestration.stderr.log"
$result=$LASTEXITCODE
if($result -eq 0){
    & pwsh -NoProfile -File "$d/wait-then-collect-full-evidence.ps1" > "$d/full-evidence-driver.stdout.log" 2> "$d/full-evidence-driver.stderr.log"
    $result=$LASTEXITCODE
}
$result|Set-Content -LiteralPath "$d/durable.exit"
exit $result
