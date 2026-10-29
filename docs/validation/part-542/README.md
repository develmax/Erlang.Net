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
