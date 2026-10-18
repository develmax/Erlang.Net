# Queue higher operations, source proposal

This package ports pinned OTP-29.1.1 `queue.erl` clauses for 13 MFAs. Twelve implementations are present; `member/2` delegates Root's authentic `QueueMemberExecution.Run(context,item,queue)`. The manifest contains 16 new C# files. No Main file, build output, CI execution, or registry ledger was modified by the proposal author. Nothing here asserts completed compatibility.

Root integration:

1. Copy the files in `copy-plan.json`, verifying each SHA-256. Register `QueueHigherModule.Register(r)` once alongside the core and ends registrations. The central public `QueueModuleNames` constants are prerequisites.
2. Before the generic per-MFA fallback in `tests/Erlang.Tests/Program.cs`, dispatch `QueueHigherMfaContracts.Owns(export.Module, export.Function, export.Arity)` to `await QueueHigherMfaContracts.Run(export.Function, export.Arity); return;`, following the existing core/ends pattern.
3. Add `Test(QueueHigherHostNames.CallbackOrder, QueueHigherHostChecks.CallbackOrderAndImproperEvaluation)` in the HOST block. This adds one HOST name. Thirteen direct MFA names arise from the existing registry loop, not an extra test runner.
4. Preserve Root's owner-aware `QueueCoreTrace` with tail-alias matching. Required shared overloads are Length/Append ownerArity, Reverse ownerArity/tail/includeOwner (defaults preserve old8). The rear-filter expansion passes `includeOwner:false`: its explicit tail call must retain genuine outer CP/public frames without inserting a current filter_r frame.
5. Run the final composed build, typed tests, original fixtures, full raw value/stack/source-identity comparison, cancellation controls, and affected/full regression. The separate original package has 892 values and 4460 full observations across all30 new APIs; test counts are not a completeness claim.

`branch-proof.json` records per-MFA clause/evaluation/error responsibilities. The higher code deliberately ports only the exact dependency paths needed from `lists:foldl`, `lists:all`, `lists:any`, and `lists:filtermap` rather than inheriting generic paths known to omit helper metadata. Rear fold delegates the existing exact shared `ListFoldRight` implementation.

Filters and deletion preserve physical queue shape and observable evaluation order, including malformed Cons tails, rear traversal before callback evaluation, first/last logical deletion, and the genuine `false` helper sentinel. The loops keep checkpoints and do not recurse on the CLR stack. Logical CP history is bounded by the runtime's supported stack limit; output reconstruction storage remains proportional to the actual result work.

Callback exceptions retain actual callback code identity and full source/caller fields. No captured native fun identity, native scheduling partition, or expected stack bytes are injected.
