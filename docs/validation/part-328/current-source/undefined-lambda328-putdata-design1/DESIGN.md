# 328 custom undefined_lambda/3: concrete invocation route design

SOURCE DESIGN ONLY, 2026-10-16. Depends on a future reviewed327 actual selected-handler dispatcher/provenance implementation. No implementation, Main edit, build, SDK/VM/native/network execution or Git mutation by this reviewer. The exact source-bound328 audit remains immutable and is refined here; no claim of complete fun-loader compatibility is made.

## Native decision and genuine current gaps

Pinned ad058 beam_common.c1645–1728: construct original argument list; badarity first1669–1674; a pending purge suspends/retries1690–1695; unresolved local fun plus CURRENT module gives badfun1697–1705; no CURRENT module directly selects the process handler's undefined_lambda/3 at1711–1725. Absent callback gives undef, never a recursive undefined_function lookup. JIT beam_jit_common.cpp1307–1364 independently agrees. error_handler.erl117–126 tries load then apply(Fun,Args); current-but-incompatible prevents infinite recursion. fun_SUITE753–812 and nofrag_SUITE130–162 are actual source references, not executed observations here.

RuntimeFunctionEntries.Restore currently has an immediate matching Factory shortcut. Thus create/export ETF → purge → Restore in the same runtime does NOT prove its unresolved wrapper: a saved Factory returns a real source closure whose invocation enters SourceFunctionInvocation and ModuleRegistry.AdmitSourceFunction. The unresolved body independently captures the decoding registry/runtime and uses Modules.Exports.Any; both are wrong foundations for executing-runtime selection and zero-export CURRENT code. FunctionTerm.Invoke already checks badarity before the body and must remain unchanged.

Current IsModuleLoaded locks loadedModules and includes PublishModule with no exports. DeleteModule removes loadedModules and current image but retains an old snapshot. Consequently absent CURRENT alone is not enough to dispatch a callback while a valid old fun is still runnable. Classify exact active old code before missing-function fallback. AdmitSourceFunction already owns pending-purge retry/control pumping, matching fingerprint and image lease; preserve that machinery.

## Proposed first meaningful bounded increment

Cover BOTH decoded unresolved wrappers and stale source factories in the same increment. Otherwise same-runtime purge/restore would still miss the feature. Do not dispatch in ETF decoding or Restore, and do not change FunctionTerm or raw-BEAM support.

A restored local FunctionTerm should retain immutable original identity/environment and an invocation delegate, even when a decoding-time factory exists. At invocation cast to the real ProcessContext and use context.Runtime.Functions for exact entry/factory resolution; the decoding runtime remains only identity/import bookkeeping. Never use decoding Find or decoding Modules as authority. The original wrapper Fun, not a temporary factory-created closure, must be passed as argument2 of undefined_lambda and retained in badfun/badarity reasons. Identity/environment equality is separate from reference identity: HOST controls should require the exact original FunctionTerm passed into dispatch.

Factory retrieval is under the function-entry gate; invoke a retrieved factory outside all gates. Matching environment count is required. Existing compatible factory closures then use normal SourceFunctionInvocation admission. For stale admitted closures, extend SourceFunctionAdmission with an explicit discriminant: Admitted versus MissingCurrentLocalFunction; a missing result owns no lease. Wrong-current code remains badfun; pending purge still waits/retries through owner mailbox and rechecks after settlement. SourceFunctionInvocation.Named/Anonymous consume this result before binding clauses or invoking Execution, dispatching only the missing result. Never catch general undef or replay an already entered closure body.

A small registry-owned InvokeUndefinedLambda helper performs the final no-CURRENT decision and captures the selected handler export/body identity under the registry gate. It must directly lookup undefined_lambda/3, reuse327 normal source/non-source admission and actual selected-handler frame policy, and invoke only after releasing every gate. No HasExport→publicCall race; no fallback to undefined_function for an absent undefined_lambda. Returned arbitrary term is the fun-call result. Callbacks can legitimately load code and retry the same fun: reentrant public invocation is allowed; no global suppression. Missing callback retains undef, with native first-frame projection verified separately before claiming exact trace shape.

No-CURRENT is checked using loadedModules/current-code state, including a zero-export image, not exports or known historical module names. The defining module decision and selected callback lookup should share the registry lock's decision snapshot. Concurrent compatible loading/factory registration needs an explicit retry disposition, not an unconditional stale badfun: an executing-runtime exact factory may have appeared after the initial function-table snapshot. Keep registry and function-entry gates unnested or establish and document one real lock order; never invoke user factories under either gate. A monotonic function-table version plus retry when the relevant snapshot changes is a possible bounded implementation, but must be reviewed before code. A SOURCE-only design does not prove race parity.

