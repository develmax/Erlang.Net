# Correct integrated workflow pins and ambiguous CI reuse

Base main f0c23d603f304acb4f60aa8502e23ddf0fbe0b56, whose tree e5280e0a5d38be0e60d35fac237cd5cf61abde45 equals PR102 head6c966ada688cf35f9202febf8029872015825671.

## Actual delivery sequence

Root converted PR102 to draft at21:11:50 UTC and published6c. A later ready-for-review attempt returned FORBIDDEN; a fresh read found PR102 already merged at21:12:41 UTC. Root did not invoke the merge API in this continuation and did not treat local controls or pending CI as sufficient acceptance. The GitHub event identifies login develmax, which does not establish whether a person or another chat initiated it. Current final-head full CI was pending; no full acceptance was claimed.

Only after observing the merge and identical complete trees, Root cancelled redundant PR Validate41719677725 before its full body. It remains cancelled, not passed. Push-main Validate41719715112 remains the one active complete gate; its resource504 and relocated-tooling jobs passed. Independent Math/NIF/GC jobs remained active. Canonical oldad791 full9682 proof stays historical.

## Failures and corrections

- Integrated41720021847: Root supplied expected6c while refmain checked out f0. The exact-head guard correctly stopped execution. Retain original [ZIP](integrated-41720021847.zip) and [complete raw log](integrated-41720021847.log.gz). Corrected invocation used actual main f0, without weakening the guard.
- Integrated41721053688: correct head then exposed stale workflow ENV709_NATIVE_PLAN. All three environment plan bytes and PlanPins were already current; only their workflow environment literals remained old. Preserve [ZIP](integrated-41721053688.zip), [complete log](integrated-41721053688.log.gz), [original workflow](workflow-before.yml.gz) and [three exact corrections](workflow-pin-correction.json). These are preparation refusals with skipped contract stages, not successful NIF execution.
- Math41719677787: Windows/Ubuntu passed; macOS never acquired a hosted runner, no steps executed. Official annotations report hosted macOS arm64 capacity constraints. One targeted retry of only the cancelled macOS job was requested after this concrete cause was established; no runner or matrix change. Earlier cancellation remains evidence.

## Bounded implementation and controls

Correct three workflow literals. Permanently check all11 PlanPins,178 raw source inputs and6 driver sources before full work through the existing CI helper tests. Stale or missing values for each of the three environment pins are independently rejected.

Reject repeated attempts in every required current/equivalent-tree/tested-head run, including nonselected runs. After downloading and verifying the ZIP, reread producer workflow lists and concrete attempts, then current merge workflow lists; changed snapshots require full validation. Controlled tests introduce late incomplete/failed siblings and a producer rerun after ZIP download. This is bounded consistent reading of GitHub APIs, not an atomic transaction or a guarantee against subsequent external changes.

[19 passing local controls](controls.json), [complete output](controls.txt) retain a positive mocked lifecycle alongside all negative controls. No live hosted reuse admission, new complete regression or module compatibility claim is made. C#/Erlang/native production sources, public deadlines, test scope, original OTP pin and branch protections remain unchanged. Fresh targeted integrated execution on the published fix commit, appropriate hosted acceptance and current review remain pending. No competing local or remote full set is started by this patch preparation.
