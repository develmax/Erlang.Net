# Decisions

1. Pin stable OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The existing reference HEAD is newer and is not the baseline.
2. Pin .NET SDK 10.0.400 and C# 14, net10.0. Avoid external restore dependencies in the initial runtime/test bootstrap.
3. Use distinct term classes, explicit exact/numeric comparison, immutable cons cells/maps/buffers and Erlang exceptions. No object/null-based term contract.
4. Use ProcessContext plus async continuations and reduction checkpoints. Tasks are scheduler infrastructure; local lifecycle, signals and selective receive are separately implemented.
5. Use dedicated Erlang lexer/parser/analyzer; borrow Roslyn only for C# lexical context. No regex language rewriting and no Roslyn fork.
6. Emit checked AST construction in generated C# as the first compilation representation. Keep direct lowering and optimized IR as explicit future compiler tasks.
7. Adjust the mandatory sample to an async process callback. Suspending receive must not block a C# public synchronous method or silently change its signature.
8. Supply #line plus original line-count preservation now; full expression columns and debugging require a later source-map representation.
9. Maintain source inventory separately from implemented MFA coverage. Classifications are provisional until component-level reuse/port audits are done.
10. Keep package/tool prototypes local, with a real PackageReference consumer. Public license selection and publication are deferred.
11. PowerShell validation and Bash commands in the development-only oracle CI workflow are non-C# test-infrastructure exceptions. Runtime, compiler, inventory, differential runner and benchmark code are C#.
12. The local exact-version Erlang runtime is absent. Remote differential execution builds the pinned source in isolation; run 37938184184 built successfully and matched 16 cases before an invalid Unicode source literal. Part 009 corrects the earlier transport diagnosis: explicit UTF-8 decoding in a follow-up run still failed because erl_scan excludes U+FFFF. The runner checks the exact release, compares tagged value/error outcomes and does not normalize meaningful term differences. Part 008 passes source as Base64 UTF-8 through an ASCII command, then explicitly scans/evaluates codepoints. Partial reports distinguish infrastructure aborts from semantic failures. Complete stacktrace/side-effect/signal comparison remains a separate future track.
13. Map pattern keys evaluate against the scope preceding the whole pattern, separate from newly bound value patterns. Closures preserve captured key scope while their value-pattern variables shadow outer variables. Optional process context reaches nested patterns for guard BIFs such as self.
14. The user authorizes publishing completed commits to the configured Git remote. Public NuGet publication remains deferred. Keep detailed English entries and immutable per-part validation reports; do not overwrite historical evidence when updating current reports.

## Part 018 — Semantic constants and readable code

Group reusable constants by semantic ownership, never by literal equality alone. Error reasons and exception classes are separate term-domain files; compiler bit segment names, byte orders, syntax defaults, floating widths, storage layout and Unicode limits have distinct groups. Lexer token kinds are not bit segment types; BIF names are not exception classes; integer default width is not bits per byte; UTF32 length is not maximum UTF encoding length. Keep source-language fixture text and one-off diagnostic messages near their use unless an actual shared contract exists. Use .editorconfig and logical blank-line separation for future changes; the hybrid source remains subject to its own preprocessing pipeline.

## Part 019 — Compiler diagnostic catalog clarification

The user's screenshot follow-up includes one-off compiler diagnostic strings, not just repeated values. This supersedes the earlier decision to keep such compiler messages inline: all CompileException codes and texts now belong to CompilerDiagnostics.cs, grouped by lexer/parser/semantics/bit-pattern/hybrid ownership. Ten named builders retain original interpolation for dynamic diagnostics. Source-language fixtures stay local and independent. LexerTokenKinds.Integer/Float remain separate from BitSegmentTypes.Integer/Float. BitSpecifierCategories.Unit (conflict key) and BitUnitSpecifier.Name (source spelling) remain separate despite both being unit. Limits of a source unit are distinct from default units with equal values.

## Part 020 — One namespace-level type per file

