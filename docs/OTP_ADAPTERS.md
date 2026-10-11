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

This is a selected adapter increment, not full OTP compatibility. Unsupported or unverified boundaries include nonempty start options, global/via/distributed names, full proc_lib/sys/debug/aliases/code upgrades, true hibernation, large timeout values, original source/stack frames and startup/parent/dead-child/shutdown races. The selected dynamic APIs support ordinary one_for_one/one_for_all/rest_for_one supervisors. Simple_one_for_one, significant/auto_shutdown and complete supervisor clock/intensity/race edges need further work. A valid original OTP input outside these boundaries can produce a different result or an error here.

At implementation `2fd52f8`, 37 independently expected compiled Erlang modules add 18 gen_server and 19 supervisor scenarios. Exact-source OTP-29.1.1 comparison passes 1293/1293 across the whole supported corpus; same-head CI passes 1482/1482 and the integration/package pipeline. [Raw provenance](validation/part-086/remote-checkpoint.json), [contract matrix](validation/part-086/contract-matrix.json), [module readiness](MODULE_READINESS.md). These are selected-contract checks; full upstream suites and performance equivalence are unverified.

Dynamic child operations run inside the supervisor mailbox. `start_child/2` validates before duplicate lookup and preserves `{ok,Pid,Extra}`. An existing active ID returns `{error,{already_started,Pid}}`; an inactive ID returns `{error,already_present}`. `terminate_child/2` waits for shutdown completion, keeps permanent/transient specs as undefined and removes temporary specs. `restart_child/2` starts an inactive spec and preserves it on failure; `delete_child/2` only removes inactive specs. Missing IDs return `{error,not_found}`; restart/delete of active children returns `{error,running}`. Ignored temporary starts store no spec. New children appear first in query order and enter the existing group restart schedule. Supervisor calls use infinity and gen_server call error context. Returned dead/nonlinked PIDs and full signal races remain unverified limitations.

```erlang
{ok,Sup} = supervisor:start_link(my_supervisor, []),
{ok,Child} = supervisor:start_child(Sup,
    #{id => worker, start => {my_worker,start_link,[]}}),
ok = supervisor:terminate_child(Sup, worker),
{ok,NewChild} = supervisor:restart_child(Sup, worker),
ok = supervisor:terminate_child(Sup, worker),
ok = supervisor:delete_child(Sup, worker).
```
Dynamic child implementation32c827a additionally verifies26 independently expected modules; supported corpus1319/1319 and same-head1512/full CI. [Dynamic proof](validation/part-088/remote-checkpoint.json), [26-case matrix](validation/part-088/contract-matrix.json). The37 static callback cases above remain unchanged historical part086 proof.

Automatic start failures now expose `restarting` in `which_children/1`, are excluded from active counts, and schedule a tagged mailbox retry. `restart_child/2` and `delete_child/2` return `{error,restarting}` while marked; duplicate `start_child/2` returns `{error,already_present}`. `terminate_child/2` clears the marker and cancels pending retry messages. `ignore` clears the marker, while every actual automatic attempt counts against restart intensity. Group strategies stop at the first failed start, then retry the whole group or failed suffix. [14 retry contracts](validation/part-092/contract-matrix.json) verified against original OTP at c20e990; [1333-case proof and same-head1526/full CI](validation/part-092/remote-checkpoint.json). This extends selected ordinary supervisors; full module compatibility remains partial.

Invalid shape/private-tag casts fail with function_clause, as pinned supervisor handle_cast has no fallback; only correctly tagged stale retries are ignored. Part089 wrong-tag expectation is superseded by the [source correction](validation/part-090/inspection.json). Full source stacks remain unverified.

Erlang start callbacks own links and may return an already terminated PID. The supervisor keeps that PID until an EXIT or explicit termination; count_children reports it inactive. Shutdown uses a matching monitor DOWN, then unlink/EXIT flush; no implicit linking is added by the Erlang adapter. Existing CLR host startup convenience is separate. [15 lifecycle contracts](validation/part-094/contract-matrix.json) verified against exact-source OTP at9ef47ed; [1348-case proof and same-head1541/full CI](validation/part-094/remote-checkpoint.json).

Significant children are supported for transient/temporary policies under any_significant/all_significant. Only unrestarted significant exits trigger auto shutdown; manual terminate/delete and ignore do not. Standalone check_childspecs/1 uses undefined policy and /2 takes an explicit policy; startup uses never by default. [32 contracts](validation/part-098/contract-matrix.json) verified against original at4992a98; [1380-case proof and1574/full same-head CI](validation/part-098/remote-checkpoint.json).

## Part 099 — Simple supervisor strategies and public queries (2026-10-11)

Implement `simple_one_for_one`: one validated template starts no initial process; `start_child` appends arguments, PID/restarting identities retain arguments across retry, and permanent/transient/temporary policies preserve source outcomes. Dynamic simple shutdown monitors/signals all children before waiting, with one shared timeout; ordinary ordered shutdown is preserved. Add `which_child/2`, `get_childspec/2`, `stop/1,3`; temporary ordinary dynamic specs erase stored start arguments as source `save_child` does. Shared callback normalization extracted from existing ErlangSupervisor. Source/reuse/limits are recorded in [audit](validation/part-099/reference-audit.json) and [dependency decisions](dependency-decisions.md). No new dependency or production BEAM; consolidated notices packaged and draft license excluded.

Local **1614/1614 and full pipeline**, **90 MFAs (+4)**, supervisor **14/23**, gen_server **10/41**, all partial API presence. **224 namespace types/no violations**. [35 independent modules](validation/part-099/contract-matrix.json), [preservation](validation/part-099/corpus-audit.json), [tests](validation/part-099/tests.json), [integration](validation/part-099/integration-results.json). All prior **778/523/79** sources/expectations/generated hashes unchanged; next exact-source corpus **1415=778/558/79**. New original comparison and same-head CI pending. Historical1380/1574 proof belongs to4992a98/part098. Isolated draft35/35 and124/124 probes are preparation evidence. Initial full run1613/1614 failed because enum enumeration reused the three-child ordinary host test for the new single-template strategy; preserve that failure and restrict the old test matrix to its three intended strategies, adding explicit host invalid-template rejection. Final run includes that new host contract,35 modules and4 direct-MFA contracts.

Full supervisor/sys/upgrades/logging/clocks/races/distribution/stacks/resources/upstream suites/performance remain unfinished. C# host simple configuration requires an Erlang MFA template; generic CLR argument factories are outside this increment. PID-map query order is unspecified and no BEAM ordering claim is made. Target10% remains active; continue collections/timers/source/application delivery after original verification, without percentage promotion from test counts.

Part100:35 simple strategy/query/stop modules verified against exact-source OTP at0d6a233;1415/1415 and1614/full same-headCI. Four selected strategies and significant policies work, full behaviour remains partial. [Proof](validation/part-100/remote-checkpoint.json).
