# Complete source-contract audit: length/1 and ordsets:size/1

Status: independent source review; no build, test, network, original execution, Main mutation or compatibility promotion was performed by this helper.

Pinned original: OTP-29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5. Read the pinned Git blobs rather than trusting the sibling working tree. The applicable upstream test is erts/emulator/test/bif_SUITE.erl:test_length, including good and improper lists across trapping, indirect length calls, guards and the non-trapping ETS match-spec variant. Project policy is AGENTS.md, Source-directed port policy, Part 048.

## Exhaustive logical argument

`ordsets:size(S) -> length(S).` is the entire source routine (lib/stdlib/src/ordsets.erl:120). There is no ordered-set validation, comparison, hashing, callback, head inspection, normalization or requirement that the input elements be unique. Consequently this contract does not depend on unresolved TermOrder, NumericEquality, HAMT, raw map cursors, or lists:usort contracts. Arbitrary immutable element objects are opaque.

The input domain is exhausted by Nil, Cons and every other term. Iterating the finite immutable tail graph partitions every Cons input into a proper Nil terminus or an improper non-Nil terminus. Cons is sealed with immutable Tail and Head; ordinary construction cannot create cyclic guest lists. The invariant is that `cursor` is the original tail after exactly `length` cells, and the original argument is retained independently. Each loop consumes one cell without inspecting its head. This establishes the result or badarg for every representable finite list, rather than extrapolating from a finite sample count.

The pinned `length_1`, `erlang_length_trap` and `erts_trapping_length_1` routines are in erts/emulator/beam/erl_bif_guard.c:210-319. At each available reduction slice, the original traverses up to remaining reductions times 16 cells. If a Cons remains, it saves cursor/count and traps after consuming the slice. A non-Nil terminus raises badarg without charging the final partial slice. Nil charges integer-divided consumed cells/16 and returns the count. Current ErlangListLength.ReadAsync preserves those branches and charging order. Its initial ChargeAsync(0) adds managed cancellation/suspension admission, without incrementing reductions. ProcessContext.ChargeAsync owns actual continuation scheduling, signal admission and cancellation. Exact wall-clock scheduling, the DEBUG/VALGRIND native 50-cell variant, and physical BEAM memory layout are not claims of this logical contract; the approved own-VM progress semantics apply.

The accumulator is a signed long and native is a tagged machine-small counter. An in-memory finite CLR graph cannot allocate enough Cons objects to overflow long; neither implementation accepts an unbounded symbolic list. There is no hidden per-element term comparison or arbitrary explicit list-size cap. This is a representability argument, not an executed giant-list test.

For improper input the native trap keeps the original in args[2]. Current ordinary dispatch preserves a copy of argument references in RuntimeExceptionCapture.InvokeNative/AwaitNative and builds erlang:length with error_info metadata. OrdsetCoreExecution.Size catches only ErlangException badarg and calls OrdsetCoreTrace.Length with the original argument: top frame erlang:length, original singleton argument, native formatter metadata, then ordsets:size/1 at ordsets.erl:120. BacktraceDepth truncation, captured caller continuation coalescing and wrapper omission use the same reviewed capture framework as other source wrappers. Cancellation, halt and suspension transfers are not ErlangException badarg and escape the filter unchanged.

The separate `db_length_1` at erts/emulator/beam/erl_db_util.c:1113 is deliberately non-trapping. Current ReadMatchSpec follows that loop and is selected explicitly in ModuleRegistry.CallMatchSpecGuard and the synchronous compiler GuardValue path. Ordinary asynchronous guard evaluation continues through the normal registered BIF and must retain the existing progress/cancellation checks. Existing nine ListLengthCases include matchspec, guard, dynamic pattern, receive, try/maybe, value and improper-tail contexts. These call sites belong in the length source guard closure; they must not be reduced to the helper alone.

## Remaining admission gates

No source-level behavioral defect was found for either API. A defensible complete-contract promotion still requires:

1. Root admission of actual pinned original observations and strict values/full stack comparisons. Current Part372 size profiles are additional witnesses, not this exhaustive proof. The retained nine original length contexts must remain admitted against the final runtime/source closure.
2. Actual compilation and execution of the proposed four permanent HOST checks on final Main sources, preserving all existing fourteen length lifecycle checks and the receive timer check. The new checks cover direct ordsets progress/cancellation/suspension and both erlang:length and ordsets:size preserving the original improper list after an actual yield. The latter checks continued same-actor usability after failure. Native asynchronous cancellation is not inferred from a raw value comparison.
3. Existing full backtrace metadata comparisons at configured depths and compiled calling contexts must pass; the new reference-identity HOST assertion deliberately supplements, rather than replaces, strict native ETF metadata comparisons.
4. Review and seal the complete current source closure, rerun affected final-source regressions and the publication-head CI. An API status must not be upgraded from a prepared proposal, an old passing DLL, or native sample count alone.

These conditions are concrete validation gates. They do not justify introducing a new implementation or demanding unrelated term ordering fixes before size/length can close. Subject to these gates, both APIs are eligible for logical full-contract promotion. This review does not itself change the registry or prove performance parity.

## Proposed permanent checks

`copy-plan.json` binds two replacement files to current Main hashes. Existing ListLengthHostChecks methods and all existing registrations remain intact. New public methods reuse its real scheduling-boundary harness:

- OrdsetSizeProgress: another real actor executes while size is paused in production traversal, then correct result and normal unwind.
- OrdsetSizeCancellation: another actor kills the suspended process, operation cancels without returning a length/badarg, process completion is killed, and owned unwind completes.
- OrdsetSizeSuspendResume: actual suspend request is admitted at a real boundary, then resume completes the original operation.
- LateImproperOriginalArgument: both length and size traverse more than one slice before badarg; the erlang:length top frame retains the original list by object reference, size keeps its wrapper frame, and the same actor subsequently executes length([]).

No Program.cs replacement is supplied: root should insert the four explicit registrations from the plan at the recorded existing anchor. No production change is proposed. `prepare.py` is artifact-local generation only. Root must build and run; this helper was expressly forbidden to do so.