The user's file-layout requirement supersedes composite type files, including the part-019 shared diagnostic catalog. Each namespace-level class/record/struct/interface/enum/delegate lives in TypeName.cs. Nested types remain in their owning declarations. Top-level executable statements stay in Program.cs and helpers move into their own files. Namespaces, access modifiers, generic parameters, attributes, type/member tokens and source values remain unchanged. Original file using directives are copied to extracted types to preserve resolution; unused imports are harmless and were not independently simplified. CompilerDiagnosticCodes, LexerDiagnostics, ParserDiagnostics, SemanticDiagnostics, BitPatternDiagnostics and HybridDiagnostics now each have a separate file, as do bit-constant groups. The package-consumer fixture copies every root HelloHybrid C# file so moved fixture types are included.

## Part 021 — Bounded journal and enforced style

Root CHANGELOG.md is a short index. Append only to the latest docs/changelog/NNNN.md volume; roll over before 32 KiB or 400 lines, preserve whole entries and keep closed volumes immutable. The migration preserves parts 001–020 exactly after accounting for relative-link rebasing. Corrections are dated additions, never history rewrites.

Format namespace-level types in matching files, enums one member per line, no leading blank lines, blank method boundaries and blank lines before returns after preceding statements in the same block. Keep a guard condition together with its returned statement. Four or more parameters/arguments, or multi-item token width over 120 characters, use individually indented lines. Explicit C# LF attributes make style checks stable on Windows checkout. Use tools/Erlang.Style --write/--check; full validation includes the check. Native-Erlang Program.cs remains in the dedicated hybrid pipeline.

Shared process exit reasons, signal inputs, message tags, monitor kinds and registry/dictionary absence sentinels have separate semantic owners. Identical undefined values stay distinct; normal termination is shared by runtime/OTP; exit exception class differs from EXIT message tag. GenServer/Supervisor protocol tags and startup-error tags are distinct domains. ETF/compiler/CLI/oracle diagnostic messages and builders own all literal exception arguments, including messages used once. Oracle success/error tags remain distinct from exception classes and process-body completion results. Test source strings remain independent fixtures.

## Part 022 — ETF numeric ownership

Use byte constants for wire tags in EtfTags and separate header, scalar field, float, atom, integer, tuple, bit-binary, map, PID/reference, compression and resource types. Preserve byte values and all numeric control flow. Nesting depth 256 is this implementation's bound; big digit count 256 and tuple arity 256 are independent one-byte/large-encoding thresholds. Atom codepoint cap 255 and small integer maximum 255 are separate contracts. UInt64 field width and binary64 float width both happen to be eight bytes and remain separately named. Compression buffer 4096 is implementation tuning, not a protocol field. PID serial and reference ID word shifts are distinct owners despite both using 32. Header bound/offset concepts also retain separate names. Existing DefaultMaxBytes remains the public named constant; ordinary loop zeros/ones are not mechanically deduplicated.

## Part 023 — Readable switch expressions

Switch expressions place the opening brace, each arm and the closing brace on separate lines. The SDK-backed style tool enforces this throughout ordinary C# sources and full validation checks it. TermOrder.Rank, Parser.Precedence and Semantics.Variables preserve their exact non-trivia token streams, values and pattern order. The independent guarded-pattern probe rejects compact layout, repairs it and passes a second check while preserving a string containing switch-like text. No new Erlang feature, MFA, compatibility promotion or dependency. Current 299-test/full integration reports are saved in validation/part-023; the 160-case oracle evidence remains historical at 666c4b5 and was not rerun.

## Part 024 — Literal-string bit construction

Integer/float literal strings now support per-codepoint sizes, units and byte order. The AST retains the string as one BitSegment (IsStringLiteral) so its size expression and bindings execute once, including empty strings. Construction expands codepoints after evaluation, reuses existing scalar encoders, and validates empty strings with zero while discarding output. Ordinary list expressions remain invalid numeric values. Default and unsized UTF strings retain existing behavior; modified non-UTF strings raise ERL004 in patterns, including empty strings. Generated C# preserves the metadata; the optional seventh record constructor parameter requires rebuilding existing binary consumers/generated code.

