# proc_lib process support

Part 340 implements all 39 registered proc_lib exports, including six node-qualified spawn/spawn_link/spawn_opt overloads and real remote process labels through rpc/erpc. Actual validation passes 7193/7193 tests and 11 integration/package outcomes after the recorded failed launches. Retain the initial Windows PowerShell preflight exit 1, the second build failure caused by an omitted immutable build/Erlang.Net.targets input, and the third result of 7191 passed/2 failed. The first two did not reach guest tests; all 88 new tests passed in the third. Two historical negative protocol fixtures are explicitly migrated because their previously unsupported spawn/alias packets are now implemented, preserving malformed-frame and missing-feature refusals. The corrected pipeline uses PowerShell 7 and the complete tracked input set outside documentation. All 7105 previous ordered test names and 576 registry rows/custom Limits remain exact. Registry: 587 APIs across 14 modules; proc_lib 39/39, rpc 2/35, erpc 3/33; erlang 351 unchanged. Connected spawn/TT/link/monitor/priority/alias33/34, visible/hidden/current-generation nodes and apply/3 native1023-argument traversal are implemented. The new 44 portable cases, 11 direct contracts, 23 connected HOST checks and 11 RPC/lifecycle HOST checks pass. Original part 339 supplies 12 independently audited observations, with a precise contract crosswalk and explicit unmatched whole-trace fields. Published part 338 original 6216/fd8 and CI 7105/11 plus part 339 CI 7105/11 are complete; the old predeclared map stdout-order strict refusal remains separate. Current part 340 CI/original comparison awaits publication. The receiver must still resolve anonymous/captured source funs when its matching module is loaded but the closure constructor has never run there; current connected fun tests use external functions. This concrete gap is next, with an independent real two-node original observer. API presence is not whole-module compatibility: inherited native tuning, physical heap/stack/scheduler values, broad logger/RPC exports, physical TCP interoperability and performance parity remain partial.

Part339 adds a standalone original-only connected-spawn collector with12 fresh-VM controller scenarios: six plain/TT/monitor-option cases and six alias33/34 admission/send/lifecycle cases. Pinned source/installed-runtime/module/fixture provenance, exact reviewed source manifest, per-case raw ETF/packets and observed normal actor cleanup are mandatory. Independent source review and12 actual passive positive/refusal controls pass; original execution and SDK semantic comparison remain pending publication. No runtime, registry, fixture expectation or compatibility-count change:576 APIs across12 modules, proc_lib33/39;338 tests7105 and separate11 integrations remain the current local authority. Injected controller observations are not physical TCP interoperability. Continue six proc_lib node overloads and real RPC label queries; known full-module/physical/logger/performance gaps remain explicit.

Part338 implements proc_lib init_p/3,5, hibernate/3 and wake_up/3, shared initialization and real crash logger dependencies. Actual frozen-source 7105/7105 tests; a separate exact readiness/integration suffix completes11 outcomes after restoring one omitted immutable comparison report. The initial exit1 is preserved; no uninterrupted full-pass claim. All7017 previous ordered tests and540 registry rows remain exact. Registry576 across12 modules; proc_lib33/39. Add logger26, application4 and error_logger2 source-guided exports with real per-runtime configuration, guest callback delivery, standard streams and iterative term limiting. Local crash reports preserve data and field order but expose heap_size/stack_size as undefined because public CLR process_info deliberately lacks BEAM physical metrics. No physical values are fabricated; the two explicit platform alternatives are recorded. Published337 CI7017/11 and separate original6171/fd8 are independently complete, including all prior6114 ordered contracts and57 new formatter cases. The known map stdout-order variance remains a separate strict refusal. Six node proc_lib overloads, remote RPC label queries, full logger background/overload/configuration/formatting, physical scheduling/resources and performance parity remain unfinished. Current338 original verification awaits publication. API presence is not semantic completion.

Part337 delivers proc_lib format/1,2,3 and report_cb/2 with an independent76/76 original term comparison (57 reports including12 raw iolists,19 backend primitives). Actual full7017/7017 tests and11 integration/package outcomes pass; all6952 prior ordered names and536 registry rows/custom Limits remain exact. Registry540 across9 modules; proc_lib29/39, sys2/36, erlang351 unchanged. Original expectations come solely from independently captured pinned OTP returns. Both source-derived28 pretty assertions and actual callback/deep-continuation controls pass separately. Source37 new production files and exact package6 are bound. Published335 CI6952/11 and original6114/fd8 are independently complete, with the existing map stdout variance retained as a strict refusal. Published336 collector76 and CI6952/11 are verified. Ten proc_lib exports remain: initializer/hibernate/wake-up and six node overloads, with genuine crash logging and connected spawning still being implemented. Generic IO formatting, broad Unicode-version/configured printable-range, native physical/scheduler and performance equivalence remain partial. Current337 CI/enlarged ordinary oracle await publication; API presence is not whole-module completion.

Part336 publishes a standalone original formatter observation collector, not a runtime increment. Published334 original passes6085/6085 plusfd8 with all6042 previous ordered contracts and43 new metadata/label cases verified; one predeclared map stdout-order variance remains a separate strict refusal. Published335 remains the runtime baseline:6952 local tests/11 integrations,536 APIs in9 modules, proc_lib25/39 and sys2/36. Current335 CI/original are pending. The private formatter has57 SDK report observations,19 primitive observations,28 source-derived backend assertions and an actual callback-await control; no original formatter match or four new exports is claimed. Continue the real lifecycle/crash-report and connected-node dependencies.

