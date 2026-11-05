# Project progress

Part593 implements the native node/dist reference query behind erts_debug and both erts_internal backends. Actual owner/header/storage census, all-scheduler admission, signal/heap classification and retirement are covered by262/262 observations plus59/59 scenarios per backend,13 physical and43 connected groups. Original Erlang/BEAM and98 exports remain. Full regression/readiness were deliberately not run; broader upstream/distributed/resource/platform acceptance and universal compatibility remain unverified. [Receipt](validation/part-593/README.md).

## Current status sources

- [ordsets verification](ORDSETS_COMPATIBILITY.md): all 21 APIs have a permanent whole-result/stack/effect corpus; the managed merge stack discrepancy is repaired. Universal compatibility acceptance remains open.

- [math backends](math-backends.md): complete 26-MFA managed surface and retained original Erlang selection; differential corpus passed, full compatibility acceptance remains open.

- [Module readiness](MODULE_READINESS.md): registered APIs, compatibility classifications and component scope.
- [Remaining work](remaining-work/INDEX.md): maintained contract queue and detailed checkpoints.
- [Detailed work changelog](../CHANGELOG.md): completed and in-progress work, validation and delivery evidence.
- [Compatibility](COMPATIBILITY.md) and [semantic differences](semantic-differences.md): contract boundaries and known limitations.

Use the source report and its exact revision when quoting results. This documentation reorganization adds no runtime, compatibility or delivery claims.

## Historical progress

The previous progress document started at Part485; the maintained work queue and changelog contain later records. Its complete content is preserved in [ordered historical volumes](history/progress/index.md), including failures, pending checks and superseded checkpoints.

## Updating progress

Keep this page as a short entry point. Put new work results in the active changelog volume and update the owning readiness or remaining-work document. Link to evidence instead of copying checkpoint narratives here.