Pinned eval_bits.erl/erl_lint.erl source audit is saved in validation/part-024/reference-audit.json. Fifteen permanent cases replace one pending-feature diagnostic (net +14): independent byte vectors, unaligned packing, Unicode truncation, float encodings, size evaluation/binding, zero-width/empty success and errors, ordinary lists, guard/map keys and pattern rejection. Full local validation passes **313/313** tests plus .erl/hybrid/PackageReference/local CLI integration. No new MFA or dependency and no broad compatibility promotion. The corpus now plans **183** cases (23 additions); live comparison is pending. Historical 160/160 evidence at 666c4b5 remains distinct. Source/debug stacks, error/side-effect ordering, large resources and full reference suites are still incomplete.

## Part 025 — Static all-size lint correction

Initial exact-version run 37998319734 at badb643 built OTP-29.1.1 successfully and matched **174/183** executed values/class/reason comparisons with zero mismatches, then aborted on <<"":all>> with erl_lint illegal_bitsize. The report is incomplete and does not verify the full corpus. Initial report/head/artifact metadata are preserved in validation/part-025; same-head Windows CI passed 313 tests and full integration.

Pinned erl_lint bit_size/bit_size_check permits static all only for binary segments. Parser now rejects non-binary static all (ERL002), including empty strings and float/integer scalars. The special size atom belongs in BitSizeAtoms and runtime/pattern use sites share that meaning. Unknown sizes resolving to all still reach runtime validation. The runtime corpus replaces the illegal static expression with case all of S -> <<"":S>> end, retaining 183 planned cases. A new diagnostic case covers six forms; the earlier float-all runtime fixture moves to that diagnostic test. Final local validation passes **314/314** plus full integration. Two local attempts stopped at the obsolete float fixture before its exact source was corrected; reports preserve the initial failure. Live comparison on the corrected head remains pending; no compatibility promotion, new MFA, dependency or license change.

## Part 026 — Complete pinned 183-case evidence

Run 37999487360 at **399062314cb5ff609e66b82a4cb923fcd2840a94** built the pinned OTP source and completed **183/183** value/exception class/reason comparisons with version verified, zero mismatches and zero aborts. Source audit confirms all previous 160 expressions are unchanged and the 23 additions exactly match the current corpus. Same-head Windows CI 37999472859 passes **314/314** and full generated .erl/hybrid/PackageReference/CLI integration. Immutable raw reports, artifact IDs/ZIP hashes, tested heads/jobs, corpus audit and 117-type layout inventory are in validation/part-026. Initial abort remains preserved in part-025. This checkpoint changes documentation only; full local validation was run in part-025, not repeated here. No new dependency, MFA, baseline or broad compatibility promotion. Full compiled-reference-module/optimizer, stacks/signals/side effects/resources/reference suites remain incomplete; full assignment remains approximately 5%.

## Part 027 — Reproducible per-module progress

Report actual registered and directly tested MFAs against the pinned union of explicit exports/BIF declarations, plus deltas from the explicitly selected immutable prior report. Keep partial C# component milestones and remaining tasks separate; never invent weighted semantic percentages. Fail readiness generation on baseline mismatch, registry mismatch, failed/stale tests or incorrect direct-test evidence. Full zero-registration rows live in JSON, focused rows in Markdown. lists nth/nthtail/seq reuse immutable Cons and BigInteger after pinned source audit; no Enumerable index/range shortcut, copied OTP implementation or new dependency. Retain the int.MaxValue sequence resource difference explicitly. [Reuse audit](dependency-decisions.md), [readiness](MODULE_READINESS.md).

## Part 028 — Preserve tested-head provenance

Complete oracle and Windows CI results are attributed to the same implementation head **6f34dc9**, with raw artifacts, ZIP hashes and 183+36 corpus provenance in validation/part-028. Refresh readiness milestone evidence without inventing a percentage promotion. Keep the delta baseline at part-026 so this evidence-only checkpoint does not erase the last feature increment (+4). Local full validation was run in part-027; no executable source changed here. Full compiled-reference-module and resource/input contracts remain separate tasks.

## Part 029 — Preserve native key-search distinction

