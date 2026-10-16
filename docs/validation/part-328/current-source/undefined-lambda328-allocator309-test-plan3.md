# 328 high-value source test plan

Source sketches only; no compiled fixture or execution result. Use published327 as the RED production baseline. Root owns execution after the candidate source is frozen/reviewed. Keep callback selection, actual closure-body behavior, race controls and native observations distinct.

## Permanent HOST discriminators

1. Real named and anonymous compiler factories, each with actual ETF round-trip: fresh executing runtime has no target module/factory, custom handler returns a marker. Require the identical restored wrapper as callback argument2, exact defining atom, captured environment and ordered arguments. Assert decoding/Restore did not run any factory/body/callback. Repeat same-runtime after real delete plus completed purge to exercise retained stale factory admission. Historical327 should build with the same public helper and fail semantic undef, not a missing member/import.
2. Executing-runtime authority: decode in A where a matching factory exists, invoke that restored wrapper in B with a different handler and no target. Only B may select a factory, code image or handler. Also pre-register a genuine compatible B factory by running its actual maker before invocation: B's admitted body must run. Do not call fresh loaded-compatible/no-maker a supported relink. Separate direct captured foreign-image transfer from these restored-wrapper controls.
3. Relevant publication race, for named, source anonymous and interpreted module-null factories. Use genuine compiled/evaluated factory plus a test-owned origin-aware wrapper installed through the actual registration method via reflection, preserving its real invocation token. The wrapper calls the genuine factory, signals construction and waits outside all monitors. Publish a replacement for the same key while blocked; release and require old body/callback count0, replacement body count1, no acquired old lease. This gives a deterministic off-gate construction barrier without fabricated publication snapshots, private field writes or production test hooks. The module-null case must reach the actual evaluator path and perform its second validation, not merely a legacy custom factory preflight.
4. An unrelated-key publication while the same barrier is held must not reconstruct/retry the selected key. Hold Registry.gate, concurrently perform existing Functions.External(safe:true), then release; require both operations settle. Do not await the blocked writer while holding the gate. User factory/body/handler checks Monitor.IsEntered is false for both monitors on their execution thread. A bounded relevant mutator plus a real runtime kill demonstrates ReduceAsync signal/cancellation progress, settled worker and no leaked image lease.
5. Pending source purge: no callback before settlement; abort runs valid old body; retire with no CURRENT dispatches callback; retire with another CURRENT gives badfun(original wrapper); killed waiter settles without callback. Active old body remains callable before retirement. Zero-export CURRENT must give badfun. Wrong arity must give badarity containing original wrapper and exact arguments before factory/handler effects.
6. Callback retry: custom handler installs/registers compatible target code, invokes its maker so a factory actually exists, and applies the same wrapper. Require a fresh token and one real body execution. A nested unrelated Retry must not be swallowed by an outer wrapper. Callback immediate/delayed return, throw/error/exit and explicit traces reuse327 selected-handler provenance controls. Missing undefined_lambda must not call a present undefined_function; a valid admitted body raising undef must not call either handler.

All race waits need bounded try/finally release and task settlement before runtime/image disposal. Exact counters can be test-local markers and actual leases, not global weak-registry deltas. No claim that a scheduling delay alone proves the publication point was reached.

## Native ordinary fixtures and setup boundary

The current CompiledModuleCase/GeneratedModuleCompiler/CompiledModuleExecution path compiles and registers one source module. A handler/driver cannot also be its purged target: its active run frame keeps old target code alive. Do not fabricate ETF identities, purge the executing driver and pretend it is missing, or label an unknown import refusal a semantic RED. Genuine paired missing-lambda tests need a distinct small target module and a surviving driver/handler module, with explicit same-source setup on both backends. Author that bounded setup separately or use a dedicated original observation until the ordinary runner has this capability; do not silently change the generic ordinary fixture ABI.

Target sketch (two real compiler producers):

```erlang
-module(lambda_target328).
-export([named/1, named_fun/0, anonymous_fun/1]).
named(X) -> {named_body, X}.
named_fun() -> fun named/1.
anonymous_fun(Env) -> fun(X) -> {anonymous_body, Env, X} end.
```

Surviving driver exports run/0 and undefined_lambda/3, saves/restores its prior error_handler in try/after, creates target fun F, obtains actual term_to_binary(F), deletes/purges target through real code lifecycle, restores F1, puts an expected-F marker, then applies F1. Callback checks Module=lambda_target328, Fun=:=F1 and Args=[argument], returning {handled,Module,Args}. Keep original raw outcome as well as projected pairable result. Capture identity and environment independently; Erlang fun equality cannot prove CLR ReferenceEquals, so that stronger requirement remains HOST-only.

Pairable cases: named and anonymous missing return; wrong-arity before callback; callback throw/error/exit; missing callback with an installed undefined_function trap; body-undef no interception; deleted but unpurged active-old body; incompatible CURRENT badfun; pending abort versus completed retirement. Fresh CURRENT/no registered SDK factory remains a declared capability limit, not an equality case. Zero-export CURRENT is useful HOST coverage, with native preparation requiring an actual separately compiled zero-export replacement. Pending-purge timing needs controlled native protocol and remains a separate original observation unless the ordinary setup can establish it deterministically.

Pinned facts reused from immutable audit7A084: beam_common.c1645-1728 and JIT equivalent enforce badarity first, pending-purge retry, CURRENT badfun and no-CURRENT direct undefined_lambda/3; kernel/error_handler.erl117-126 retries apply only after ensure_loaded. None of the sketches constitutes native execution or exact trace equality evidence.
