# Project progress

Part574 consolidates the final Linux descriptor poll source and audits stale branches. Exact-head Linux/original replay, CI, review and merge remain pending; no API/full-contract promotion. See [branch decisions and acceptance gates](validation/part-574/README.md).

Draft Part568 prepares the bulk physical-occurrence runtime patch in a separate PR to main. Ordsets stays6 Verified/15 KnownDifferences; current whole ETF replay remains RED. See [evidence and open acceptance gates](validation/part-568/ordsets-bulk/README.md).

## Current status sources

- [String integration](validation/string-otp-merge-20261104/README.md): both71-API alternatives retain explicit selection;9706 full tests and separate8522/depth comparisons per backend pass. Source identities and limitations remain explicit; final delivery is tracked in [PR105](https://github.com/develmax/Erlang.Net/pull/105).

- [Accepted main PR103](https://github.com/develmax/Erlang.Net/pull/103): native async accounting, independent logical/physical port closure, pending RPC shutdown and final microstate cancellation. See the [retained report](validation/part-557/REPORT.md) and [exact incoming progress snapshot](history/main-103-progress/index.md). These bounded results do not close the whole statistics contract.

- [Integration PR #102](https://github.com/develmax/Erlang.Net/pull/102): documentation/history split and local libraries are combined; final-head delivery gates are tracked in the PR and [integration evidence](validation/integration-20261103/README.md).

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

PR102 is merged as f0c23d603. Its complete unit report passed9687/9687, while overall main Validate failed before async observer execution on a stale caller pin. The [repair evidence](validation/part-569/ci-integrated-pins-and-reuse/hosted-results/README.md) records corrected native cohort and async23 focused successes. Final repair-head whole CI/current review remain pending; generated readiness is unit evidence, not a whole-pipeline acceptance claim.
