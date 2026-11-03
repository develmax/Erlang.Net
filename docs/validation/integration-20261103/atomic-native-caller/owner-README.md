# Validated atomic native-caller handoff

Apply atomic-native-caller.patch or copy source/ paths after verifying manifest.
Four production inputs and six permanent test files; existing6-argument dispatcher
ABI retained. Register the two tests from source/HARNESS-REGISTRATION.txt; original
BeamOrdsets381/lists.beam is already embedded by the test project and hash checked.
No150s watchdog, global deadline, protocol, source/BEAM rewrite or ledger promotion.

source/SOURCE-AUDIT.md covers source writer, gate, ownership, tracing, lifetime and
unsupported claims. The standalone build reads the exact w-docs project/source
base, all outputs here, and only4reviewed compile input overrides.
owner-final-build.log has0warnings/errors; owner-final-run.log runs the actual
proposed permanent tests:7controls plus actual reverse interleaving/control.
Candidate6DLL hashes are in manifest.json. Do not mix shared-root Runtime binaries:
its Managed factory ABI drifted independently and a mixed runner failed acquisition.

Baseline race-before2.log records replacement_reverse with stale native-only
umerge2_2:3471; control-before.log has the same replacement with no such helper.
Candidate retains selected native badarg and its correct frame. All mutations use
real DeleteModule/Register interleavings; no sleeps/race loops. The test observer
preserves/restores the AsyncLocal slot and does not change VM capability data.

No live source/index/commit/push/PR edit by this audit. Revalidation found18/19 prior
frozen files unchanged and external drift in tests/Erlang.Tests/Erlang.Tests.csproj;
that file was not edited by this audit. Exact before/after hashes are retained.
Full baseline9666/9666 belongs to coordinator; new9668full+CI/review is required.
This closes a narrow gate only and does not certify all21MFA contracts.
