# Part578: timer constants and scoped publication

Date: 2026-11-04. Baseline: OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5.

All36 timer exports are available through an explicitly selected C# alternative;
the original timer source and BEAM remain unchanged. This part removes embedded
protocol/source/time/closure literals and publishes only the timer implementation
and its connected runtime integration over main67809531b.

## Semantic owners

API function names, server requests, reference kinds, cancellation results,
action outcomes, protocol atoms, child-specification fields, external calls,
message/table/MFA/timestamp layouts, closure identities/environments, time units
and pinned source lines have named owners. Equal values with different contracts
remain separate. Existing BIF/operator/error/boolean/native-error catalogs are
reused in their actual domains. Source conversion lines are explicit per tc
overload rather than derived from an unexplained line offset. Ordinary API
signature arities and simple local indices remain structural declarations.

Validator options, dependency/file names, deadlines, diagnostic text and provider
contracts are owned separately. Python BEAM parser widths/tags/bit fields and
expected corpus counts are named. No original Erlang fixture or upstream test
source is rewritten. [Literal inventory](literal-audit.json) reports zero inline
string literals in the executable timer bodies.

## Current verification

The [admitted summary](audit-summary.json) binds current scoped source checksums,
production assemblies, complete corpora and all successful reports:

| Check | Result per backend |
| --- | --- |
| Primary values/reasons/full stacks |728/728 |
| Behavioral scenarios |11/11 |
| Unchanged timer_simple_SUITE testcase bodies |47/47 |
| Backtrace depths0/1/2/3/8/32 |728 primary and11 scenarios at each depth |

The clean scoped tree builds with ordinary dotnet build, zero warnings/errors and
no source-exclusion filter. Its validator composes existing public ETS primitives
and does not depend on the unfinished concurrent ETS/DETS ports. An SDK layout
check of all owned C# files passes. The source audit finds36 exports/37 imports
and confirms the original pinned source/BEAM identity. A fresh official-provider
capture checks the refactored acquisition script; original input acquisition and
complete failure history remain in [Part565](../part-565/README.md).

Only timer and connected checks ran. Full regression/global readiness, timer_perf,
randomized timer_SUITE load, other operating systems and universal scheduling,
physical resource/distribution/module-metadata certification are unrun. Default
timer registration remains0 and the global compatibility ledger is not promoted.

## Reproduction and evidence

Build tools/Validation/Erlang.TimerCompatibility with -m:1 and an isolated
--artifacts-path. Use tools/validate-timer-original.ps1 -CaptureOnly to acquire
original inputs with the admitted OTP29.1.1 provider; -IncludeUpstreamSuite adds
the unchanged upstream cases. Run the validator for Managed and Beam, then suite
and depth:N modes. The source-audit CLI accepts an optional pinned OTP checkout
path. The summary CLI accepts an optional built validator directory and refuses
incomplete reports or mismatched source/assembly/input digests.

[Raw evidence](raw-evidence.zip) preserves exact inputs, reports, logs and source
bindings. [Archive identity](archive.json) records its checksum. Reference-valued
returns and nonnegative measured durations retain the explicit Part565 projections;
no stack frame/source location is removed and no failed testcase becomes a pass.
The accepted upstream corpus has47 successful tests; an executed proof verifies
that exactly three reference-valued returns are projected and other terms remain
unchanged. The failed later original send_interval2 acquisition stays historical.

This is a clean timer-only PR tree over main; unrelated dirty work is preserved
outside the commit. PR creation is authorized; merge and package publication are
not part of this request. Exact commit and PR identities are reported from the
actual Git/GitHub results.

## CI source-binding closure

The changed runtime files initially failed10 of13 passive source-plan admissions
and the current MFA/native byte binding check. Preserve those failures in raw
local receipts. Reuse the pinned Part577 bounded updater, retargeted to this
part, and create one parented488-row current-source snapshot for monitor inputs.
Historical snapshots are unchanged. Refresh29 existing manifests/workflow files:
only declared hashes, byte lengths, producer pins and the parented snapshot
pointer change; membership, acceptance classifications and workflow logic remain
unchanged. [Semantic scope](source-plan-refresh/semantic-scope.json) and the
archived predecessors document every change.

Afterward: all13 source plans pass,4218 Resource529 dependencies pass, all5400
MFA/native raw rows pass and the22 CI-reuse controls pass. No SDK/product VM or
original runtime is executed by these passive admissions. Product comparisons
above remain the actual timer acceptance; full local regression is unrun.

The repository-wide documentation-restoration checker requires a historical
ordsets-owner-handoff/source-archive.json absent from the base Git tree. That
unrelated archival input is not fabricated or imported into this PR. Added and
changed documentation links are checked against tracked base files plus this
PR's own files; inherited missing links are kept separate.
