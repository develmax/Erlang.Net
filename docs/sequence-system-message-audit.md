# Sequential tokens on selected system replies

Pin: OTP-29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5, read with git show rather than the newer checkout.

## Actual receipt and sentinel

erts/emulator/beam/erl_message.c:483-500 documents and assigns am_undefined in the five-argument erts_queue_message; erl_message.h:525-526 distinguishes the explicit-token variant. emu/msg_instrs.tab:161-179 clears a recipient token for NIL, installs an actual token for other values, and leaves it unchanged for am_undefined. The distinction applies to actual selective consumption, not unmatched admission.

The managed producers below previously queued a null token with default non-preserving behavior, incorrectly clearing the token on matched receipt. The correction preserves the existing token and sequence clock without simulating an ordinary sequential send. The new internal ProcessRuntime.SendSystem performs the existing runtime gate/live-recipient lookup and SendTraced(message,null,preserve:true). Correlation, priority, pending admission, ordering and death fanouts remain in their existing paths.

| Pinned native producer | Producer evidence | Managed method |
| --- | --- | --- |
| notify_sys_task_executed | erl_process.c:10574; reply tuple and explicit am_undefined at 10618-10626; dead-target path 11489 uses the same function | ReplyGarbageCollection; ReplyCodeCheck |
| handle_process_info | erl_proc_sig_queue.c:5312; reply and explicit sentinel 5407-5413 | ReplyProcessInfo |
| handle_rpc | erl_proc_sig_queue.c:3564; reference/result and sentinel 3612-3616 | ReplySavedCallRequest |
| sync_suspend_reply | erl_proc_sig_queue.c:5480; sentinel and queue 5529-5530 | asynchronous suspension acknowledgement |
| erts_proc_sig_send_sync_suspend | erl_proc_sig_queue.c:3192-3241; missing-target five-argument queue with system origin | dead asynchronous suspension acknowledgement |
| bif_timer_timeout | erl_hl_timer.c:1146; five-argument clock-service expiry queue 1182-1183 | RuntimeTimers.Fire |
| send_async_info/send_sync_info/no_timer_result | erl_hl_timer.c:1992/2036/2209; clock-service queues 2026/2068/2229 | asynchronous read/cancel reply; managed direct synchronous returns unchanged |
| monitor-down conversion | erl_proc_sig_queue.c:5034 tuple; priority token 5040-5042 and ordinary token 5050-5052 explicitly am_undefined | MonitorDown retains SendPending, adds preserve:true |

Ordinary user sends, EXIT and sender-origin metadata are unchanged. Native ordinary-trace origins require a separate producer-specific composition: live task/info/RPC replies originate at the executing process, timers at clock_service, dead suspension at system, DOWN metadata depends on monitor subtype. Seq preservation must not suppress ordinary receive instrumentation; the process-trace integration owns an independent admission-trace flag.

## Twenty independent permanent modules

SequenceSystemMessageCases.Modules independently expects {ok,done} for each new module. Each preservation case sets label and advances serial to {0,1} before the operation, consumes the correlated reply, asserts the unchanged label/serial and a print advancing to {0,2}.

Cases: gc-sync-live, gc-async-live, gc-async-self, gc-sync-dead, gc-async-dead, info-owner-dictionary, info-owner-messages, code-check-sync, code-check-async, saved-calls-owner-rpc, suspend-async-live, suspend-async-dead, send-after-expiry, start-timer-expiry, read-timer-async-live, cancel-timer-async-live, read-timer-async-missing, monitor-down-process, monitor-down-noproc, ordinary-message-still-clears. The contrast consumes an actual ordinary message from an untraced child and asserts token removal while the local sequence clock persists. Preliminary child creation/death consumption occurs before the audited token is set.

No existing provider, outcome, common corpus catalog or functional catalog was edited by this handoff. Root owns appending the provider, direct functional linkage, guards and full/native validation. Historical 4434 modules/150 diagnoses and 6013 tests do not prove this change. Planned totals with all20 appended:4454 SDK modules,4448 native-comparable modules,6033 tests. No build/focus/style CLI/native oracle ran during this coordinated freeze. Exact before/after hashes are in artifacts/seq-system-change-manifest.json; original before hashes remain in artifacts/seq-system-owned-before.json.

## Remaining boundaries

Persistent time-offset monitor notification also carries am_undefined: erl_proc_sig_queue.c:2474-2477, erts_proc_sig_send_monitor_time_offset_msg:2495-2499; actual changing-clock producer erl_time_sup.c:2034-2107. Managed time-offset monitoring registers without an actual clock-change notification producer. This is an unfinished protocol rather than a confirmed received-message regression; no simulated notification fixture was added.

EXIT is not a blanket system-sentinel case. erts_proc_sig_send_exit in erl_proc_sig_queue.c:2515 accepts its producer token. An untraced EXIT can correctly clear the recipient; a traced EXIT needs the real producer token. No EXIT path or assertion was altered; explicit EXIT propagation needs separate source and native contracts. No general claim that all internal messages preserve tokens follows from this audit.
