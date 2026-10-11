# Erlang behaviour adapters

Erlang callback modules execute through the native C# behaviour engines. No Erlang/BEAM installation is needed in production. `ModuleDefinition.Register(runtime.Modules)` installs selected OTP exports once and registers the module's exported callbacks. A host using `ModuleRegistry` directly can call `Erlang.Otp.OtpModules.Register(runtime.Modules)` explicitly. Existing `IGenServer` and `ChildSpec` C# APIs remain available.

| Module | Registered entry points | Selected contract |
| --- | --- | --- |
| gen_server | start/3,4; start_link/3,4 | Empty options list, PID result, optional `{local,Name}`, init ok/stop/error/ignore |
| gen_server | call/2,3; cast/2; reply/2 | Local PID/atom server names, callback state, monitor-based calls, deferred replies |
| gen_server | stop/1,3 | System terminate request, termination and monitor completion |
| supervisor | start_link/2,3 | Erlang init, optional local name, static children started by MFA |
| supervisor | which_children/1; count_children/1 | Serialized child snapshots, child type/module metadata |
| supervisor | check_childspecs/1 | Selected map/legacy child-spec validation |
| supervisor | start_child/2; terminate_child/2; restart_child/2; delete_child/2 | Serialized dynamic specs, shutdown completion, inactive restart/delete and source-shaped errors |

Callbacks run with the process's own `ProcessContext`, including its PID, dictionary, mailbox, links and monitors. The From tag is a managed monitor reference; OTP alias tag encoding and late-reply cleanup are unfinished. gen_server supports init/1, handle_call/3, handle_cast/2, handle_info/2, handle_continue/2 and terminate/2. Missing handle_info/2 preserves state; terminate/2 is optional. Init and ordinary callbacks treat thrown terms as callback returns; terminate exceptions remain failures. Integer timeout, infinity and `{continue,Term}` actions are supported within CLR timeout bounds. `hibernate` currently has idle-loop behaviour without BEAM memory reclamation semantics.

Supervisor init returns `{ok,{Flags,Children}}` or `ignore`. Flags can be a map or the legacy strategy/intensity/period tuple. Children can use map specs or the legacy six-element tuple. Child start applies the registered `{Module,Function,Arguments}` in supervisor context and accepts `{ok,Pid}`, `{ok,Pid,Extra}`, `ignore` or `{error,Reason}`. The native engine supports one_for_one, one_for_all and rest_for_one, permanent/transient/temporary policies and reverse shutdown; complete error/retry/race semantics remain partial. Queries return children in reverse specification order, retaining ignored non-temporary specs as undefined.

This is a selected adapter increment, not full OTP compatibility. Unsupported or unverified boundaries include nonempty start options, global/via/distributed names, full proc_lib/sys/debug/aliases/code upgrades, true hibernation, large timeout values, original source/stack frames and startup/parent/dead-child/shutdown races. The selected dynamic APIs support ordinary one_for_one/one_for_all/rest_for_one supervisors. Simple_one_for_one, significant/auto_shutdown, automatic restart retry/restarting states and complete supervisor intensity/race edges need further work. A valid original OTP input outside these boundaries can produce a different result or an error here.

At implementation `2fd52f8`, 37 independently expected compiled Erlang modules add 18 gen_server and 19 supervisor scenarios. Exact-source OTP-29.1.1 comparison passes 1293/1293 across the whole supported corpus; same-head CI passes 1482/1482 and the integration/package pipeline. [Raw provenance](validation/part-086/remote-checkpoint.json), [contract matrix](validation/part-086/contract-matrix.json), [module readiness](MODULE_READINESS.md). These are selected-contract checks; full upstream suites and performance equivalence are unverified.

Dynamic child operations run inside the supervisor mailbox. `start_child/2` validates before duplicate lookup and preserves `{ok,Pid,Extra}`. An existing active ID returns `{error,{already_started,Pid}}`; an inactive ID returns `{error,already_present}`. `terminate_child/2` waits for shutdown completion, keeps permanent/transient specs as undefined and removes temporary specs. `restart_child/2` starts an inactive spec and preserves it on failure; `delete_child/2` only removes inactive specs. Missing IDs return `{error,not_found}`; restart/delete of active children returns `{error,running}`. Ignored temporary starts store no spec. New children appear first in query order and enter the existing group restart schedule. Supervisor calls use infinity and gen_server call error context. Returned dead/nonlinked PIDs, automatic retry and full signal races remain unverified limitations.

```erlang
{ok,Sup} = supervisor:start_link(my_supervisor, []),
{ok,Child} = supervisor:start_child(Sup,
    #{id => worker, start => {my_worker,start_link,[]}}),
ok = supervisor:terminate_child(Sup, worker),
{ok,NewChild} = supervisor:restart_child(Sup, worker),
ok = supervisor:terminate_child(Sup, worker),
ok = supervisor:delete_child(Sup, worker).
```