Part335 adds proc_lib stop/1,3 and sys terminate/2,3 using the real system request/reply, aliases and DOWN protocol. Actual6952/6952 tests and eleven integration/package outcomes pass in one complete isolated pipeline; all6918 previous ordered names and532 registry rows/custom Limits remain exact. Registry536 across nine modules: proc_lib25/39, sys2, erlang351 unchanged. Focused29 compiled,4 direct and1 bare-runtime HOST checks pass. Gen_server/supervisor acknowledge before termination callbacks or child shutdown; the host stop helper consumes ACK and waits for DOWN under one budget. Published334 CI is verified6918/11; original334 remains running. Published333 five typed terminal scopes are complete6042/fd8/CI6871, with strict text refusals retained separately. Original335 and current-head CI await publication. Fourteen proc_lib exports and wider gen/sys/logger, connected-node, tuning, physical and performance contracts remain incomplete.

New stop/1,3 use sys termination rather than kill, preserve exit-wrapper and timeout boundaries, and wait for the real DOWN. Source-guided before/after ACK, missing/dead/self/named/via targets, late alias replies, actual gen_server/supervisor shutdown and bare C# host use are permanently tested. Original335 remains pending.

Part334 implements proc_lib initial_call/1, translate_initial_call/1, set_label/1 and get_label/1. Actual6918/6918 and eleven integration/package checks pass in one complete isolated pipeline; all6871 prior ordered tests and528 registry rows/custom Limits are preserved. Registry532 across eight modules; proc_lib23/39, erlang351 unchanged. Forty-three new compiled scenarios and four direct API checks pass. Published333 is fully audited: CI6871/11, original6042/6042 plusfd8, all6001 previous and41 new contracts match semantically; all five typed terminal scopes pass. One predeclared map stdout-order variance and the separate distribution strict representation refusal remain raw and explicit. Original334 and current-head CI await publication. API presence is not semantic completion. Remaining16 proc_lib exports cover lifecycle, node overloads and report formatting, with gen/sys/logger and connected-node dependencies still incomplete.

New APIs: initial_call/1 and translate_initial_call/1 query live PIDs, numeric PID tuples and source-shaped process-info lists. set_label/1 stores any term in the actual process dictionary; get_label/1 reads self or another process without inventing label inheritance. Remote RPC and nonlocal numeric PID channels remain explicitly incomplete. Local labels/introspection are validated by43 compiled scenarios plus4 direct contracts; original334 is pending.

Reference: [OTP29.1.1 proc_lib.erl](https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/lib/stdlib/src/proc_lib.erl).

Part333 registers19 of39 declarations. Status: **Partially compatible**.
Registry528 across eight modules; erlang351 unchanged. API presence does not
establish complete compatibility or a performance improvement.

| APIs | Implemented local contract |
|---|---|
| spawn/1,3; spawn_link/1,3 | Existing atomic creation, ancestry, caller/child guard boundaries and initial-call metadata |
| init_ack/1,2 | Ancestry or explicit parent; arbitrary acknowledgement return term |
| start/3,4,5; start_link/3,4,5 | Real monitor, optional atomic link, arbitrary ACK return, real DOWN after NACK, unlink-before-timeout kill and selective EXIT cleanup |
| start_monitor/3,4,5 | Returns {Ret,Ref}; ACK keeps the original monitor; failure/death/timeout requeues the actual consumed DOWN at the mailbox tail |
| spawn_opt/2,4 | Fun or MFA wrapper metadata and ancestry; existing option parser, atomic relations and PID or {PID,Ref} return |
| init_fail/2,3 | NACK before error/exit/throw or explicit-stack raise |

Startup's user monitor options are rejected through the source's two ordered
lists scans after MFA guards and before parent lookup. A monitor atom or two-tuple
raises badarg; a matching monitor tuple of another arity raises case_clause.
Options otherwise inherit the current runtime's link/monitor relation flags and
async_dist support. Public spawn_opt can use supported monitor tags/aliases;
startup's own monitor remains controlled by its protocol. Other valid native
heap/scheduler tuning options remain unsupported. This is not unrestricted OTP
spawn-option compatibility.

Creator ancestry uses its registered name or PID and retains list-shaped tails.
Native initial_call describes proc_lib:init_p, while '$initial_call' describes
the user call or source-defined gen translation. The shared receive route keeps
timeout expression evaluation before selection and defers value validation until
a genuine no-match wait. Existing SDK zero-timeout snapshot scheduling remains;
exact VM scheduling under continuous arrivals is unclaimed.

Actual6871/6871 and eleven integration/package checks pass in one isolated full
run. All6822 previous ordered tests and521 registry rows/custom Limits remain.
This slice adds41 compiled scenarios, seven direct API contracts and one HOST
group containing three deterministic real queued ACK/NACK/DOWN controls. Two
pre-full test-source syntax errors (reserved maybe atom and catch stack pattern)
were corrected without production changes or weakened assertions; failed receipts
remain preserved. Six tracked package resources were bound before validation.
Exact source bytes are promoted; current-head CI and original333 remain pending.

Published332 passes original6001/6001 plus fd8 and all five terminal typed scopes.
All39 added startup/receive scenarios have matching published source, local actual,
original and SDK observations; strict ordinary variance is zero. Historical
isolated packaging failure and successful suffix remain distinct from its later
uninterrupted CI pass. Distribution representation differences remain separately
recorded without normalization.

Remaining: remote overloads, unsupported tuning options, introspection/lifecycle
helpers, crash logging, complete stack/tracing parity and gen/sys system protocol.
See [priorities](DEPENDENCY_PRIORITIES.md) and
[full validation](validation/part-333/final-candidate/full-guard.json).
