# Known semantic boundaries

All current compatibility entries are implemented or partially compatible, not globally verified compatible. Source-driven regressions do not replace running OTP 29.1.1. The exact-version 183-case corpus passed at 3990623 in run 37999487360, including float/UTF/numeric-coercion and literal-string additions; only these value/error cases are verified, not full modules, stacks, signal effects or reference suites. Earlier failed-run diagnoses remain in CHANGELOG.

Anonymous fun matching now shadows captured values separately per clause, preserving a capture in fallback clauses whose own head does not bind that name. Three regressions cover tuple-head mismatch, guard failure and bit-pattern fallback. Named/external fun forms and full scope combinations remain pending.



- Scheduling uses CLR continuation execution with expression reductions; arbitrary C# callbacks cannot be preempted. Local process completion denotes logical death and signal publication, not guaranteed completion of arbitrary host callback cleanup. No BEAM scheduler fairness guarantee.
- Local links/monitors and mailbox delivery are lock-serialized. Atomic local spawn_monitor and iterative link-failure propagation are implemented. Full Erlang signal queues, priority messages, aliases, spawn_request, distributed identity and monitor option variants are pending.
- PID/port/reference identities currently have 64-bit IDs. Wider reference ID vectors, exact distributed identity ordering and external fun terms are incomplete.
- Signed zeros are distinct under exact equality for OTP 29.1.1. Numeric equality ignores the sign. Map key ordering puts integer types before float types. Atom ordering compares Unicode codepoints. Float nonfinite values are rejected. Atom interning/table limits and complete Unicode literal escapes are pending.
- Map source construction/updates/patterns now use exact keys and support the existing guard-expression subset. Map comprehensions, the full map BIF/guard set and complete oracle verification are pending. Bitstring source construction/patterns support integer/binary/float/UTF segments, sizes/units and byte order, signed integer extraction, guard sizes and prior-segment bindings. Float construction uses 16/32/64 bits with direct binary16 rounding and explicit nearest-even integer conversion; narrowing may retain infinity bits, but decoding infinity/NaN fails matching. Zero-width float patterns bind positive zero. Local tests pass; 183 selected cases, including 31 UTF and 23 new literal-string cases, match OTP at 3990623. UTF8/16/32 use strict scalar decoding, including noncharacters and unaligned offsets. UTF literal strings expand into scalar segments. All explicit UTF sizes/units, including literal undefined, are rejected. The earlier explicit-size abort and correction remain historical evidence in part 016; the corrected current run is complete. Integer/float literal-string construction now supports per-codepoint sizes/units/endian, one size evaluation and empty-string validation. Non-default non-UTF strings remain forbidden in patterns by the pinned lint contract. Static all sizes are rejected for non-binary segments during parsing; a variable evaluating to all remains a runtime size check. Complete scopes, error ordering/resource boundaries and compiled-reference comparison remain pending. Remaining source gaps include records, includes/macros, comprehensions, try/catch/after, if, types/specs, external/named fun references and dynamic module calls. Unsupported syntax produces diagnostics rather than a language rewrite approximation.
- Local module tail calls through sequences/case/receive are trampolined. Anonymous/named fun tail calls and all other compiled continuation forms are not complete; deeply nested non-tail calls may exhaust CLR stack.
- Error stack frames are currently placeholder empty lists. Complete Erlang exception shapes, stacktrace capture, badfun/badarity/undef argument details and complete arithmetic error contracts need differential hardening. Shared nearest-even integer conversion and per-operand finite checks now cover mixed arithmetic and integer /; six selected arithmetic oracle cases match in the complete 183-case corpus; broader inputs/errors remain unverified.
- Map guards is_map/map_size/map_get/is_map_key use the existing exact map-key contract. Nonmaps raise {badmap,Value}; map_get missing keys raise {badkey,Key}; guard errors reject the guard alternative. Arbitrary maps module calls remain illegal in guards. Differential outcomes now compare value or exception class/reason; stack traces, signals and side effects require separate future comparison tracks.
- is_bitstring accepts binary and non-octet BitString values; bit_size returns bits and byte_size rounds up for partial bytes. Non-bitstrings produce badarg for size BIFs. Current representations have int-sized bit lengths; larger BEAM/resource boundaries and error stack fidelity are unverified.
- gen_server has call/cast/info/stop, C# callbacks and reference-monitor replies. It lacks alias-based late-reply suppression, full OTP exception context, callback module adaptation, sys protocol, hibernation, continue/timeouts, name variants and hot upgrades. Late replies after timed-out calls can remain in the caller mailbox.
- Supervisor strategies/policies/intensity and ordered shutdown exist. Dynamic child APIs, auto_shutdown/significant children, complete childspec validation, restart retries, complete startup-failure contracts and nested-supervisor shutdown contracts are pending. Child factories must return a live, long-running process.
- io:format supports only ~s, ~p, ~w, ~n and ~~; many formatting directives, devices and Unicode/iodata variants are absent. BIF/MFA error compatibility is only partial.
- Hybrid blocks are recognized at selected C# boundaries and require async context with `erlangProcess`. There is no implicit C# local conversion. Each block has a fresh Erlang binding scope. Synchronous receive, arbitrary inline tuples/calls/operators in C#, cross-block bindings, diagnostics column maps and debugger stepping are pending.
- Compiler output currently constructs an AST executed by the C# evaluator. Direct operation lowering/optimized IR is pending. Module AST collections are trusted compiler structures and not a hostile-input sandbox.
- The package supports the pinned SDK/net10.0 prototype. Other SDKs/TFMs, design-time IDE builds and publish/trimming/AOT compatibility are unverified.
- Inventory exports are extracted before conditional/macro expansion. Source modules, explicit exports and BIF declarations do not establish complete effective public-contract coverage.
- Distributed Erlang, ETS/DETS, code loading/upgrades, NIF/ports execution, applications, gen_statem/gen_event and the remaining OTP libraries are Not started.

