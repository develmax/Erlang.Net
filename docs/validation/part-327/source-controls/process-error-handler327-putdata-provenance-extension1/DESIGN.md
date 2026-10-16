# Selected-handler trace provenance: bounded Terms extension

SOURCE DESIGN ONLY — 2026-10-16. No implementation, build, runtime, native, network, Main or Git mutation by this reviewer. Candidate3 is diagnostic-only; Root reports its strict nested selector actually failed. This proposal does not independently attest that execution; the existing independent candidate3 source peer and Root actual receipt remain separate authorities.

## Recommendation

Extend the approved nine-production-path candidate with exactly two Terms paths: the existing ErlangException.cs and a new one-type enum file for typed trace origin. Existing Terms friend access already grants Erlang.Runtime internal access; no friend or callback ABI change is needed. A private immutable origin on each ErlangException distinguishes Uncaptured from SelectedHandlerCaptured. Default construction is Uncaptured. Only an internal selected-handler capture method can create the captured origin; user StackTraceTerm initializers and matching frame values do not establish provenance. There is no ambient/global state, AsyncLocal, Exception.Data, side table, exception subclass or activation-MFA deduplication.

The essential extension to the initial marker idea is that ordinary WithStackFrame must preserve this immutable origin and treat a marked trace as already innermost-first: new frames are outward continuations and APPEND. Only unmarked exceptions retain the current PREPEND behavior. This is a selected-handler-specific policy, not a global reversal of existing exception stacks.

## Concrete operations

1. Ordinary WithStackFrame first checks ExplicitStackTrace. If true, return this exact exception with its exact trace. Otherwise construct the same four-element MFA/argument frame as today. For Uncaptured, prepend exactly as today. For SelectedHandlerCaptured, append the frame to the existing trace. Every clone preserves Reason, ExceptionClass and origin; never mutate the original exception or list.
2. Internal WithSelectedHandlerFrame applies the same explicit-trace identity guard. For Uncaptured, prepend the actual selected handler MFA/three arguments before any existing compiler/BIF frames, then set SelectedHandlerCaptured on the new exception. For an already captured origin, append the fresh actual handler activation and retain the origin.
3. Both sync and awaited selected callback failure paths call this one method exactly once per selected invocation. The two paths are mutually exclusive: synchronous InvokeSelected failure is caught centrally; a returned failing ValueTask is captured only in the async await helper. No shared invocation state or equal-MFA deduplication is required. Distinct or recursive equal callbacks each append one actual activation.
4. Keep per-call Direct/UndefinedFunctionHandler out-policy and its exact admission placement unchanged. It continues suppressing only the synthetic requested missing MFA in compiler/apply. Terms provenance orders real continuation captures; it is not a registry selection policy and must not decide whether fallback occurs.
5. Keep arbitrary public callback signatures and public catch(ErlangException) behavior unchanged. A callback may catch/rethrow a captured exception across an await; immutable origin travels with the exception and survives ordinary WithStackFrame clones. A fresh replacement exception starts Uncaptured, as it is a new failure. Concurrent captures of one reused raw exception create separate clones and cannot contaminate one another.

## Ordering examples and boundaries

Uncaptured source error/1 currently yields a compiler-created BIF frame. First selected capture prepends actual inner handler, so [BIF] becomes [inner-handler,BIF]. A subsequent legitimate helper/caller capture appends, yielding [inner-handler,BIF,helper]. The outer selected callback appends its fresh activation: [inner-handler,BIF,helper,outer-handler]. No prior frame is deleted or matched by value. A plain HOST Nil failure becomes [inner-handler]; nested outer capture yields [inner-handler,outer-handler]. Repeated equal activations remain two or more frames.

Explicit Nil/custom raise, error/2 and error/3 already have ExplicitStackTrace; preserve their exact exception and trace identity, including projected arguments. Do not set ExplicitStackTrace merely to suppress adapter frames. No heuristic tests Nil/nonempty trace, frame MFA, argument shape or frame count to infer a handler capture.

Append must rebuild only the immutable Cons spine, retaining each original frame Term and terminal tail verbatim. Use iterative traversal, not recursive stack traversal or Cons.Items validation that could replace the primary exception with badarg for a HOST-authored improper nonexplicit trace. For a proper trace append before Nil; for an improper trace retain the same terminal tail after the inserted frame. This preservation is a HOST robustness rule, not a claim that native accepts malformed public stack traces. No arbitrary depth cap or normalization should be introduced here.

The SDK compiler's ordering and content accumulated BEFORE the first selected handler capture remain existing behavior. This proposal does not fix a helper/callee-first stack generated before that capture, native tail-call elimination, full argument retention, complete compiler continuation parity, autoload/default error_handler services or undefined_lambda. The tagged outward policy is justified only after a real selected handler has been captured. Native error/1 observation must allow actual first handler arity 3 OR the exact retained arguments; error/2 and error/3 projected arguments stay strict. No blanket native exact-stack claim is warranted.

## Required discriminating controls

- Existing strict distinct HOST inner-before-outer selector, synchronous and delayed Nil callbacks; Root's actual nested RED must become GREEN without changing its expectation.
- Same handler module/function/arguments in two or three real non-tail recursive callback activations: preserve every activation, innermost first, no equality suppression.
- Captured inner handler, then a real ordinary helper/caller WithStackFrame clone, then outer selected capture: exact inner/BIF/helper/outer order and original frame identity/arguments retained. Also a real generated source nested helper path to exercise compiler plumbing, not only a Terms unit construction.
- Source error/1 with existing compiler BIF trace: actual handler remains first. Native comparer accepts arity3 or exact actual arguments only; source error/2/3 preserve strict projected arguments.
- Explicit Nil/custom trace returns the very same exception and trace across both ordinary and selected capture. Retain catch class/reason and existing error/throw/exit behavior.
- Unmarked generic errors, including nonempty manually constructed trace and valid callee undef, preserve old prepend output. Unknown registered send still badarg without invoking handler. Absent handler preserves original missing-MFA contract.
- Concurrent actors and concurrently reused raw exception: independent immutable origin/clones, no leaked handler marker. Captured rethrow retains origin; fresh replacement failure does not inherit it accidentally.
- Existing source success/fault/kill lease tests, both sync and async selected failures, all19 ordinary cases, historical compatibility and actual pinned OTP cases remain required. SDK diagnostics do not substitute for future original observations.

Implementation approval should name the two additional Terms paths and provenance-specific marked continuation policy explicitly. Preserve candidate3 and all actual RED streams. New source candidate must receive independent review before Root formatting/build/SDK execution.