## Minimum production surface and scope limits

Expected mandatory existing paths:
1. RuntimeFunctionEntries.cs — deferred executing-runtime resolution, original-Fun preservation; no immediate Restore factory invocation.
2. ModuleRegistry.cs — typed source admission at retired/current decision; preserve normal pending wait/recheck and admitted lease path.
3. SourceFunctionAdmission.cs — add explicit missing disposition without confusing null image with missing; preserve existing constructor compatibility where practical.
4. Compiler/SourceFunctionInvocation.cs — handle missing before execution; preserve old named/anonymous success and exact lease disposal.
5. ProcessErrorHandlerNames.cs (from327) — semantic undefined_lambda/3 callback name/arity.
6. ModuleRegistry.ErrorHandler.cs (from327) — reuse reviewed generic selected callback invocation/provenance; no duplicate capture implementation.
New narrow files: SourceFunctionAdmissionKind.cs and ModuleRegistry.LocalFunctionDispatch.cs. This is a proposed eight-path ceiling, subject to the actual327 final helper shape and race-resolution needs. A function-table version may stay within RuntimeFunctionEntries; no arbitrary production path expansion is preapproved.

Do not implicitly expand this slice to fresh-runtime compatible BEAM/source lambda relinking. Registering a source module does not necessarily install all local-fun factories: current factories are registered when named references/fun expressions execute. If a fresh runtime has compatible CURRENT code but no factory, a complete native relink would require a compiler resolver from local identity/index/environment or eager registration. That is a real additional feature, not justified by equal module fingerprint alone. A first slice may retain compatible *pre-registered* factory relinking and mark the missing eager resolver partial. If native comparison requires fresh-runtime compatible relink, approve its concrete extra compiler scope before implementation.

Direct transfer of a captured source FunctionTerm between unrelated HOST runtimes is also distinct from transferring a restored wrapper. Existing captured image has no registry identity and may be accepted into a foreign purge table; do not claim this arbitrary embedding path fixed unless image ownership is explicitly validated. Required cross-runtime test here transfers a restored wrapper and invokes it in another real ProcessContext, proving BOTH factory and module/handler authority come from the executing runtime.

## Discriminating test plan

A. Compile a genuine module exporting maker with captured environment. Create real fun and ETF in runtime A, actually delete/purge defining code, decode in fresh runtime B with no matching factory/current module, install B handler and invoke from B. Assert callback receives defining atom, original exact restored Fun, ordered Args, same self/dictionary and free variables; arbitrary handler return is actual result. Verify no callback at decode/Restore. No fabricated local identity or private field edits.

B. Same-runtime create/ETF/purge/restore must now reach the missing callback despite a retained factory. Bind actual public purge completion and absence of CURRENT module; prove stale source-admission missing route separately from wrapper no-factory route. A fun retained without delete/purge executes its real body; Delete without retirement still runs valid old code and must not call handler.

C. Decode wrapper using A with a matching factory, invoke in B with no factory/module and a different handler: B callback fires, A factory/body/handler marker stays absent. Conversely B pre-registered compatible factory must execute B admitted code. No decoding-time Factory shortcut can mask these tests.

D. Wrong arity returns badarity containing original Fun/Args before entry/module/handler lookup. A CURRENT incompatible module gives badfun(originalFun) with no callback. PublishModule zero-export current image must also give badfun; known historical or failed-on_load-only module must not act as CURRENT. Wrong environment plus current code stays badfun, not handler.

E. Pending purge: before settlement callback marker absent. Abort resumes valid old body; successful retirement with no CURRENT invokes callback only after settlement. Another CURRENT image means badfun. Kill during wait must unwind real owner mailbox wait and leases; no callback, no admission replay.

F. Callback immediate/delayed return, throw/error/exit, explicit Nil/custom trace, replacement between calls, nested retry/load, actual source owner/image lease and actor isolation. Missing callback gives undef without invoking an installed undefined_function callback. A valid local closure body that raises undef never invokes undefined_lambda. Preserve327 provenance guarantees and ordinary error/2/3 projections.

G. Historical RED uses the SAME public helper/fixture against published baseline: genuine unresolved restored fun invokes custom handler expectation but actually raises undef; no compile/reflection/missing-import error accepted. Original paired fixtures compile real module bytes and use code lifecycle; no historical hardcoded ETF is relabelled newly compiled evidence.

Next approval should explicitly decide whether this eight-path first slice excludes the fresh-compatible-module-with-no-factory resolver. Do not claim all local fun restoration paths complete until that resolver and direct foreign captured-image ownership have independent contracts and execution.
