# gen_statem sys continuation review repair

Address review4467799778. Replace the nested await from Message to the original
sys handler with an internal IOwnedControlTransfer506 carrier. A module driver
catches that carrier after old Message/Run/sys/system_continue invocations unwind,
then executes the next real sys call. Nested system_continue uses the existing
driver for the same ProcessContext. The carrier publishes its complete term
payload through existing owned invocation boundaries. No public OTP export changes.

Keep the original proc_lib exception handler outside the driver. Unwinding to the
process dispatcher instead was rejected: it dropped the required
proc_lib:init_p_do_apply/3 source frame in sys_error_once. The final implementation
does not change the shared runtime. Original Erlang/BEAM bytes are unchanged.

The permanent Managed sys_retention probe runs32 generations with get_state,
get_status, suspend and resume. It walks actual Task continuations from the idle
mailbox, excluding unrelated term/runtime/ExecutionContext graphs, and counts
reachable GenStatemMachine instances. The original library retains97; the repaired
one retains1 with the same runtime. A zero-machine observation is not accepted:
the probe waits for the successor to attach its mailbox awaiter. Clean stop must
invoke terminate exactly once. This is a host retention test, not an OTP corpus row.

Ordinary Release runner build: zero warnings/errors. Both explicitly selected
backends pass74/74 complete API observations and311/311 engine scenarios against
the unchanged admitted original OTP corpus, including full exception stacks and
sys_error_once. Full global regression/readiness and manual CI dispatch are unrun.
Final source/producer pins and their passive admission are saved separately;
required hosted final-head checks and review remain gates. No review thread is
resolved and no merge/package publication is performed.

Commands: ordinary Release build of tools/Validation/Erlang.GenStatemCompatibility
with `--nologo -m:1 -nr:false -v q`; invoke the DLL with repository, admitted corpus,
Managed or Beam; add Managed's fourth argument `sys_retention` for the retention
probe. The same new validator is also run in an isolated directory with the
original saved C# library and the same final runtime to reproduce the failure.
No product source is rewound for that comparison.

Retain rejected probes and original failures separately. Early weak-reference
native-fun payload probes also measured opaque dispatch/heap pins and are not
admitted as evidence about managed continuation reachability. A smaller graph
walk cap rejected the old unbounded chain before yielding a count; the bounded
100000-node probe yields the admitted97/1 results. Old overwritten report copies
from failed early attempts are not used in the acceptance summary.
