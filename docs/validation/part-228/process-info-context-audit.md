# Part228 process-info observer audit

Original228 at published9f716f4 is FAILED/ABORTED:5399 planned,5200 executed,5195 passed and five mismatches;151 diagnostic cases were not reached. This is a read-only analysis of the two process-info mismatches. No original, Main, candidate, harness or production source was changed; no new build or test is claimed.

## Confirmed execution context

`tools/Erlang.Differential/CompiledModuleProtocol.cs:14` wraps the native module invocation with `{OracleRunner,OracleMonitor}=spawn_monitor(fun()->...apply(M,run,[])...end)`. OracleOwner is the evaluator process and its monitor remains installed while run/0 executes. It is removed only after receiving the tagged final outcome. `UntrappedFresh` chooses trap_exit=false; the enum does not mean unmonitored. Native root monitored_by therefore legitimately contains the oracle owner. The measured PID <nonode@nohost.10.0> is run-specific and must not be hardcoded or filtered.

`CompiledModuleExecution.Execute` uses managed Runtime.Spawn with no observer monitor. Its fresh root is unmonitored. Thus the two sources compare unequal environments when they assert monitored_by=[] on self(). Both managed fixtures returned {ok,done}; both native fixtures failed on the incoming oracle monitor. This evidence identifies a source/harness context defect, not a demonstrated process_info implementation defect.

## Pinned original semantics

Inspected read-only git objects at OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5:

- erts/emulator/beam/erl_bif_info.c: pi_args marks monitors and monitored_by for signal processing; ERTS_PI_IX_MONITORS enumerates origin monitor nodes; ERTS_PI_IX_MONITORED_BY enumerates target nodes through both target list and monitor tree. collect_one_target_monitor records mon->other.item (the actual observer PID) for process monitors. There is no special oracle observer filtering.
- erts/emulator/beam/bif.c: monitor local_process branch checks id==c_p->common.id and performs no self-monitor installation, while still returning the reference. A monitor of self does not remove or hide other incoming monitors.
- Managed ProcessRuntime.Monitor likewise returns the reserved reference without adding a monitor entry for self. ProcessRuntime.ProcessInfo includes every actual monitor whose target is the queried process, selecting Observer. These selected paths agree with source semantics; artificially dropping the oracle PID or returning an empty monitored_by list would be wrong.

## Exact failed evidence

### erlang-process-info-observable-monitors-empty

Source SHA256: 4D807A982E27C84020E8898180FD6305678B55ED1E87D1BAFF95EE81879AC94B
Generated C# SHA256: B4869CEC948711EB38F2A953E7A555FAB6BBBBF4202FD9835ED6D9C60E4637F0
Native: {error,error,{badmatch,{monitored_by,[<nonode@nohost.10.0>]}}}
Independent fixture: {ok,done}
Managed: {ok,done}

```erlang
-module(info_observable). -export([run/0]). run()->{monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(self(),monitored_by),done. worker(Parent)->put(kept,{value,[1,2]}),Parent!{ready,self()},receive finish->ok end. stack_probe()->S=process_info(self(),current_stacktrace),put(stack_marker,seen),S. wait_status(P,State,0)->error({no_state,P,State});wait_status(P,State,N)->case process_info(P,status) of {status,State}->ok;_->receive after 1->ok end,wait_status(P,State,N-1) end.
```

### erlang-process-info-observable-self-monitor

Source SHA256: 85C2D746143FD832BF363661C95ECE224D04F1320A1B3EFD69B1C86BE2248779
Generated C# SHA256: CCD7F1AED8347F85A2FD632A70629AC96B8A22784C6A5FA615FC86EB09ED49A1
Native: {error,error,{badmatch,{monitored_by,[<nonode@nohost.10.0>]}}}
Independent fixture: {ok,done}
Managed: {ok,done}

```erlang
-module(info_observable). -export([run/0]). run()->R=monitor(process,self()),{monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(self(),monitored_by),true=demonitor(R),done. worker(Parent)->put(kept,{value,[1,2]}),Parent!{ready,self()},receive finish->ok end. stack_probe()->S=process_info(self(),current_stacktrace),put(stack_marker,seen),S. wait_status(P,State,0)->error({no_state,P,State});wait_status(P,State,N)->case process_info(P,status) of {status,State}->ok;_->receive after 1->ok end,wait_status(P,State,N-1) end.
```

## Proposed independent source correction

Keep each existing independent {ok,done} outcome. Move only the monitor inspection into a fresh plain-spawned child. The outer oracle observer then monitors the outer runner, not the child. Do not use spawn_monitor to create that child and do not install a parent-to-child monitor until after the child reports that both assertions passed. Keep the child alive awaiting finish so post-assertion cleanup can install a genuine monitor and wait for normal DOWN. Forward caught assertion failures explicitly to the parent; timeouts remain failures. This creates a controlled original/managed context without filtering observations.

For monitors-empty, the replacement run body can be:

```erlang
S=self(),P=spawn(fun()->Outcome=try {monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(self(),monitored_by),done catch Class:Reason->{Class,Reason} end,S!{probe,self(),Outcome},receive finish->ok end end),receive {probe,P,done}->ok;{probe,P,Failure}->error({probe_failed,Failure}) after 5000->error(no_probe) end,M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok after 5000->error(no_down) end
```

For self-monitor, use the same outer child lifecycle, replacing the child try body with:

```erlang
R=monitor(process,self()),{monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(self(),monitored_by),true=demonitor(R),done
```

Both replacement modules must retain their old complete source and hashes in the explicit correction record, then record the new full source, source hash and generated C# hash. Keep their names, CallerContext, CompilerPolicy, streams and independent done outcomes unchanged. The old failures remain immutable. New success checks should also explicitly install a parent observer and assert {monitored_by,[S]} before/after self-monitor, followed by real demonitor and empty recovery. This proves the implementation still exposes a genuine observer instead of accidentally teaching it the old empty-list expectation.

A broader alternative is a separately specified SDK/native monitored fresh runner context, implemented equally in both harnesses. That affects all self-observation and system-message contracts and needs an explicit harness change audit; it should not be slipped into this repair. The narrow independent child fixtures avoid that expansion.

No diagnosis/exclusion is added or removed here. The other three sequential-trace mismatches and the native abort require their own pinned-source repairs. Passing new local fixtures cannot establish original228 completion; a new exact-head original run and all151 diagnostics remain required.

Evidence: artifacts/part-228-raw-mismatches.json and artifacts/part-228-differential/differential.json. The platform-composition helper remains source-prepared only; no helper or combined builds have started.
