# Part560: string stack parity and explicit integration

Baseline: **OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5**.
Both unchanged original Erlang/BEAM and the manual C# implementation are retained.

| Main differential observation | C# | Original BEAM on .NET |
| --- | ---: | ---: |
| Declared/exercised public exports | 71/71 | 71/71 |
| Complete cases | 8521 | 8521 |
| Returned values matched | 6363 | 6363 |
| Exception class/reason matched | 2158 | 2158 |
| Complete stacks matched | 2158 | 2158 |
| Literal stack equality | 2156 | 2156 |
| Stacks with live compiled-pattern bindings | 2 | 2 |
| Backing observations | 13/13 | 13/13 |
| Differences | 0 | 0 |

Each of depths0,1,2,3,8,32 independently passes17/17 complete error outcomes in
both backends:102/102 per backend. Raw references are retained. Only actual
compiled-pattern resources proven by live binary:match queries receive a
one-to-one binding; four negative controls reject changed stack/proof forms.

The corpus includes2112 unique calls captured while all44 functional testcase
functions of unchanged pinned string_SUITE pass in the official oracle. It
also covers every export, malformed parameter types and UTF-8, mixed/improper
chardata, non-byte-aligned bitstrings, large lexeme indexes and number tails.
Performance and doctest infrastructure are not claimed as executed.

## Evidence

- [managed report](managed-report.json), [original report](beam-report.json),
  [depth profiles](depth-profiles.json), [provider provenance](provenance.json).
- [Complete raw acquisition/execution archive](acceptance5.tar.gz): expected and
  actual ETF, observer BEAM, suite capture, all profile reports/logs and provider
  receipt. No failed earlier run is rewritten into this archive.
- [Five integration groups](integration-accepted.json): both string backends,
  preserved public selector ABI/dependency refusals and both12247-row ordsets
  corpora, with unchanged expected bytes and unchanged outer test budgets.
- [Eight selector controls](selector-contracts.json): restored mandatory native
  capture refusal plus both string backends, original collections and math.
- [Final five controls](final-controls.json): final selector guard and complete
  OS command corpus budget, plus both string backends and ABI/dependencies.

The final native-capture admission correction changes startup validation only;
it preserves existing native primitives for managed-capable libraries instead
of accepting a BEAM image that replaces them with ordinary code. Genuine images
and the original negative control are both tested. The8521-case archive binds
the exact replay assemblies; the final admission correction has the separate
fresh selector receipt above and does not replace those assembly identities.

## Global gates

Pre-fix focused controls are [15/16](completion-controls.json): both string
backends, dependency/ABI refusals and all twelve shared term-contract shards
pass. The remaining failure is `runtime/review498-imported-flat-map-copy`,
reproduced independently in [remaining controls](remaining-controls.json).
The attempted broad logical-map equality correction caused ten term-contract
failures ([rejected experiment](map-final-controls.json)) and was completely
reverted. The twelve shards pass after that revert.

The narrower correction handles ordinary logical flat maps whose keys are all
atoms: their internal atom indexes can differ between runtimes, while names
identify the same keys. Compare these canonical key/value pairs; preserve the
physical equality path for other key shapes and native maps. All
[16/16 fresh controls](atom-import-controls.json) pass, including imported copy,
all twelve term shards and both string selections/dependency refusals. The
full regression on this isolated assembly terminated without a final report.
Its [raw log](full-tests-atom-import.log) contains1328 passing rows, then four
OutOfMemory failures from process-thread accounting, followed by a fatal Roslyn
`Stack overflow`. The [observed run state](full-tests-atom-import-state.json)
is explicitly incomplete, not a harness pass report. Windows subsequently
reported about2GiB free physical memory out of64GiB; that snapshot does not
establish the cause of either exception. See the retained
[host observation](host-memory-after-full-crash-elevated.json).

The completed outside-sandbox full run is [9642/9647](full-tests-outside.json).
Four failing controls were subsequently repaired or passed fresh isolated
checks, but this is not a new full passing report. A redundant final full run
was deliberately interrupted after the remaining map defect was reproduced;
its [log](full-tests-final-interrupted.log) and exact owned-process
[interruption receipt](full-tests-final-interruption.json) are retained.
Canonical readiness generation and publication remain open: a fresh whole
passing report and reconciliation of shared source-binding changes are required.
No passing global report or whole-module Verified status is fabricated.
The sixteen current source-binding mismatches are recorded with an explicit
[review and ownership boundary](source-binding-review.md).

The final SDK formatting command exits0 with no files requiring changes.
[Source digests](completion-source-files.json) and
[replay assembly digests](completion-assemblies.json) bind this checkpoint.
Original OTP source/image paths have no working-tree diff.
Both final MSBuild examples build without warnings/errors and return the same
result: [Managed](example-final-managed.json), [Beam](example-final-beam.json).

String corpus acceptance and global project acceptance are separate. A full
sandbox run produced9495/9647 and is retained in artifacts/string-port: most
failures are Windows process/pipe EACCES. The same port control passes outside
sandbox. Earlier full-run attempts also retain real timeout failures and the
native-capture regression that motivated the final corrections. The final
full regression/readiness/publication receipts must be assessed on their own
results; no failed or interrupted run is relabelled successful.

Default registry counts remain unchanged:1 shared native string primitive.
Each explicit library backend has71 public exports. No canonical MFA status is
promoted to universally verified from this finite corpus. See
[current backend contract](../../../string-backends.md) for the exact recipe
and [the prior failed checkpoint](../../string-port-20261103/README.md) for
preserved historical stack differences.
