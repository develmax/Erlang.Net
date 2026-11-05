## Part580 — 2026-11-04 — Publish gen_statem alternatives with semantic constants

**Part status:** implementation and focused final validation passed; PR publication
is the delivery step. Full OTP compatibility acceptance remains open.
**Objective and scope:** publish the user-requested gen_statem C# port while
retaining original Erlang, remove magic strings/numbers and open a scoped PR.
The prior local port was absent from main; this PR includes that module and its
minimal startup integration. Unrelated local drafts are excluded.

### Changes

Implement forty non-module_info exports, both callback modes and state entry,
callback result/action ordering, postponed/inserted events, callback-module
stack changes, event/state/named timeouts, request collections, startup, sys
callbacks, hibernation, termination and log formatting. Preserve original source
and BEAM unchanged and require explicit Managed/Beam selection; no fallback or
live migration. Retain opaque source wrappers for long-lived C# state.

Give protocol atoms, functions, modules, status keys/labels, log formats, pinned
source lines/private arities, sys records and receive limits semantic owners.
Replace generated format names such as Value/P2 with role names. Replace source
line subtraction with explicit per-MFA delegation coordinates. Separate equal
strings that have different function/protocol/metadata meanings and remove
obsolete aliases. Give validator paths, provenance/reflection/boot contracts,
deadlines and corpus dimensions their own owners. Keep source-language observer
fixtures unchanged, including their deliberate invalid terms and source lines.

Use existing public ETS bootstrap services in the focused runner, avoiding a
dependency on the unrelated ETS draft. A clean-main replay exposed a lost
badfun frame in BEAM invocation: retain the failed application and its suspended
caller and update the connected synthetic tail-rejection expectation.

### Decisions and compatibility

Pinned baseline remains OTP-29.1.1 at
ad05823719d77c8faee87348ea39513d4e2f99c5. Reuse existing runtime services,
original OTP helpers and .NET BCL; no package or license selection. Preserve
Apache-2.0 notices. See [decision0007](../decisions/0007-gen-statem-alternatives.md)
and [dependency decisions](../dependency-decisions.md).

Finite comparisons do not certify universal OTP compatibility. Full upstream
gen_statem_SUITE, cross-platform/resources/owned heaps, distributed/global/via,
actual structured Logger effects and module metadata parity remain open.
The focused fixture disables primary Logger output; format values/errors are
compared separately. No generated global readiness/API ledger is promoted.

### Validation

Exact commands, fresh original-VM inputs, source/assembly hashes, complete reports,
connected test selection and original failures are retained in the
[Part580 receipt](../validation/part-580/README.md). Full regression/readiness and
manual CI dispatch were deliberately not run under the user's focused scope.

After reconciliation with main91c05943d, both alternatives pass74/74 complete API
observations and311/311 engine scenarios. Ordinary Release runner/test builds
have0 warnings/errors; exactly4 connected call/continuation tests pass. Source
and forty-export admission, package SDK layout and693 Markdown targets pass.
Existing Resource5294218 dependencies, all13 passive source plans, current MFA/
native byte bindings and22 CI-reuse controls pass. The identity-only refresh
preserves plan membership, policy/status fields and workflow logic/triggers.

### Handoff

Open this branch as a PR to main after final focused acceptance. Required CI and
review remain PR gates; do not merge or publish packages in this task. Updated
PROGRESS, NEXT_STEPS, COMPATIBILITY, semantic differences and DECISIONS link the
module's evidence and unresolved acceptance scope. Local checkpoint commit:
`feat(gen_statem): publish selectable port with semantic constants`.
