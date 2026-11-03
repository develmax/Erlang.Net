# Integration evidence — 2026-11-03

[PR #102](https://github.com/develmax/Erlang.Net/pull/102) owns current merge/check/review status. All receipts below identify their source window; earlier failures remain evidence of those attempts.

- [Documentation and link reconstruction](final-doc-verification.json) and [API/evidence verification](final-api-verification.json) record the first integrated snapshot; newer revalidation reports are linked below.
- [Source identity refresh](source-plan-refresh/audit.json), [continuation](source-plan-refresh/continuation-audit.json) and [published-head correction](source-plan-refresh/checkout-source-identities.json) retain original manifest bytes in content-addressed files. These are source identities and audit records, not new execution acceptance or compatibility promotions.
- [Math three-platform CI receipts](math-ci-41631118602/README.md) apply to head20e02eb9. Later-head CI remains required.
- [String review fix](string-review/summary.json): the UTF-8 validator reads at most four initial bytes instead of copying an entire suffix for every grapheme. Before/after5226 actual outcomes and13 backing observations are byte-identical. The inherited1030 stack differences remain; full string acceptance is not claimed. Both full raw results/reports are retained as gzip files in that directory.
- [Ordsets partition corpus](revalidation/partition-corpus.json):24/24 cases, all12247 observations in each backend. Every raw input/expected row is preserved; all12 partitions reconstruct the unchanged input and golden ETF files byte for byte. Deadlines are unchanged. The partition tool refuses unsupported ETF tags rather than normalizing data. The earlier aggregate timeouts and unsuccessful host-actor experiment remain under revalidation as gzip logs/reports.
- [Roslyn layout equivalence](revalidation/style-equivalence.log) confirms identical token kinds/text and directives in the six layout-only changes. Intentional review/harness changes are described separately.

No new runtime dependency, third-party license change, NuGet publication or complete-module compatibility promotion is introduced here. Final full regression, fresh CI, source/metadata admission and review are delivery gates. Later concurrent source edits in the original shared checkout are preserved separately; this revalidation uses the isolated integration snapshot.
