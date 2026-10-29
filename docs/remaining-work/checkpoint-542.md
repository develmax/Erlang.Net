# Part542 — BEAM local-function admission and exact badfun stack

## Implemented

Genuine prepared BEAM closures now use the shared local-function admission protocol. Registry, code fingerprint, selected image and published source owner are authenticated. The admitted execution lease passes once through the existing single432 interpreter entry; pre-entry failure and cancellation release it. Passive function references do not acquire execution leases.

An admission-only badfun failure records the actual BEAM call site in addition to its saved continuation. The scoped site applies only to the matching non-tail call, is cleared before the called body/undefined-function handler, and is restored at caller unwind. Errors thrown by the function body are not recaptured as admission failures. Full stacks are never edited in the observer/comparer.

## Original-driven correction

Before production edits, fresh original OTP29.1.1 workers establish completed purge -> badfun with the original function in the reason; aborted purge -> callable; identical reload -> callable. The unchanged managed runtime fails complete/reload with bare undef while abort passes. The five-file admission candidate corrects all3.

An old406 test encoded wrong identical-code behavior. A separate original A→A→purge execution returns {{value,1},false,true,{value,1},1}. Pinned fun-entry staging confirms identical code shares its dispatch entry. The test now requires current-image calls to succeed and old-image active count0. The no-current-image pending/abort/cancel test remains unchanged. Corrected affected block28/28 passes.

The first comparison with an original erl_eval parent was intentionally not called full-stack equivalence. A matched original BEAM worker/spawn context then reproduces four original frames versus three managed frames. The admission-frame correction makes all3 scenario stacks equal byte-for-byte, including the duplicated capture frame. No frame projection, opaque-function-ID renumbering or raw ETF rewrite is used.

## Permanent validation

The ordinary test catalog now embeds the exact original observer/worker BEAM images and immutable raw stacks. There is no environment-variable or generated-source fallback. Every case checks source-owner identity, role equality inside Erlang and full stack bytes; abort/reload also compare complete nonopaque outcomes. Complete-mode whole output contains VM-specific fun creator IDs, so its bytes remain preserved without a false cross-VM identity assertion. Cleanup is recorded after actual actor/VM disposal, separately from the observation.

Fresh final Debug original+managed comparison passes all3 semantic scenarios,5 complete raw comparisons and all cleanup. Final11 C# sources SHA256 A54537B03A523911167A3BE5C4C400FAD6DC578729018FDE8E60EDC490F11421. Formatting has exact Roslyn token/literal evidence. Final Debug500/500 and Release500/500 explicit block composites pass. Fresh original/managed replay in EACH profile passes all3 semantic modes, all3 complete raw stack streams and both whole nonopaque outcomes, with actor/VM cleanup. Metadata gates, publication and CI remain pending.

## The 24-contract priority

No whole-contract credit follows this increment. The exact24-row audit now lives in [current-closure](contracts-24/current-closure/README.md). The next direct API batch is historical entry/function ABI and options/error compatibility for erlang:load_nif/2, followed by genuine upgrade, monitor/select and remaining callback/platform closure. A source-only prepared patch is not executed implementation. Native634 literal/opcode expansion is no longer the first priority for reducing24.

Part541 remains separately frozen and published as PR93; this candidate does not rewrite its evidence.
