# Part565: timer managed/original alternatives

Date: 2026-11-04. Baseline: OTP-29.1.1,
`ad05823719d77c8faee87348ea39513d4e2f99c5`.

## Implementation

All 36 timer exports are implemented in C# and the original Erlang source and
BEAM remain unchanged. [Source inventory](source-inventory.json) records every
export, all 37 imports and source clauses. See the [backend guide](../../timer-backends.md).

The user explicitly required implementation before checking and only connected
checks. No regression tests were run before completing the initial library and
selection implementation. Later fixes follow actual focused observations.

The port retains native timer/BIF services and explicitly used original
gen_server/supervisor/ETS dependencies. It implements timer references, supervised
startup, cancellation, source arithmetic/guards, callbacks and both interval
worker protocols in C#. Transfer recipes rebind the entire captured environment.
Termination uses actual selected ETS foldl, avoiding a snapshot traversal.

The connected runtime fixes preserve the two frames of a failed fun application
and the caller/source/error_info metadata of convert_time_unit. They are shared
runtime behavior exercised by both selected timer implementations.

## Confirmed observations

Both implementations pass 728/728 primary observations and all 11 behavioral
scenarios. Export equality and actual source ownership are checked against the
original oracle. Comparisons include full exception reasons and stacks, without
removing acquisition frames or source locations. The oracle and managed workers
use identical observer entry points.

Fresh references are checked and projected to a reference marker; timers are
cancelled. Invalid conversion-unit exceptions validate a nonnegative duration
before projecting that measured argument. No deterministic result or stack
metadata is normalized. The scenario suite validates delivery, timer tags,
double cancellation, server callbacks, supervised startup, overlap, completion
waiting, owner death, registered names, tc overloads and exit/kill.

Both also pass all47 non-benchmark cases of the unchanged timer_simple_SUITE.
Backtrace depths0/1/2/3/8/32 each pass728 primary observations and11 scenarios
per backend. Final assembly hashes match across all those runs. The complete
[admitted summary](audit-summary.json) checks current owned source bindings,
actual assembly hashes, corpus completeness and every successful report.
An altered727-row partial report is rejected without producing a summary.

The final malformed-state witness preserves the handle_info case-clause reason
and complete stack when ets:take returns an unexpected tuple. The earlier silent
noreply result is corrected. Both original and managed observers use the identical
scenario worker, preserving the complete caller stack.

[Raw evidence](raw-evidence.zip) includes failures, successful reports, original
corpora, observer BEAMs, source snapshots and the exact isolated build inputs.
[Archive identity](archive.json) records its SHA256. The first47-row upstream
reference input and its three reference projections are both retained, with an
executed projection proof. Final upstream replays use the same production
assemblies admitted by the primary and depth comparisons.

## Evidence history

Retain the initial ordinary build failure in unrelated concurrently edited
modules, then isolated build/output receipts; source staging must occur outside
a retained registry writer. Keep the first oracle's extra acquisition frames,
all 40 initial managed discrepancies, the corrected guard/source-frame failures,
the original BEAM fun-call failures and every failed attempt.

The first genuine upstream acquisition completed all 47 non-benchmark tests.
Its .NET comparison had exactly three fresh-reference differences, with all
testcase bodies succeeding. A later original acquisition itself timed out in
send_interval2; the validator refuses that failed expected row. It is retained
as a failed reference run, not used as an expected successful result.

The accepted upstream corpus is derived from the first successful raw corpus
with timer_suite_capture:project/2. That function refuses any failed testcase
and projects only reference-valued results. Exactly three rows change; the
original raw corpus is retained. The unchanged upstream testcase source and
actual compiled observer are replayed against each implementation.

## Commands and boundaries

Use the permanent tools under tools/Validation/Erlang.TimerCompatibility:

```powershell
py tools/Validation/audit-timer-source.py artifacts/timer/source-inventory.json
dotnet build tools/Validation/Erlang.TimerCompatibility -m:1 --artifacts-path artifacts/timer/build
pwsh -File tools/validate-timer-original.ps1 -Output artifacts/timer/original -CaptureOnly -IncludeUpstreamSuite
dotnet <validator.dll> . artifacts/timer/original Managed
dotnet <validator.dll> . artifacts/timer/original Beam
dotnet <validator.dll> . artifacts/timer/original Managed suite
dotnet <validator.dll> . artifacts/timer/original Beam suite
```

This shared working tree has unrelated concurrent incomplete module ports. The
recorded build uses a task-owned DirectoryBuildTargetsPath that excludes those
unfinished additions and a registration snapshot excluding their stages, while
retaining the timer stage. This is a focused current-source build, not a clean
whole-repository build. Inputs and the exact filter are preserved in raw evidence.

No full regression, global readiness regeneration, load benchmark, randomized
timer_SUITE, distributed-node audit, other-OS timing/resource certification,
publication-head CI/review, commit/push or package publication is claimed.
Default timer APIs remain absent; selected timer has 36 library exports. The
canonical readiness report is preserved as historical and not refreshed from
partial results. Finite evidence does not establish universal 100% equivalence.
