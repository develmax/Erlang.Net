# Focused supervisor acceptance

See [backend selection and commands](../../../docs/supervisor-backends.md).

Implementations are completed before validation. The observer captures complete
API values/errors/stacks and stateful effects on the admitted OTP-29.1.1 VM,
then executes the same observer BEAM against the C# and unchanged original
supervisor on this .NET runtime. It includes 19 unchanged upstream testcase
functions, direct callbacks, restart/auto-shutdown matrices, retry loops,
startup rollback, upgrades/sys, naming, argument retention, exact IDs,
shutdown ordering and a parallel shutdown barrier. Upstream testcase results
must be `ok` during oracle capture, and each testcase is explicitly cleaned up.

`Reference/` contains unchanged pinned Ericsson source files under Apache-2.0;
the source audit compares their Git blobs with the pinned reference checkout.
No production dependency on ERTS is introduced. The official VM is the
development oracle/compiler only. The tool references runtime core, stdlib and
serialization directly; it does not require unrelated managed kernel ports.
Separate build output prevents concurrent compiler writes to shared DLLs.

The optional fourth argument selects a scenario by its Erlang printed name.
This permits replaying a failed connected scenario without rerunning the corpus.
An incomplete report must not be labelled complete. A passing finite report
must not be interpreted as universal compatibility or global readiness.
