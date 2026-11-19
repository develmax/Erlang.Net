$ErrorActionPreference = 'Stop'
(Get-Process -Id $PID).PriorityClass = 'BelowNormal'
$stage = 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/late-list-and-tail-copy-provenance-1870'
$eval = 'C:/Users/Maksim/.codex/worktrees/beam-release-parity/Erlang.Net/tools/Validation/Erlang.BeamCorpus/rank-opcode-differences.eval'
$identity = (Get-FileHash -LiteralPath $eval).Hash
if ($identity -ne '45D2567BADF01109BB469FCD9985485AF71D741FBF91B0A98454DA7B1AED82EB') {
    throw 'The reference-cache evaluator changed'
}
$env:ERL_CRASH_DUMP = "$stage/rank-crash.dump"
$timer = [Diagnostics.Stopwatch]::StartNew()
& 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe' +S '1:1' +A 1 -noshell -eval (Get-Content -LiteralPath $eval -Raw) -extra 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed' "$stage/rank-input" "$stage/function-rank.terms" 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/opcode-rank-cache' $identity > "$stage/rank.stdout.log" 2> "$stage/rank.stderr.log"
$result = $LASTEXITCODE
$timer.Stop()
@{ExitCode = $result; Seconds = $timer.Elapsed.TotalSeconds; EvalSha256 = $identity} |
    ConvertTo-Json | Set-Content -LiteralPath "$stage/rank-receipt.json"
exit $result




