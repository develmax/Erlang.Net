# Part579 — gen_statem alternatives

The complete forty-export C# library surface and state-machine engine are
implemented beside unchanged original Erlang/BEAM. Implementation preceded
verification. Both alternatives run inside our .NET runtime; original OTP is
the development oracle. This is a completed focused checkpoint, **not a
certification of 100% OTP compatibility**.

## Final connected acceptance

| Check | Result |
| --- | --- |
| Ordinary validator dependency build | 0 warnings/errors; no exclusion filters |
| C# backend | 74/74 complete API observations; 311/311 engine scenarios |
| Original BEAM backend | 74/74 complete API observations; 311/311 engine scenarios |
| Retained source | Exact pinned source bytes unchanged; official BEAM hash matches |
| Export inventory | All40 non-module_info exports, including behaviour_info/1 |
| Source layout | SDK-backed owned-source check: 0 files need formatting |
| Changed corpus control | Rejected before execution |
| Partial-report control | Named case succeeds but remains incomplete; summary rejects it as full acceptance |

Canonical fresh receipts are in [final/acceptance.json](final/acceptance.json),
[Managed](final/Managed-report.json), [BEAM](final/Beam-report.json),
[source audit](final/source-audit.json), [provider/corpus provenance](final/provenance.json)
and [build log](final/build.log). Earlier passing checkpoint receipts at this
directory's root remain unchanged. [files.json](files.json) binds preserved
inputs/source snapshots; [history.zip](history.zip) retains preceding failures,
controls, builds and reports. The tag identity is independently retained in
[reference-tag.txt](reference-tag.txt).

The311 scenarios include256 mode/event combinations, both callback modes,
state-enter/repeat restrictions, module push/pop/change across different
callback modes, bad actions/modes/results, callback error/exit/throw, postponed
and inserted events, named/event/state/zero/real/absolute timeouts, cancellation
and update, requests/collections, monitors/named startup, enter_loop, sys
get/replace/status/suspend/resume/code-change and stop/reply. Two controls assert
exactly one terminate callback after sys detours, including a resumed error.
Error observations compare complete stack-containing terms. No frame is
removed to obtain equality.

The managed startup receipt has40 exports,0 native gen_statem primitives and
the actual C# assembly identity. No gen_statem BEAM is in its image list. The
BEAM execution owns the original image for every declared export. Other OTP
helpers remain their real original implementations. Native timer/receive/
alias/registration operations remain shared primitives.

## Commands and environment

Use `tools/validate-gen-statem-original.ps1` for fresh immutable acquisition,
`dotnet build tools/Validation/Erlang.GenStatemCompatibility -m:1` for the
ordinary build, and the runner with repository/oracle-directory/Managed|Beam.
Run `audit-gen-statem-source.py` and `summarize-gen-statem.py` to admit the two
complete source/assembly-bound reports. Exact local accepted output is under
`artifacts/gen-statem/runner-delivery`; source-bound inputs are under
`artifacts/gen-statem/oracle-last`. See the
[runner instructions](../../../tools/Validation/Erlang.GenStatemCompatibility/README.md).

The provider is official OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5.
Capture rechecks admitted kernel/stdlib/ERTS sources/images and VM files before
and after running. Replay binds both observer sources/images, corpora, export
and authentic feature-bootstrap files. A real original init/loader/code_server
boot uses the existing beam_code_server_subject410 fixture and actual native
ETS/filesystem capabilities. No production MFA is stubbed. Logger primary level
is `none`; report formatting is compared separately.

The engine uses opaque source-wrapper custody for C# state kept across awaits;
no optimized borrowed-heap or performance assertion is made. The original
source notices are preserved. Semantic names have separate owners for protocol,
function, dependency, source/status and formatting contracts.

## Preserved failures and remaining scope

History contains the observer get/1 shadowing error, missing ETS/provider and
formatting/feature/code-server bootstrap closure, unrelated concurrent
compilation failures, missing/incorrect source frames, early malformed-input
CLR failures and serialization of a span-bearing diagnostic object. They are
not passing acceptance. Focused diagnostic/exclusion builds preceded ordinary
passing builds and are not used as the final build claim.

Full upstream gen_statem_SUITE, native owned-heap/resource/platform stress,
distribution/global/via integration, actual structured Logger handler effects
and module metadata parity remain acceptance work. Finite Windows corpus
agreement does not prove universal OTP compatibility. **Full regression and
global readiness regeneration were deliberately not run**, as requested; no
whole-module compatibility-ledger status is promoted from these counts.

Changes remain local in a shared dirty checkout. No isolated publication-head
commit, push, PR, merge, CI or package publication is claimed. Preserve unrelated
work. The [backend guide](../../gen-statem-backends.md) describes selection and
required dependency composition.
