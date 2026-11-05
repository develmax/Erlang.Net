# ETS main reconciliation — Part586 / PR116

The initial ETS head96260610b39f6009348ebfe8ea82bc803704b923 is reconciled with
mainb317a9c2f3cd12d81bbc01249d89411e33266981. Preserve the published file/gen_event choices, append the
ETS stage after them, and refresh only existing source identities/size/pins.
All36 conflicts are resolved. Existing admission membership, approval/execution
flags, workflow logic and compatibility statuses are preserved; the monitor
freeze is a successor of main's committed manifest.

Ordinary no-exclusion build:0 warnings/errors. Each backend passes355 complete
observations,8 stateful scenarios,12 terminal/regex scenarios and75 real
filesystem/DETS/corruption scenarios:450 per backend. Both load the official dump;
official OTP29.1.1 validates both exported dumps. No oracle expectation changes.

[summary.json](summary.json) and the six separate reports bind current assemblies,
fixtures, complete terms/stacks and controlled effects.
[source-bindings.json](source-bindings.json) binds84 source documents to tested PDBs,
with0 current differences. [literals.json](literals.json) and
[layout-check.log](layout-check.log) pass. Only worker table identities and the
documented separate i/0 header/table-presence observation are projected.

[source-preflight.json](source-preflight.json) passes all13 passive plans;
[resource529.json](resource529.json) passes4320 dependencies;
[mfa-native-bindings.json](mfa-native-bindings.json) preserves raw admissions.
[Semantic scope](source-plan-refresh/semantic-scope.json) verifies existing-field
identity/size/pin updates only. Unchanged absent historical evidence is read from
the frozen main Git objects. These checks do not execute unrelated native suites.

[raw-evidence.zip](raw-evidence.zip) preserves raw observations, dumps, helper
BEAMs, every bound source snapshot and tested binaries/PDBs. Its digest is in
[archive-sha256.txt](archive-sha256.txt). The [initial PR116 receipt](../ets-pr-20261104/README.md)
and [Part569 receipt](../ets-port-20261104/README.md) retain earlier evidence and
failures unchanged. For reproduction, use the initial receipt's commands with
fresh artifact names and the admitted official OTP provider.

No full local regression, canonical readiness refresh or manual CI dispatch ran.
Full upstream/platform/resource/concurrency/metadata/startup-contract acceptance
remains open;100% OTP compatibility and final-head hosted CI are not certified.
See [backend selection](../../ets-backends.md).
