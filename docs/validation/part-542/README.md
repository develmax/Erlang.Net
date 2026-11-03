# Part542 validation — original BEAM functions and exact badfun stack

## Implementation and boundary

Genuine BEAM closures use shared function admission with registry/image/fingerprint/source-owner checks. One execution lease reaches the existing single432 interpreter entry. Actual invocation, pending purge/cancellation and passive function identity are distinct; passive values do not pin executable code. An admission-only BEAM call-site scope captures the missing error frame before caller unwind and is cleared before body/error-handler execution. It does not recapture arbitrary errors thrown inside the callee.

## Actual original-driven evidence

- Initial unchanged runtime: two real semantic failures (complete/reload return bare undef), abort passes. Whole raw observations and original compiler/provider receipts remain retained.
- Separate original identical A→A→purge proves a previous406 test expectation was wrong. The repaired test requires current dispatch and zero old active leases; independent no-current pending/abort/cancel controls remain intact.
- Matched original BEAM observer and spawned worker reproduce4 original frames versus3 managed frames. The corrected actual stack contains all4 frames, including both capture frames. No test inserts/removes/normalizes frames.
- Final Debug500/500 and Release500/500 are seven disjoint blocks140+140+19+38+33+102+28 with the same DLLs within each profile. This is scoped regression, not a full9,431-case local run.
- Both profiles freshly compile/execute the original observer and compare3 complete stack streams plus2 whole nonopaque outcomes. All5 raw comparisons pass in each profile; all3 semantic role cases and actual cleanup pass.
- Complete badfun outcome includes VM-specific fun creator IDs. Its bytes are preserved, not arbitrarily renamed; the original function identity is asserted within Erlang and the entire stack is compared unchanged. The other two outcomes are directly byte-equal in full.

## Permanent tests and provenance

The default catalog embeds the genuine original observer/worker BEAM images, their sources, hashes and exact original stacks. No optional environment or generated-source fallback can bypass the original BEAM path. Observation receipts precede assertions; cleanup receipts follow actual fixture/runtime disposal. Failed assertions cannot masquerade as successful cleanup.

Eleven final C# files are frozen by source-freeze.json, SHA256 A54537B03A523911167A3BE5C4C400FAD6DC578729018FDE8E60EDC490F11421. Both build windows verify the source bytes before/after. Scoped formatting has real Roslyn token-kind/text equality11/11. The fresh original comparer protects35 source/helper/fixture inputs and the full DLL/config inventory.

Raw proof archive/source metadata gates and current-head CI/review remain distinct delivery gates. Counts708 registered/180 verified/504 pending/24 known remain unchanged. The new [24-contract matrix](../../remaining-work/contracts-24/current-closure/README.md) prioritizes direct load_nif ABI/upgrade/callback behavior instead of unrelated native-prototype expansion. Whole-contract promotion requires its complete original acceptance matrix.

Final source-only gates:8/8,4,014 exact members/2,222 guards; early independent preflights13/13. Evidence archive:7,037,917 bytes/640 members, SHA256 67FAD0E908D2060F509CFBB14BA3962B84AB034C4801690BFF3426ECACC6307A; every member verified. Exact-head CI and review remain pending.

## Current PR94 review correction — owned function boundary

Review identified that the genuine owned export creates a different callable object. The strict admission marker then failed to recognize that exported callee. Three permanent genuine-BEAM owned controls reproduce2 PASS (abort/reload) and1 RED (complete purge stack) on unchanged production code. A narrow trusted-boundary alias preserves the same captured frame for the exact exported object until invocation/export/exception transfer completes; ordinary ownership export and strict reference matching remain intact. The unformatted correction passes3/3; final-profile regression and fresh original replay are being completed.

The current13 styled source files are frozen in review-owned-source-freeze.json, SHA256 FA842D7E8AEE81B2EE16ED3D1835A019DBB6B0252D1C78A8EF9903240CC2A385. Exact Roslyn token-kind/text proof covers13/13. The revised collector retains all35 original input paths and adds2, and pairs default3 plus owned3 with one fresh original3 per build profile. It compares all six complete stacks and four nonopaque outcomes without projecting metadata. The earlier11-file evidence/archive above remains the historical pre-review result, not final corrected-head acceptance.

This direct published-BEAM alias correction does not claim general restored-function factory propagation or whole P4 compatibility. Those broader source-backed chains remain separate acceptance work. Catalog expected9434; API/full deltas0/0 and708/180/504/24 remain unchanged. Corrected exact-head CI and fresh review are still required before merge.

### Final local corrected-source acceptance

Both Debug503/503 and Release503/503 are explicit same-DLL composites, not uninterrupted503-case runs. Debug preserves278 sandbox PASS plus the exact2 EACCES cases passing outside the sandbox, then223 further PASS. Release preserves279 PASS plus one isolated unchanged-DLL retry after an actual child CLR Out of memory, then223 further PASS. The original failed reports/child stderr are retained; the isolated PASS does not establish the OOM's cause. No production source or expectation changed for those retries.

Each profile freshly compiles/runs3 original OTP modes and replays default3 plus owned3 on that profile's final DLLs: six complete stack streams and four complete nonopaque outcomes all equal (10/10), with6 actual cleanup receipts. No function-ID remapping or frame projection. Final13 source freeze/token proof and both build windows remain exact. Runtime evidence supplement:1,789,763bytes/535members, SHA256 89456AD2788E3959894949C6CAA50488903704180F14955956C5E6B8250725D6; every member verified. Historical evidence.zip stays unchanged.

Local runtime acceptance is complete for this direct owned-boundary correction. Corrected-source metadata gates, current-head CI, fresh review and merge remain independent delivery gates. General restored-function factory chains/P4 and whole API promotion remain open.

Corrected source metadata now passes8/8 gates and13/13 early preflights:4,016 exact members,2,222 source guards. A new88-row current-source union replaces the stale historical541 freeze only in the current645 CI step; the historical freeze remains intact. Metadata/index hash checks execute no Erlang VM and award no API credit. Current-head CI/review/merge remain pending.
