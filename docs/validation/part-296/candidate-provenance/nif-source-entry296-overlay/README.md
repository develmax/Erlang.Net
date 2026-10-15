# Source named-entry dispatch candidate

Source only, not built or executed by this worker. Apply on the Part 295 source baseline after verifying every replaced file hash in parent-source.json. Register `Test(Erlang.Tests.NifSourceEntryHostNames.Test, Erlang.Tests.NifSourceEntryHostChecks.Run);` additively; Program.cs is deliberately not overlaid.

The compiler checks the exact captured ModuleCodeImage after its source lease is attached and its function frame entered, before fallback clause selection. A ready binding owns a NifImageCall across the complete awaited body. Disposal and the existing call finally release admission once. No binding table is created for ordinary source calls: the ConditionalWeakTable.TryGetValue fast path creates neither a table nor a call object. Runtime grants internal access to Compiler for the actual dispatch integration; tests use reflection, without a test friend or public hook.

## Entry route ledger

| Route | Existing path to the hook | HOST evidence prepared |
| --- | --- | --- |
| Exported MFA | ModuleDefinition.Register delegate -> Execution.InvokeAsync -> InvokeFrame | Registry Call target/1 |
| Direct source entry | Execution.InvokeAsync -> exact AcquireSourceCode/Attach -> InvokeFrame | target/1 |
| Non-tail local call | Expr.Call -> nested InvokeAsync -> InvokeFrame | local/1 tuple construction |
| Local tail call | TailCall -> InvokeAsync loop -> InvokeFrame | tail/1 |
| Remote source tail call | SourceTailCall -> lease replacement -> InvokeFrame | remote/1 |
| Named local fun | NamedFunctionReferences -> SourceFunctionInvocation.Named -> AdmitSourceFunction -> InvokeAsync | named/1 |
| External fun | Runtime external function dispatch -> registered source delegate -> InvokeAsync | external/1 |
| Anonymous fun calling named entry | SourceFunctionInvocation.Anonymous -> InvokeAnonymousAsync -> TailCall transfer -> InvokeFrame | anonymous/1 |

Anonymous closure bodies themselves remain ordinary clauses; only their actual named-entry calls route to a named binding. The table is keyed by function and arity on the exact executable image, not by module name. HOSTs prepare old and current tables with different results and invoke both, exercise missing-entry fallback, and prove ordinary fallback leaves its image unattached.

Owned-drain HOSTs block inside the fake body, retire its exact table, and require cleanup to remain pending until real body unwind. The cancellation case sends an actual kill signal, observes the body finally, holds that finally behind a barrier, and requires retirement to stay pending until release. Every actor/barrier/retirement wait has a five-second deadline; releases run in finally. Stub target(no) does not match test arguments, proving the dispatch precedes fallback clause selection.

## Scope and remaining integration

These are fake internal managed bindings. No original OTP or native parity claim is made. This does not expose load_nif, create a native ABI, change feature inventory, integrate BEAM function entry, or attach table retirement to purge/on_load/runtime shutdown. The separate coordinator overlay may be composed because this candidate does not touch ProcessContext, code-modification admission, or on_load source. A deliberate manually retired table rejects admission through the existing lifetime primitive.
