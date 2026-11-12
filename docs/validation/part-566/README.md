# Part566 — gen_event C# implementation and focused acceptance

Baseline: **OTP 29.1.1**, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`; Windows x64, .NET SDK 10.0.400.

All **44 non-metadata signatures** have a C# implementation beside unchanged
original Erlang/BEAM. The C# manager owns its protocol loop, handler transitions,
supervision, state/system callbacks and formatting logic. Original `gen`,
`proc_lib`, `sys`, formatting and Unicode dependencies remain separate libraries.
There is no call-through from the C# backend to original `gen_event`.

| Final check | C# | Original BEAM on .NET |
| --- | ---: | ---: |
| Complete focused observations | **105/105** | **105/105** |
| Non-metadata export inventory | 44/44 | 44/44 |
| Included unchanged upstream testcase functions | 10 | 10 |
| Backend-owner checks and bounded runtime disposal | pass | pass |

Ten upstream functions are a subset of `gen_event_SUITE`: add_handler,
delete_handler, swap_handler, swap_sup_handler, notify, sync_notify, call, info,
get_state and replace_state. Other cases cover callback errors/throws/exits,
optional and missing callbacks, supervised exits, duplicate handler IDs, startup,
hibernation, request collections, logger formatting and invalid API arguments.
All returned observations, including exception stacks, are compared exactly.
The status testcase explicitly checks the parent PID relationship; it does not
compare unrelated runtime process numbers. No exception frame is stripped.

## Final depth checkpoint

The final reports under [final/](final/audit-summary.json) supersede the initial
105-case implementation binding retained at this directory root. Both current
backends again pass105/105. Six additional complete profiles at backtrace depths
0,1,2,3,8,32 pass630/630 observations per backend; see the
[profile receipts](depth-profiles/summary.json). A real prefix-truncation defect
in generated source errors was fixed before these profiles.

The first parallel C# depth32 run has a retained upstream_call failure: the
upstream test assumes a zero-timeout request has not already received its reply.
Here it received a valid already-queued reply; the serial repeat passes. The
failure is not rewritten or dropped. This remains a scheduling-equivalence
limitation, not evidence of universal100% compatibility. Final scoped builds also
exclude concurrent Timer changes needing newer runtime name definitions, in
addition to DETS; the exact final filter and registration snapshot are retained.

The immutable initial reports below remain historical evidence for their own
source binding. Use final/ for the current primary source/assembly acceptance.

## Evidence and reproduction

- [Managed report](Managed-report.json), [BEAM report](Beam-report.json),
  [admitted summary](audit-summary.json), [provider/source provenance](provenance.json).
- [Frozen corpus](original.etf), compiled identical observers and upstream fixture
  BEAM files, [implementation source hashes](source-bindings.json).
- Final build logs: [stdlib](build-stdlib.log), [runner](build-runner.log),
  [runtime send repair](build-runtime-send2.log); zero warnings/errors.
- [Raw attempts](raw-attempts.zip) preserve early build failures, dependency
  admission/formatting/bootstrap failures, source-frame differences, the local
  fun corpus decode failure, file-copy contention and superseded reports.
  They are not additional acceptance cases.
- [Partial-report rejection](rejected-partial.log) verifies the summary refuses an
  otherwise copied report with `CompleteCorpus=false`.

Acquisition command:

```powershell
pwsh -File tools/validate-gen-event-original.ps1 -CaptureOnly -Output artifacts/gen-event566/oracle
```

Each final backend runs the same current
`Erlang.GenEventCompatibility.dll repository corpus-directory Managed|Beam`.
Then run `tools/Validation/summarize-gen-event-audit.ps1` with `-Directory` and
`-RunnerDirectory`. It checks source, dependency, observer, corpus and assembly
hashes and requires both complete successful reports. The retained gen_event
image matches the admitted provider SHA256
`732B365DA64EB2B6D418652E70C4506516BB9DA223E1EDE90795864AE69EA11E`.

## Fixes and limits

The comparisons drove source-faithful callback/request/error continuations,
the exact no-callback-module atom, generated behavior exports and malformed
system-state guards. A shared BEAM send-opcode defect omitted the native
`erlang:send/2` error frame; its repair exports rooted arguments at the exception
boundary and preserves the owned send path. Both final backends pass the same
invalid-send observation and normal manager message traffic.

Real original `init` and `code_server` execute on .NET for feature/formatting
dependencies; missing capabilities are not replaced by successful stubs. Native
storage/file bootstrap and the existing logger host are recorded dependencies.

The checkout is concurrently modified by other module tasks. Final focused
stdlib builds use the retained [temporary build filter](focused-build.targets)
and a task-owned registration snapshot to exclude unfinished DETS; dependency
assembly inputs are frozen and hashed. This does not establish an ordinary
whole-checkout build or a clean publication-head receipt. Earlier unrelated
ETS/code/gen_statem/file errors and stale validation-process contention remain
in the raw archive.

Full regression and canonical readiness regeneration were deliberately not run,
as requested. The previous immutable readiness comparison remains unchanged.
Finite same-platform acceptance is not universal distribution, physical
scheduling/resource, module-metadata or platform compatibility certification.
No performance, commit/push, hosted CI, merge or package-publication claim is made.
See the [backend selection contract](../../gen-event-backends.md).