Reuse existing term/numeric infrastructure in iterative C#; preserve the pinned native small-integer/float fast path rather than substituting global numeric equality for every key. Keep architecture-bound small integer limits in ListKeySearchLimits and result atoms in ListKeySearchAtoms, each in its own file. Repair reverse/1 clause selection; arbitrary reverse/2 tails remain valid. Readiness delta now uses part-028 as its explicit preceding feature baseline (+3 MFAs). Source/reuse audits and permanent boundary tests precede publication; live 249-case reference comparison follows on the published head.

## Part 030 — Preserve scoped module progress

Store complete 249-case and same-head 344-test CI artifacts with source 219+30 provenance and verified ZIP hashes. Retain part-028 as the explicit comparison baseline so the last feature increment remains visible (+3), even though this evidence-only checkpoint adds no MFA. Keep rounded key-search comparison, exact membership/map keys and 64-bit reference scope separate. Next implement a separate compiled-reference-module oracle track using the existing compiler/runtime and development-only pinned reference; it remains unrun here.

## Part 031 — Separate compilation from expression evidence

Reuse SDK Roslyn and BCL collectible contexts to exercise actual generated assemblies without adding packages or a production BEAM requirement. Use ASCII Base64 transport for complete Unicode module sources and compile:forms/code:load_binary in a fresh pinned reference process. Keep 249 expression cases and 14 module cases separately reported with source/generated hashes and independent expected fixtures. Test repeated identical assembly loading and rejected source locally. Readiness comparison baseline moves to immutable part-030; no new MFA, delta 0. Full optimized behavior/stack/resource/reference-suite scope remains incomplete.

## Part 032 — Keep track-level proof explicit

Preserve complete 249-expression and 14-compiled-module outcomes separately, with same-head 361-test CI, archive/head provenance and source/generated SHA256 audit. Selected default-optimized compiler outcomes now have actual reference evidence; do not promote full optimizer/native-backend or whole-module compatibility. Readiness remains 56 MFAs with delta 0 against part-030; this work advances validation milestones. Reuse the existing pinned workflows/SDK tools, no new production dependency or license choice.

## Part 033 — Reuse values without merging protocol roles

Keep CommonAtomNames/AtomCache internal to Terms: a spelling identifies the shared immutable value, not a success/error role in every caller. Preserve IoResultAtoms.Ok, ExecutionResultAtoms.EmptySequence and OracleOutcomeTags.Success as independent contracts. Choose a fixed FrozenDictionary rather than unbounded global string/object interning. Unknown names allocate normally; existing null/name-value contracts remain intact. Permanent tests exercise concurrency, equality/hash, fresh Unicode values, parser/ETF reuse and zero per-call allocation for a warmed common value. Fix SDK style insertion at full XML-comment trivia boundaries and test idempotence. Readiness comparison moves to immutable part-032 CI (delta zero); no MFA/grammar/status promotion.

## Part 034 — Observe compiled order rather than infer it

Retain stage-separated bit expression evaluation/binary validation and clause-local binding environments. Exercise these through actual generated assemblies and separately compiled pinned modules, with independent expected values and observable process dictionary order. The twelve fixtures add evidence without changing the existing implementation or adding MFAs; report new local proof separately from pending reference proof. Preserve all previous expression/module sources, expected outcomes and generated hashes. Reuse existing SDK/BCL/compiler/runtime/oracle with no library/license change; comparison baseline is immutable part-033 local tests.

## Part 035 — Separate expression order from binding visibility

Pinned erl_lint expr_bin/bin_element validates every construction value and size against the original Vt and merges their exports afterward. Snapshot/merge construction scopes accordingly; do not change runtime evaluation ordering or sequential pattern binding. Preserve the aborted reference run and move the invalid input to an explicit diagnostic track rather than accepting it or silently deleting evidence. Compare specific unbound_var reason terms and independent C# ERL006/text expectations. Complete aggregate counts require all three tracks, with process/scan/parse failures still infrastructural. Normalize diagnostic reasons only; annotations, warnings and full diagnostics remain unverified. No new dependency/MFA/status promotion.

## Part 036 — Keep corrected proof and the failed attempt separate

