# Historical DETS implementation receipt

Initially numbered locally as Part581 before the main changelog advanced.
Publication uses Part591 and independent isolated-branch acceptance.

Baseline: OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5.
Historical pre-publication implementation and focused acceptance. The PR branch is independently revalidated in Part591. All64 explicit dets exports are
implemented; original Erlang/BEAM remains selectable. See
[backend selection](../../../dets-backends.md) and
[scope and reproduction commands](../README.md).

| Gate | Actual result | Saved report |
| --- | --- | --- |
| C# primary,21 scenarios plus full errors | Pass after final option-clause fix | [final-managed-primary.json](final-managed-primary.json) |
| C# unchanged upstream cases | All7 pass after improper-list fix | [final-managed-suite.json](final-managed-suite.json) |
| Original primary and upstream | Both pass,21 scenarios and7 cases | [initial focused report](final-dets.json) |
| Magic resources and connected interpreter/GC/ETS |8/8 pass | [final-connected.json](final-connected.json) |
| Production v9 native file interchange |6/6 pass after final source change | [receipt](interchange-final2-receipt.json) |
| Tests and interchange builds |0 warnings/errors | Commands in scope record |
| SDK source layout |0 files requiring changes | Owned sources checked in isolated layout root |

The initial five-case report is deliberately retained with its failed managed
suite row. That row is superseded by the separate successful managed-suite report;
it is not edited into a synthetic all-green harness report. Original tests and
connected runtime checks precede the last change to the managed open_file option
clause. That change affects no original-BEAM/runtime code. Both managed primary
and managed upstream cases were rerun on the final C# assembly. All six native
interchange cases were rebuilt/rerun after that change as well.

[Observations](observations/) contain full returned ETF, error ETF, source-owner
checks and implementation identity. The retained failed observation contains the
unmodified full upstream failure stack. [Input hashes](implementation-inputs.json),
[current source inventory](current-source-inventory.json) and
[host assembly hashes](assemblies.json) bind this scope separately from historical
draft evidence. Native capture provenance is copied alongside the reports.

The first rebuild attempt during the prior original test was refused by the
Windows assembly file lock. It changed no loaded test assembly. After that process
finished, a clean incremental build completed with0 warnings/errors and both
managed acceptance groups passed. Earlier failures remain in artifacts and the
initial report; no expected result, suite body or assertion was weakened.

No full regression, validate.ps1 or global readiness regeneration ran. No
cross-OS, complete upstream-suite, exhaustive scheduling/resource-failure or
whole release-manifest startup certification is claimed. Original source assets
and the sibling OTP checkout are unchanged. No external ERTS is used by production
DETS execution; the official VM is the development oracle.

Publication is separate: the shared checkout contains substantial uncommitted
changes belonging to other library work, including shared selection files and
interleaved changelog volumes. This receipt does not authorize bundling them into
a DETS commit. No DETS commit/push has been made from that mixed checkout.
