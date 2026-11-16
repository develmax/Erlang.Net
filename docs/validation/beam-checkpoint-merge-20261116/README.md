# BEAM checkpoint integration

The integration brings the published BEAM compiler, pinned OTP image fixtures, compiler regression checks and corpus runner into the current main tree. It retains the current OTP backend implementations and the current partitioned Ordsets registrations where the parents overlap. The merged test project includes the linked sources and embedded-resource entries from both parents.

The Release test project and corpus runner built without warnings or errors. Eleven focused tests passed on the initial integration commit. Five focused tests passed after updating three assertions for supported compiler syntax. Six regex checks passed after loading the pinned Windows OTP admission in the isolated checkout.

The current source preflight passed all thirteen workflow plans. The MFA and native inventories verified 5,400 raw source and evidence rows. Resource529 verified its current source plan. These reports record input identity and do not replace execution evidence or the full readiness report.

The original 1,366-image comparison remains in [part 1820](../part-1820/README.md). The integration also starts a fresh comparison from its compiled corpus runner.
