# process_info/1 supported logical subset candidate

Source-only overlay; no build, tests, native execution, Main edit, Git mutation or workflow dispatch. This is a deliberately bounded API increment, not complete pinned default/resource compatibility.

## Production behavior

ProcessInfoRegistrations keeps the entire existing /2 registration expression unchanged apart from indentation and appends a call to the new ProcessInfoDefaultRegistrations. The new /1 delegates to QueryDefaultLogicalProcessInfo, already implemented and covered by archived Part281 HOST evidence. It performs one coherent logical snapshot, retains local/foreign/stale/dead PID handling, checks cancellation and omits registered_name for unregistered processes. It returns the actual default list or undefined, not an internal diagnostic projection object.

Thirteen logical keys are supported when registered; twelve when unregistered: registered_name,current_function,initial_call,status,message_queue_len,links,dictionary,trap_exit,error_handler,priority,group_leader,reductions,suspending. The four exact-pinned native resource keys remain absent: total_heap_size,heap_size,stack_size,garbage_collection. process_info/2 still rejects these unsupported resource items; this overlay does not change its parser, owner snapshot or switch. No CLR GC totals, fake zeros or frame counts are substituted.

Pinned erlang.erl5535-5540 expressly allows default tuple selection/order to vary and5570 requires a local process. Its debugging-only description permits exposing this supported subset honestly. Pinned erl_bif_info.c1007-1025 still implements seventeen fields; readiness must continue to identify the four-field divergence and the incomplete physical model. The existing internal property UnavailableMandatoryFields is retained for compatibility, not treated as a stronger public guarantee than OTP documentation.

## Tests and integration

The new HOST exercises public /1, registered omission/inclusion, all four deliberate omissions, unchanged /2 resource badarg, fixed-local PID, dynamic foreign owner, stale/remote/non-PID and dead target paths. It is source only. SDK-only omission/rejection assertions are explicitly not comparable native expectations.

Eight new differential source cases compare common typed field presence/types, registered-name semantics, stable /2 projections and PID domain. They do not compare exact reductions, all default key order, timing-dependent status values or native physical numbers. Original's additional four fields are intentionally outside the shared projection, predeclared in integration-proposal.json. No old corpus row or exclusion is replaced. The older strict seventeen-key source templates are not appended as passing SDK cases.

Root integration proposal: apply overlay in an isolated copy of actual Main; add ProcessInfoDefaultSubsetHostChecks.Run under its named HOST row; append ProcessInfoDefaultSubsetCases.Modules after historical compiled cases; run the actual target/focus and original comparison before claiming their results. Regenerate supported-mfas and readiness from the validated runtime; expected presence delta is +1, unchanged reference catalog. Every generated inventory/readiness row must say supported default subset rather than full seventeen-field compatibility. No test Program, central differential list or measured report is silently replaced by this overlay.

## Evidence boundaries

../process-info-default-completion-audit1 binds exact pinned sources and current source hashes, rechecks the archived Part281 HOST row and eight source files, and enumerates genuine resource blockers. Those are historical source/log facts, not fresh execution of this new public API. Completing exact native heap/stack/GC accounting needs a real process-owned runtime model and all writer/lifetime routes, beyond a registration wrapper.