Reference comparison currently evaluates expressions with erl_eval. Compiled-reference-module/optimizer equivalence and full reference compiler suites are separate unfinished tracks.

Parts 018–019 are code-organization refactors. Domain constants and compiler diagnostic builders preserve the existing literal values, diagnostic texts/codes/offsets and grammar. Local validation was rerun; reference results remain attributed to their historical tested heads. These parts add no semantic compatibility claim or new boundary.

Part 020 changes source-file organization only: namespace-level types now each have a matching file, with nested declarations preserved. Source/C# debug file and line locations change; no grammar, term, API or runtime algorithm changes. The local full pipeline and type-token/layout audit pass; no new reference-suite coverage is claimed.

Part 021 is a style/constant/journal refactor, with a development-only SDK Roslyn checker. Existing code tokens and literal values match after domain-constant/message-builder expansion; no grammar/runtime/MFA change is claimed. C# source/debug line positions and journal locations change. Historical reference results remain attached to their actual head. The style utility's SDK dependency is not a production BEAM or runtime dependency.

Part 022 only names existing ETF numbers. Nesting depth 256, default byte limit and support for at most two reference ID words remain implementation bounds. Numeric tags, field/header sizes, thresholds and compression buffer behavior are unchanged; no ETF compatibility expansion or new pinned-oracle case is claimed.

## Part 023 — Readable switch expressions

Switch expressions place the opening brace, each arm and the closing brace on separate lines. The SDK-backed style tool enforces this throughout ordinary C# sources and full validation checks it. TermOrder.Rank, Parser.Precedence and Semantics.Variables preserve their exact non-trivia token streams, values and pattern order. The independent guarded-pattern probe rejects compact layout, repairs it and passes a second check while preserving a string containing switch-like text. No new Erlang feature, MFA, compatibility promotion or dependency. Current 299-test/full integration reports are saved in validation/part-023; the 160-case oracle evidence remains historical at 666c4b5 and was not rerun.

## Part 027 — List sequence resource boundary

lists:nth/2 and nthtail/2 use iterative Cons traversal, including valid improper prefixes. Sequence values/count calculations use BigInteger. Materialized seq/2,3 results exceeding int.MaxValue elements raise system_limit before allocation; this is an implementation bound, not claimed OTP behavior. Smaller allocations may still exhaust host resources. Selected function_clause/badarg/value contracts have permanent local tests and 36 pending oracle cases; complete stacks/resources/applicable inputs remain unverified. Readiness declaration percentages do not quantify semantic compatibility.

## Part 028 — Evidence update

All 36 selected lists values/class/reason cases now match within a complete **219/219** exact-version result at 6f34dc9. This supersedes part-027's pending reference status only for those selected cases. The sequence count resource bound and full stack/input/side-effect limitations remain. No new runtime/compiler semantics or dependency in this part.

## Part 029 — Key-search architecture scope

Key-search position and small-key fast path model the pinned 64-bit reference small integer representation. 32-bit BEAM position boundaries are not verified. The small integer/float element rounded-double comparison is deliberately local to key search; global numeric equality, exact member/2 and map keys are unchanged. Reverse/1 now distinguishes short-clause function_clause from reverse/2 badarg on longer improper lists. Value/class/reason regressions pass locally; full stacks/resources/reductions remain unverified.

## Part 030 — Selected reference confirmation

All 30 added key-search/reverse cases match within complete 249/249 at 85bd19f, superseding the part-029 pending status for those cases only. Position architecture scope, full stacks/resources/reductions and compiled-reference-module boundary remain. No runtime/compiler semantics changed in this evidence checkpoint.

## Part 031 — Module comparison scope

The new reference track compiles full fixture modules with normal OTP compiler optimizations and invokes run/0; .NET emits actual generated C# Release assemblies and runs their AST/evaluator definitions. This checks selected compiled-module behavior and generation integrity, not full optimizer equivalence/native lowering or stack/debug mappings. Existing expression reports remain erl_eval evidence. No language/runtime semantics changed; compile/load infrastructure failure is kept separate from a compared value/class/reason mismatch. The initial host SDK memory failure and bounded-process retry are preserved separately.

## Part 032 — Compiled-module evidence boundary

Selected 14 generated C# Release assembly/default-optimized BEAM module outcomes now match within complete 263/263 at b13b56a, alongside the preserved 249 expression cases. Earlier pending module status is superseded only for those fixtures. Generated code remains AST/evaluator-based; full optimizer equivalence/native lowering/compile warning diagnostics/stack mappings/signals/resources/reference suites remain unverified. No semantic implementation changed in this evidence-only checkpoint.

## Part 033 — Atom representation boundary

A fixed ten-name immutable object cache reduces repeated factory allocations; arbitrary names remain uncached. Name-based equality/hash/order is preserved, including freshly constructed Atom objects. CLR reference identity is not a semantic test for Erlang equality. This is not the BEAM global atom table and does not establish its atom-count/length/resource limits. Existing oracle evidence remains at b13b56a; no new oracle/performance measurement here. Independent test fixture atoms are intentionally kept with their source examples; only protocol wrappers share domain constants.
