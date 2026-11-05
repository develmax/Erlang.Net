# PR112 main conflict and source-identity repair

Merge main c6ce68bfc into PR112, preserving code/file/gen_event/gen_statem startup
selection and registration together. Keep main enum values and append GenStatem.
Keep the main BEAM send-error source capture and the PR badfun frame correction.
No gen_statem engine behavior changes in this repair.

The branches independently allocated Part580. Main's file evidence stays at its
original path. All nineteen earlier gen_statem publication files are preserved
byte-for-byte under [the dedicated gen_statem namespace](../gen-statem-port-20261104/README.md).
Its original changelog narrative is preserved there separately. Old source and
assembly receipts remain historical evidence, not results for this merge head.

Start source plans from main's current policy/membership, refresh declared
digests, Bytes/SealedBytes and producer/workflow pins, and give the monitor freeze
a successor with preserved parent path/hash. The structural scope proof permits
only those changes; no status, membership, execution flag, workflow logic or
trigger is changed. Original OTP29.1.1 source/BEAM assets are unchanged. No new
package or license decision; reuse the existing main source-admission helper.

Ordinary Release builds of tests and runner pass with zero warnings/errors and
no exclusions. Four exact function-call/continuation tests pass. The original
corpus `system` scenario passes separately with Managed and Beam; those subset
reports explicitly remain incomplete. Full regression, full311-scenario/74-API
replay, readiness regeneration and manual CI dispatch are unrun in this urgent
conflict/hash repair. Prior whole-module results keep their original scope.

Commands: `dotnet build tests/Erlang.Tests -c Release --nologo -m:1 -nr:false -v q`;
the same build for tools/Validation/Erlang.GenStatemCompatibility; exact four
`--case` names in focused.json; run the compatibility DLL with repository,
original admitted corpus, backend and `system`; Resource529 verify-source-plan.py;
unchanged13-plan passive admission extracted from validate.yml;
verify-mfa-source-bindings.py; unittest discover under Erlang.CiReuse.

The initial CI-reuse control invocation failed because its new monitor receipt
was not yet in HEAD. Rerun against the committed candidate is the accepted result;
the original failure remains in ci-controls.log. Hosted checks/review still apply.
The separate review finding about retained async sys continuations remains open;
this repair does not claim it is fixed or resolve its thread. No merge or package
publication is performed. Push this repair immediately to existing PR112.
