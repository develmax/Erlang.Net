# proc_lib local process foundation

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
