#329 physical CLR stack guard source prototype

UNBUILT / UNEXECUTED. Only candidate/src/Erlang.Compiler/Execution.cs changes. Exact working Main328 Main2 BA9F82F8 /B86A971D is the parent; published3274bc remains a distinct historical head. No Main/sourceplan/workflow/test edit or build/process/native execution was performed. Root reviews after328 publication.

## Exact change

Both InvokeFrame and InvokeAnonymousFrame check RuntimeHelpers.TryEnsureSufficientExecutionStack immediately before EnterFunction. If false, check current runtime halt/cancellation, await Task.Yield, and check halt/cancellation again. There are no new semantic strings/constants, helper types, counters or APIs. Fast-path true adds no guest reduction and no logical frame. The guard does not call ReduceAsync, reset the existing slice, alter charge count, or introduce an extra owner-signal admission.

The existing InvokeAsync/InvokeAnonymousAsync caller has already acquired the actual source lease and attached image flow. That lease spans the yield through the unchanged caller finally; AsyncLocal flow is awaited normally, without suppressing ExecutionContext or spawning user work. The logical callee frame is created after the guard, just as its body starts. Factory publication and admission stay before the body, with no re-execution/retry of effects on guard resumption. No monitor is acquired by this patch. The native path entering InvokeFrame also sees the same guard; this is compiler source execution, not a change to raw BEAM/native callback dispatch.

This is a candidate mechanism, not a stack-safe claim. Task.Yield resumes according to the current host synchronization/scheduling context. Entry checks cover repeated function descent, not arbitrary nested AST/guard/pattern/term recursion that occurs without a new frame. Nor do these checks prove a long synchronous completion chain or exception/finally unwind is bounded; scheduling races and CLR continuation inlining must be measured in isolated children. If return/unwind remains fatal, do not widen scope silently or call the prototype complete. A heap continuation evaluator/trampoline is a larger separate design.

## Minimal real Erlang controls

non_tail_stack329.erl contains five independent exported cases. Authored finite planned depths are512,2048,8192; test each kind in a separate fresh child,120s wall deadline, CPU2 for SDK. Only append a larger bounded depth if actual earlier results justify it. Expected native results are source hypotheses until actual pinned compilation/execution:

- tail(D) -> D, with accumulator; controls existing tail path.
- non_tail(D) -> D; addition after recursive return keeps a real non-tail continuation.
- anonymous(D) -> D; explicit fun argument recursion also adds after return, avoiding unsupported named-fun syntax.
- unwind(D) -> {D,D}; each non-tail try/after increments process dictionary once on successful return.
- error_unwind(D) -> {caught,D}; a leaf error passes through every after before the outer catch. Do not assert raw full stack equality with OTP.

The actual retained fatal first328 byte-walk fixture must also be replayed unchanged in its own child. Its first source is in the bound focus1 UndefinedLambdaCaseNames.cs; do not replace it with oracle2's tail helper and then call the old failure repaired. The new smaller depth cases are discriminators, not an observed replacement reproducer yet.

## Root command/proof route (not executed)

Original: use the existing controlled pinned source-built OTP workflow/oracle route, exact ad058 runtime/provenance and fresh output. Compile this exact .erl via that runtime's erlc into fresh ebin; invoke ONE export/depth per fresh erl -noshell -pa <fresh-ebin> -eval 'io:format("~w",[non_tail_stack329:non_tail(8192)]),halt().' with whole-child timeout/owned cleanup and raw stdout/stderr/exit. Replace only the named export for the other authored cases. This is a future reviewed integration, no new workflow/POST is authored here.

SDK: use existing GeneratedModuleCompiler + CompiledModuleExecution focus machinery over one source wrapper/export per fresh child, recording the exact source/generated hash and loaded Compiler/Runtime/Tests identities. Preserve originalSource/same expectedTerm when layering the prototype. Root can reuse current bounded focus driver/runner; this artifact adds no execution framework. Always retain process envelope even on fatal exit/no JSON; classify StackOverflow/no-report separately from assertion mismatch, compile/reflection/parse failures and timeout. No automatic retry after fatal failure.

GREEN additionally needs actual actor kill/cancellation during deep pending work, halt transfer, after ordering/exactly-once effects, old source-image lease/purge held through guard suspension, and logical error-handler frame order. Guest reduction totals for finite shallow/deep equivalent controls must remain unchanged by the guard; physical scheduling and elapsed time are not expected equal. Use public state/effects or existing observation bridges, no private counter preconditions or forced production yield hooks. A successful build or depth case alone is insufficient for these lifecycle claims.
