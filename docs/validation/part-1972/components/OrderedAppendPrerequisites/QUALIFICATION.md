# Ordered construction of an append operand

This independent source draft stages the ordinary calls of a nested proper-list operand before emitting its immutable tuple and cons tree. It uses the existing general emitter and call-argument construction planner. There are no module names, source paths, line numbers or release bytes in the production rule.

## Source and baseline

The patch uses the exact Part 1969 compiler closure. Its compiler SHA-256 is `665a0830c1ef6b17a6abca85c2bd67f1f44bced61b368afd52cd9443e7d8ffc5`. The source ZIP SHA-256 is `ef40fcb5d34616d6f68af07ca6e39a739b019a75d8c65e2bf760c8f39cafc009`. The independent preimages, patch digest and changed-file postimages are in [patch-receipt.json](patch-receipt.json). Strict Git application with long paths and whitespace checked passed on Part 1969. The exact Part 1970 GeneralEmitter has SHA-256 `26aaed4da8aa2941651587a8846252f767afa0d8dd711e3889e617bbfc96c6e9`. Its [diff against Part 1969](general-emitter-1969-to-1970.diff) leaves the full EmitAppend span byte-identical. The emitter hunk also passed strict application against a copy of that Part 1970 file.

The pinned OTP source is commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. In `v3_core.erl`, `expr/2` uses `safe_list/2` for cons and tuple children. `safe/2` and `force_safe/2` lift separate occurrences of unsafe expressions without moving them across sibling effects. `beam_core_to_ssa:expr/3` takes tuple members in order and exposes cons inputs before emitting `put_list`. In the five diameter templates, the pinned initial SSA already builds the fallback tree after its final annotation call. The pinned precodegen listing then has eight real Y roots and a single 34-word reservation. See the [saved pass audit](../pending-constructor-schedule.md).

The implementation enters for non-tail ordinary append with a prepared left call and a singleton proper-list tuple operand. It preflights the source tree, retains only proven established Y operands, recognizes ordinary calls and accepts lowered comprehensions represented by those calls. The release comprehensions lower to a local helper whose leading generator argument is itself an ordinary prepared call. The optional leading-producer proof is confined to RHS leaves. The compiler source preimage for that lowering is SHA-256 `3c43d988c183651939586b21376daff7bf2520426c8ddff86a670d2bb817b692`. There must be a dynamic nested constructor completed before another producer in source order. The planner reserves the earlier results, invokes the original expression emitter once per source occurrence, leaves the final result in X0, and emits the pure tree using the existing planner. BIF resolution, binding-changing matches, maps, binaries, improper lists, tail calls and receive scratch fall through. Existing call and arithmetic Line emission remains in use.

These are source and schedule claims. Physical Y assignments, frame optimization and whole-image equality require the C# run. The independent binary outgoing last-read patch is outside this source draft.

## Measured evidence

- The permanent test is `compiler/beam/stages-nested-append-constructors-after-prerequisites`. The same source was extracted from the C# fixture and compiled by the installed pinned OTP. Its accepted `run/1` shape is six provider calls including a prepared leading producer, a 19-word reservation, four tuples and two cons instructions before `++/2`. The [OTP listing](fixture/pinned/disassembly.terms) retains the opcode sequence and file/line annotations.
- The [native fixture](fixture/native.eval) passed six outcomes including successful construction, failure in the left operand, failure inside the RHS nested tuple, late RHS failure, append badarg after RHS evaluation and an unchanged binding-match failure. It verifies the written event trace, meaningful error frames and lines 5, 7, 9, 5 and 14. The pinned ETF SHA-256 is `a2e490b6686c6116c61e0b13ff6e7699d31ae58861f3aeaa7d61398022ae5e34`.
- The actual uncached Part 1969 diameter image has SHA-256 `d3daa46d5e6350dbeb93956179aaff75189fc4f527d8751bb8c66bc4ef45fd7b`, pinned release SHA-256 `afecb6bc42661d2509a339f7665f1497252df8ffee2d455a533b71162225ad0a`. Its five functions still show actual entry width 7, two eager construction groups ahead of the final producer and a 21-word group after it. The [saved actual disassembly](diameter_codegen-part1969.terms) and [baseline native outcome](diameter-part1969-native.log) bind the proof to Part 1969. The 15 private outcomes and public generation of 51 forms matched pinned ETF `791b8f88edd262bcaecee672fd385c3750543aa4c5f11e097ddb32467fe883bf`.
- The [bounded census](release-census.terms) checks 23 release modules, 16 Different and seven Equal on the sealed Part 1969 report. Equal output copies are represented by the raw pinned images under the immutable equality receipt. It compares 324 pinned and 328 actual ordinary append sites, pairing same function and append ordinal only when counts agree and requiring identical last three call MFAs. Seven witnesses had an eager construction in Part 1969 with one later heap check on pinned. The five targeted `diameter_codegen` functions pass the source predicate. Source audit rejects two transitive lookalikes. `diameter_codegen:avp_name/1` passes a fun argument to its left call. `shell:expand_records/2` uses an already bound variable as its left operand. No whole-corpus coverage inference is made. The reproducible [scope selector](select-release-scope.py), [raw input identities](release-scope.tsv) and [OTP structural matcher](census.eval) accompany the report.

## Isolated qualification

Use the output paths of a fresh qualified lane. The exact Part 1969 source ZIP is an overlay. Hydrate its compiler and test closure from the recorded source identities before applying the patch. For a reconstructed downstream base, verify hunk and closure identity independently. Set the process environment before any dotnet invocation.

```powershell
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_PROCESSOR_COUNT = '1'
git -c core.longpaths=true apply --check --whitespace=error-all <ordered-append-prerequisites.patch>
```

Build the baseline with the new fixture and prove the focused structural failure first. Apply the production hunks and run its matching focused check, including the six managed outcome cases. Also run the existing `stages-pure-cons-heads-after-tail-call` test and layout/style checks. Use the pinned OTP runner below on the actual C# fixture BEAM with unchanged lines. The second image is the already pinned provider. The evaluator asserts values, error class, meaningful stack frames, source lines and trace order directly. Its ETF may contain different path strings for equivalent compiled fixtures.

```powershell
$expression = Get-Content -LiteralPath "$fixture/native.eval" -Raw
& $pinnedErl +S 1:1 +A 1 -noshell -eval $expression -extra $candidateFixture "$fixture/pinned/ordered_append_provider.beam" $candidateOutcome
```

Recompile exactly the release paths represented by the 23-row scope. Keep reference and generated images separate. Compare untouched raw BEAM files for exact counts and byte losses. Inspect the five diameter entry frames, source lines, same-order local and remote calls, one 34-word reservation, no pre-call constructor groups and function-normalization deltas. Require no difference introduced by this pass in the two source-negative witnesses and retain the seven exact controls. Compare unchanged modules and prior versus new images before integration.

Repeat the [release native recipe](../binary-outgoing-last-read-draft/release-native-retake.eval) on the actual diameter candidate. Its argument order is `<candidate BEAM> <base_rfc6733.dia from installed diameter src/dict> <outcome ETF>`. It verifies the temporary export chart preserves Code bytes. Compare the ETF to the pinned hash above. Run pinned loading and separate normalized/physical frame checks for all changed release images.

This artifact records the completed pinned and read-only checks. C# compilation, managed red/green, candidate native results, style and candidate scope counts await the scheduled isolated SDK lane.
