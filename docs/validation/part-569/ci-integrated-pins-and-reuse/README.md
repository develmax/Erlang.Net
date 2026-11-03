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


## Final equivalent-ancestor snapshot correction

The independent 1590 review identified an additional late ancestor-history window. Preserve [the complete review](reviews/ci-reuse-fix569-review1590/review.md) and earlier 1586/source snapshots. The final collector also rereads every equivalent-ancestor history after ZIP download; any changed snapshot falls back. [Final controls](controls-final.json) and [raw output](controls-final.txt) cover late ancestor failure and concrete producer retries despite a lagging list, with distinct producer100/consumer900 identities. All19 pass. The independent Math5/5 report applies to earlier1a85, not this later correction. Earlier controls/reports remain unchanged. Final current-head review and live hosted admission remain pending.


## Real execution exposed nested source/producer bindings

Run41723631184 passed the complete N1 component (42 original cases/84 comparisons,4 module-text/8 comparisons, GC818 both profiles and all4 mutex routes), but the full cohort remained RED: ENV input admission failed on .gitattributes and N2 original rejected its N1 producer plan. [Complete raw artifact, log and RED/GREEN controls](deep-plan-correction/summary.json) retain the genuine failure.

The generic refresher had incorrectly chosen repository-root .gitattributes for three Root=Packet rows. Correct typed root resolution restores the actual packet attribute bytes and therefore restores the three original ENV workflow literals; aligning workflow literals alone was insufficient. N2 original/managed WrongEntryNativeBuildPlans also needed current N1 Windows/Linux producer hashes. Refresh downstream plan references/pins to the actual corrected plan bytes, retaining all predecessors in source-plan-refresh. The permanent passive check now verifies nested qualified inputs and native/original/wrong-entry producer links. A deliberate repository/packet filename collision is a permanent negative. All20 local controls pass after genuine pre-fix RED. No runtime/native source, hash mode, deadline, case count or compatibility status changes. Fresh full-cohort hosted acceptance remains pending; the earlier bounded collector review does not certify this newer metadata correction.


## Main failure was a literal PowerShell consumer pin

Completed main41719715112 log confirms9687/9687 actual unit rows and1321/1321 configured catalog/native9/9/cleanup pass, followed by Source-plan byte pin differs at collect.py:38 before the async observer ran. This is separate from the older main41706938411 aggregate173 error; its old cause remains unproven. [Caller correction and complete literal audit](powershell-consumer-correction/summary.json) preserve the old A372 default, actual711A plan,93 matching source rows, genuine RED/GREEN and official log scope. Only the default literal changes; caller is outside its own plan, so no self-hash cycle.

Scan7508 active executable source files/390 full64hex literals against238 known historical source-plan digests: exactly one stale-plan consumer candidate before correction, zero after. Unrelated immutable ZIP/golden digests are not rewritten or claimed validated by this mapping audit. Permanent early checks now cover the actual PS default and deliberately stale input. Review1593 also identified role fields previously checked only when present; known N2/ENV original/managed roles now require their native/original/wrong-entry links, with8 deletion negatives. All22 controls pass. Fresh actual async23 and whole-package hosted acceptance remain pending.
