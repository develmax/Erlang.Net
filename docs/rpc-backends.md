# rpc backends

The complete OTP-29.1.1 `rpc` library has an opt-in C# implementation in
`RpcLibrary`, beside the retained original `rpc.erl` and `rpc.beam`. Both expose
the same 35 explicit exports. `module_info` is runtime metadata and is outside
this library-export count. No original rpc body executes the C# implementation.

The pinned reference is OTP-29.1.1, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. Original Ericsson Apache-2.0 notices
are retained. There is no new package or external ERTS dependency in production.

## Selection and dependencies

Select the implementation at startup through the existing library selection API:

```csharp
RuntimeModuleSelection.Managed("rpc")
RuntimeModuleSelection.Beam("rpc", originalRpcBytes, dependencies)
```

The equivalent MSBuild/manifest backend values are `Managed` and `Beam`.
BEAM manifests require the exact image SHA-256. Selection diagnostics report
backend, assembly and image ownership. There is no automatic BEAM-to-C# fallback
or live rex state migration.

`rpc` composes ordinary dependencies, as upstream does: `erpc`, `gen_server`,
`lists`, `maps`, `ets`, `net_kernel`, `persistent_term` and `unicode`, plus their
runtime primitives and transitive support modules. These must be available in
the application. Selecting rpc alone does not install or boot an entire kernel
release. In particular, the old unselected erpc subset cannot supply every
asynchronous or multicall operation. Supply a complete erpc implementation.

The focused runner explicitly publishes original dependencies and checks every
rpc export's selected owner; it does not replace rex, erpc, gen_server, the node
monitor, ETS or Unicode conversion with test stubs. See its `Program.cs` for a
library-only composition. The managed entry is `Erlang.ManagedKernelRegistration`;
the older unselected partial registry is not the complete opt-in backend.

## Implementation scope

All public calls, casts, request keys/yields, multi-node calls, server protocols,
broadcasts, parallel evaluation, pmap and pinfo have C# bodies. The C# library
also owns rex startup/callbacks, spawned monitored calls, serialized block calls,
finite-timeout middlemen, node feature observation/restart, the early-user proxy
and C-node stdout. Stored-function recipes own transferred free environments.

The port preserves upstream distinctions: block_call does not rpcify a normally
returned EXIT tuple; nb_yield timeout keeps the request; multicall preserves node
order and duplicate nodes; broadcasts skip non-atom nodes where upstream does;
improper-list guards follow Erlang cons semantics. The C-node Unicode failure
branch deliberately returns the upstream atom `state`, including the resulting
badrecord on a later output request. Full source frames are retained for these
errors and monitored replies.

## Validation and remaining acceptance

[Publication evidence](validation/rpc-pr-20261106/README.md) and the [review follow-up](validation/rpc-pr-20261106/review/README.md) record build, original
OTP comparisons, backtrace profiles and two-runtime distribution-packet checks.
Implementation preceded verification. Only rpc and connected dependencies were
checked; the full regression and canonical readiness regeneration were not run.

The observations compare values, complete reasons/stacks and protocol effects.
Opaque async keys are checked as two distinct references and consumed through
yield. The captured eval_everywhere callback is compared by module, name, arity
and free environment; implementation-specific code identity is not equated.
Only the observer's own PID inside a C-node failure-state record is renamed,
preserving its relationship to the caller. No stack frames or error metadata are
discarded. Lifecycle checks await DOWN; successful short calls use a generous
deadline instead of requiring identical scheduler latency.

Finite Windows acceptance does not certify universal 100% OTP equivalence.
Remaining gates include the complete peer/old-release/EI upstream suite,
external ERTS transport interoperability, all scheduler/resource/failure races,
physical hibernation/heap behavior, other platforms, function/module metadata
identity, whole-release startup manifests and global regression/readiness.
