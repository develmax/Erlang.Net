# Compatibility checkpoint — 2026-10-09

Reference: **OTP-29.1.1**, commit ad05823719d77c8faee87348ea39513d4e2f99c5. The full implementation is incomplete. Production execution uses C#/.NET independently of BEAM; the reference runtime is development-only and is not installed locally.

Current local evidence is **299/299 tests**, 49 direct registered-MFA cases, a warning-free full build and generated .erl/hybrid/PackageReference/CLI integration. Reports: [part 021](validation/part-021/tests.json), [integration](validation/part-021/integration-results.json).

Exact-version oracle run [37955525454](https://github.com/develmax/Erlang.Net/actions/runs/37955525454) matched **160/160** selected values/exception class/reason cases at **666c4b5**: the prior 93 cases, 30 float, 31 UTF and 6 arithmetic-coercion additions. Version confirmed; complete report; zero mismatches/aborts. [Original report](validation/part-017/differential.json), [verified artifact/head metadata](validation/part-017/remote-checkpoint.json). Same-head Windows CI [37955516789](https://github.com/develmax/Erlang.Net/actions/runs/37955516789) passed 299 tests and full integration. Reference expressions run through erl_eval; compiled-reference-module/optimizer comparison is a separate unfinished track.

Part 018 changes code organization and whitespace only. Domain constants preserve literal values; the [token audit](validation/part-018/source-token-audit.json) compares 21 existing C# files to the preceding head. Local validation was rerun; the historical 160-case oracle was not rerun and no compatibility status is promoted.

Part 019 completes compiler diagnostic/category/token extraction. Text, code, formatting and offsets are preserved; [token audit](validation/part-019/source-token-audit.json) plus the rerun local pipeline provide refactor evidence. No language/MFA status change or new oracle run.

Part 020 moves namespace-level declarations into their matching individual files. All 80 type declarations/namespaces and executable statement token streams are preserved; [layout report](validation/part-020/layout-audit.json) and the rerun full pipeline validate the move. Public APIs, grammar and compatibility statuses are unchanged. Source file locations and C# debug line locations necessarily change.

Part 021 adds development-only style enforcement, domain atom/exception constants and indexed changelog volumes. Existing source tokens/literal values are preserved after constant/builder expansion ([audit](validation/part-021/source-token-audit.json)); local full validation was rerun. No grammar/MFA/reference status is promoted; the historical oracle remains at its stated head.

| Implemented source subset | Evidence and remaining scope |
| --- | --- |
| Maps | Construction, associative/exact updates, exact keys, nested patterns, captured key scope, duplicate/repeated bindings and guard-expression keys; tested cases match OTP. Comprehensions/full map BIFs/scopes/error precedence remain partial. |
| Map/bit guard BIFs | is_map/map_size/map_get/is_map_key and is_bitstring/bit_size/byte_size, including rounded byte counts, badmap/badkey/badarg and guard alternatives. Full inputs/stack traces/resource boundaries remain partial. |
| Integer/binary bit segments | Construction/patterns, signed extraction, units, byte order, unaligned slices/final tails, exact literals, prior/captured sizes, binding rollback and selective receive. Selected cases match OTP; complete scope/error/size boundaries remain partial. |
| Float bit segments | 16/32/64, direct half rounding, nearest-even integer conversion, endian/unit/unaligned extraction, signed zero, finite-only matching and zero-width patterns. Narrowing may retain infinity bits as binary data. 30 selected oracle cases match; full applicable reference inputs remain pending. |
| UTF bit segments/strings | Strict UTF8/16/32 scalar construction/matching, endian/native, unaligned prefixes, noncharacters and literal strings. Invalid/overlong/surrogate/truncated inputs fail matching. Numeric size/unit specifiers are diagnosed; explicit undefined size is rejected by erl_lint and our parser. 22 new local tests and 1,280 deterministic scalar roundtrips pass; 31 selected UTF oracle cases match; full applicable inputs/scopes/errors remain unverified. Non-UTF string size modifiers remain absent. |
| Numeric coercion | Shared Integer.TryToDouble applies nearest-even conversion to mixed arithmetic, integer / and float segments/literals. Overflow is rejected per operand with call-site error class/reason; six local regressions pass. Six selected arithmetic oracle cases match within the 160-case corpus; full numeric inputs/errors/stacks remain pending. Exact integer/float comparison stays rational. |
| Anonymous fun capture | Each attempted clause shadows only its own head bindings and preserves other captures through guard/pattern failure. Selected regressions match OTP; named/external fun and full scope contracts remain pending. |

All broad language/runtime/OTP features and all 49 MFAs remain **Partially compatible**. Selected successful cases do not verify a whole function/module or its stack traces, asynchronous signals, side effects and complete reference suite. Runtime/behaviour/scheduler limits are in [semantic differences](semantic-differences.md) and [matrix](compatibility-matrix.md).

[compatibility.json](compatibility.json) tracks feature evidence; [supported-mfas.json](supported-mfas.json) is the implemented-MFA overlay. The [source inventory](otp-inventory.md) has 1,289 source modules, 38,622 explicit exports and 524 BIF declarations before macro/conditional/NIF/generated/platform expansion. These are not an effective API coverage denominator.

Historical failed runs, fixture corrections, old counts and checkpoint-specific boundaries are preserved in [CHANGELOG](../CHANGELOG.md), parts 001–021, and immutable validation/part-NNN reports. Current-state documents do not repeat superseded pending-oracle claims.
