# Dependency-directed API implementation

Part338 — proc_lib33/39: prioritize six real node spawning overloads and their connected spawn/TT/monitor/link/alias dependencies, followed by actual erpc/rpc label queries. Broader logger overload handling and physical/runtime equivalence remain separate measured work.

Part337 — proc_lib29/39 now has original-verified report formatting. Prioritize the real logger/error_logger/application-environment dependencies and shared initializer/crash-report path, then connected spawn protocol/ownership and six node overloads. Test real delivery and resource/order boundaries; retain unsupported native tuning/physical/performance scope explicitly.

Part336 — Capture original57 report and19 primitive results first; compare with private SDK observations, fix actual differences and only then publish four report exports. In parallel complete initializer/hibernate with real crash-report/logger dependencies; six node overloads also require actual connected spawning.

Reference: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
The starting inventory is part329: 509 registered MFAs across seven modules.
Registration measures API presence, not semantic completion. Explicit reference
exports include internal behaviour callbacks; implement coherent contracts rather
than treating every missing export as an equally valuable user operation.

## Dependency groups

An arrow means that the module on the left uses the module on the right.
These are selected source-level dependencies, not a complete runtime call graph.
Dynamic calls, conditional compilation, generated modules and error-only paths
need separate review for each implementation slice.

```text
lists, maps, records -> erlang and selected runtime internals
binary, unicode, collections -> core terms, lists, maps and runtime primitives
gen <-> proc_lib <-> sys
gen_server -> gen, proc_lib, sys, lists, maps, diagnostics
supervisor -> gen_server, sys, lists, maps, diagnostics
gen_statem, gen_event -> gen, proc_lib, sys, diagnostics
io/io_lib <-> string, unicode, binary and selected scanner/file services
logger -> gen_server, supervisor, ets, formatting and handler infrastructure
application_controller -> gen_server, ets, code, file, filename, parsing
application_master -> proc_lib, gen_server, ets, timer
code_server -> runtime loading, file/loader services, ets
rpc/erpc, global, net_kernel -> local OTP foundation and connected distribution
```

Cycles require a minimal functioning group followed by additional features.
Formatting, logging and file services should supply the contracts needed by the
current group first; completing every export of those modules is not a
prerequisite for starting local behaviours. Likewise, ETS table primitives must
precede its optional query, traversal and persistence dependencies.

## Implementation order

| Priority | Group | First acceptance scope | Enables |
|---|---|---|---|
| 0 | Existing runtime/compiler repair | Finish part329 recursion and cancellation evidence against the pinned reference | Reliable source-function execution and ownership cleanup |
| 1 | proc_lib, gen, sys | Local process startup, ancestry, initial call, acknowledgement/failure; system messages, state inspection and suspend/resume | A reusable OTP process foundation |
| 2 | gen_server, supervisor | Local request/response families, monitored startup, enter_loop, sys integration and actual supervisor callbacks | Supervised application services |
| 3 | binary, unicode, string, proplists, queue, collection modules | Contracts actually called by the next source module, then complete families | Shared standard-library dependencies |
| 4 | io_lib, io, filename, file, ets | Formatting/encoding, basic IO protocol, files, table ownership and owner death | Diagnostics, configuration and shared state |
| 5 | gen_statem, gen_event, timer | Local behaviour protocols and lifecycle | State machines, event services and timer services |
| 6 | logger, code, application and their supporting modules | Coherent boot, loading, dependency startup and shutdown | Whole OTP application execution |
| 7 | inet, gen_tcp, net_kernel, global, rpc/erpc | Real transport, connection lifecycle, remote operations and failures | Distributed applications |

Priorities3 and4 can supply narrowly required contracts during priorities1 and2.
Local behaviour work must preserve existing distributed-runtime regressions;
this ordering does not discard or claim completion of the current network work.

## First API slice

Implement local `proc_lib:spawn/1,3`, `spawn_link/1,3` and `init_ack/1,2`,
then `start/3,4`, `start_link/3,4` and `init_fail/2,3`. Review `/5` options and
`start_monitor` separately: successful monitored startup retains its monitor,
whereas ordinary startup removes it. `init_ack` acknowledges an arbitrary term;
the existing gen_server helper's `{ok,_}` classification cannot be reused as the
public proc_lib contract unchanged.

Retain proper ancestry, registered parent identity, child execution/failure
boundaries, initial-call metadata, atomic links and selective mailbox behavior.
Reuse existing CLR process and registry machinery after auditing its observable
behavior. Crash reporting, remote overloads and unsupported options must remain
explicit limitations until implemented and compared.

Each completed slice needs direct registered-MFA cases, meaningful error and
lifecycle cases, a scenario involving its dependent module, original-OTP
comparison, current readiness data and an English changelog entry. Test count
and export count alone are not acceptance criteria. No performance improvement
is claimed without comparable measurements.

Part331 checkpoint: the first six local proc_lib APIs pass Main6775/11 and remain pending original331 observations. Synchronous startup is the next slice. The bounded329 recursion/cancellation reference gates are now complete; this is not whole-runtime completion.

Part332 checkpoint:12 local proc_lib APIs are present after adding synchronous start/start_link and init_fail. Actual6822 tests and eleven cumulative integration/package outcomes pass; original332 remains pending. Continue monitored/option-bearing startup, then gen/sys. Presence is not complete compatibility.

Part333 checkpoint:19/39 proc_lib APIs, including the local monitored and option-bearing startup family. Actual6871/11 local validation; original333 pending. Priority now moves to minimal gen/sys protocols with required proc_lib introspection/lifecycle support. Native tuning options and remote overloads remain explicit gaps.

Part334:23/39 proc_lib APIs and real process labels; actual6918/11. Finish cooperative sys stop and lifecycle/crash reporting, node overloads and exact report formatting. Source-only drafts are unregistered; full proc_lib remains incomplete.

Part335:proc_lib25/39 and sys terminate2/3 delivered;6952/11 local. Continue report formatting and crash logging, initializer/hibernate and node overloads. Full gen/sys and connected spawning remain dependencies; raw termination is not a substitute for their protocols.