Preserve complete282/282 oracle and same-head 389-test CI with exact head/archive metadata and all three source/expected/hash/diagnostic audits. Keep the initial268-case abort immutable; it motivated a real construction-scope correction. Only selected normalized reasons and C# diagnostic code/message are confirmed, not warning/annotation/full diagnostic or optimizer equivalence. Readiness comparison moves to immutable part 035 tests; no MFA or broad status promotion. Reuse existing workflows/BCL/SDK tools with no dependency/license change.

## Part 037 — Preserve improper-tail boundaries and arbitrary integer indices

Iterate Cons cells rather than converting to CLR lists or narrowing counts. last rejects improper termination; split permits arbitrary tails once count reaches zero, distinguishes proper exhaustion badarg from unmatched improper tail function_clause and preserves original suffix identity. Reuse immutable terms and existing reverse; no new dependency/license choice. Permanent tests and 22 oracle inputs cover these distinct contracts. Compare readiness against immutable part036 CI389/56; local414/58 is new evidence, historical282 oracle is not a rerun.

## Part 038 — Scope compatibility proof to checked contracts

Preserve complete304/304 and same-head414-test CI with exact head/job/build/artifact SHA metadata. All249 prior expression sources and28 compiled sources/independent outcomes/generated hashes plus5 diagnostic reasons/code/messages remain checked. Added22 last/split reference boundaries agree. This establishes selected value/class/reason behavior, not full lists/resource/error annotation/stack equivalence. Compare readiness against immutable part037 tests; evidence-only delta0, cumulative58 MFAs/lists15/91. No executable or dependency/license changes.

## Part 039 — Keep existing member semantics and preserve recursive error order iteratively

member/2 was already registered and exact/lazy; retain it rather than claim another MFA. Extend permanent contracts including signed-zero distinction. append/1 first traverses the outer spine, then combines prefixes from right to left using existing append/2 behavior; the last element is returned untouched. This preserves outer function_clause precedence over malformed-prefix badarg and suffix identity without recursion. New MFA count is one,59 total/lists16/91. Full resource/reduction/stack equivalence remains unverified. Reuse existing terms/BCL helpers; no dependency or license change.

## Part 040 — Keep new exports separate from stronger existing contracts

Preserve330/330 and same-head442 CI with exact head/job/reference build/archive SHA provenance. All 271 prior expression sources and28 compiled sources/independent outcomes/generated hashes plus5 diagnostics remain checked. New26 cases establish selected append/1 and existing member/2 behavior; only append/1 increases the registry. Compare readiness to immutable part039 tests, evidence-only delta0;59 MFAs/lists16/91. No full resource/reduction/stack/module promotion or executable/dependency/license change.

## Part 041 — Tail-first flatten with an explicit list-spine distinction

Follow pinned do_flatten by evaluating each tail before the head; frames distinguish required Cons/Nil spines from opaque leaf terms. This preserves function_clause on malformed nested tails, leaf order, and the untraversed flatten/2 suffix without recursion. Duplicate reuses immutable element identity. Its local int.MaxValue cap has its own ListDuplicateLimits type despite equal Sequence limit value; guard before narrowing and do not claim reference resource equivalence. No external dependency/license change. Current62 MFAs/lists19/91; validate against immutable part040 CI442/59.

## Part 042 — Separate value contracts from local allocation limits

Preserve355/355 and same-head472 CI with exact head/job/build/archive metadata and all297 old expression,28 compiled and5 diagnostic source/hash/expected contracts. The25 new cases establish selected duplicate/flatten values/errors; huge positive allocations and local int.MaxValue/system_limit cap remain explicitly outside reference proof. Readiness comparison immutable part041 tests;62 MFAs/delta0 in this evidence part, lists19/91. No executable/dependency/license/resource/full-module promotion.

## Part 043 — Name strings by semantic owner

Share language operator spellings between compiler and runtime and erlang MFA names between registration and guard validation using Terms-level catalogs. Compiler-local syntax/keywords/attributes/scopes and Runtime-local maps/io/list names retain their own owners. Slash/minus/colon in different grammar roles stay distinct; error/throw/exit BIF names remain separate from exception classes. Use string.Empty only for the empty string representation. Existing immutable atom caching is unchanged. Verify319 references through SDK Roslyn token/value audit and preserve generated hashes; no full compatibility promotion.
