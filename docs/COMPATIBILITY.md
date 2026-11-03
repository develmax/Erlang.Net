# Compatibility overview

Draft Part568 prepares the bulk physical-occurrence runtime patch in a separate PR to main. Ordsets stays6 Verified/15 KnownDifferences; current whole ETF replay remains RED. See [evidence and open acceptance gates](validation/part-568/ordsets-bulk/README.md).

## Current state and sources

The integration also retains accepted main [PR103 native async/cancellation work](validation/part-557/REPORT.md). Its bounded controls and remaining public statistics contracts keep their original scope; [all incoming compatibility records](history/main-103-compatibility/index.md) remain intact. A new combined-source CI report is required before treating the earlier PR102 full report as current.

Current [ordsets verification](ORDSETS_COMPATIBILITY.md) covers all 21 APIs with exact outcomes/stacks/effects and repairs native reverse source frames. Universal acceptance remains open.

Compatibility is baseline-bound and tracked per command. A registered API, a prepared original image, a finite comparison and full contract acceptance are separate claims. Read each source with its revision and recorded limitations.

- [API compatibility index](API_COMPATIBILITY.md): command classifications, complete scopes, ordered evidence and hashes.
- [Module readiness](MODULE_READINESS.md) and [machine-readable readiness](module-readiness.json): generated registration/component snapshot.
- [Compatibility ledger](mfa-compatibility.json): authoritative command records. Rendering documentation does not refresh runtime or source-binding acceptance.
- [Known semantic boundaries](semantic-differences.md): limitations and their history.
- [Backend selection](RUNTIME_LIBRARY_SELECTION.md) and [math backends](math-backends.md): explicit alternatives and scoped comparisons.
- [Remaining work](remaining-work/INDEX.md) and the [active changelog](../CHANGELOG.md): later checkpoints, incomplete gates and delivery evidence.

The sources may represent different checkpoints while work is in progress. Resolve differences against their exact source/report revisions; do not combine stale counters into a new acceptance claim. This documentation split changes no API classification or runtime behavior.

## Baseline and acceptance

The pinned baseline is defined in [reference-baseline.json](../reference-baseline.json). Follow the [architecture decisions](decisions/index.md) and current [project rules](../AGENTS.md). Full compatibility needs the complete supported observable contract and authentic reference evidence; finite local regression or image availability alone is insufficient.

## Historical checkpoints

Every prior paragraph, result, failed attempt and unfinished scope is retained in [ordered compatibility history](history/compatibility/index.md). Old headings labelled current refer to their recorded checkpoint. Put new results in the active changelog and update the owning ledger/report instead of appending historical narratives here.
