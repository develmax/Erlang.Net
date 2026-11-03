# Part541 — four full-CI regressions and nonblocking physical retirement

## Baseline and corrections

Authenticated CI40898640629 at009658617513ed4e0def79e9f739df4b705d2ad6 failed9425/9429 unique tests; both Linux jobs passed. All four failing cases were reproduced together locally on the unchanged production implementation. Independent dictionary108-byte, match-spec210-byte, ETS498 and monitor10-row original comparisons in that failed run still pass; they do not turn the whole run green.

- Update both private Terminate fixture calls to supply the two existing optional payload arguments, preserving the collector prestart/death assertions.
- Restore existing graceful shutdown DOWN callbacks. The former fresh-node predicate suppressed them after disposed became true. Terminal delivery now retains exact-runtime/table membership, halt, cancellation, Done and Terminated checks while allowing an existing recipient during graceful close.
- Derive mailbox ownership only from actual retained graphs of this same owner, still in its active list with positive references. Register the child before releasing the parent. Fresh/generic admission and the permanent drained task stay closed. Source652 proves target/observer/sentinel table order, nonempty tag custody, actual DOWN callback, all three cancellations/completions and native atomics retirement despite a live CLR reference. It checks foreign anchors, stale references and fresh/generic post-close refusals. The initial guard-only probe was insufficient because the old guard hid required callbacks; restoring callbacks with old593 admission is a meaningful ObjectDisposedException RED.
- Decode deferred monitor reasons before monitor lookup, including custom factory effects, then drop an absent reference before detached copying. The final lookup/removal and admitted native custody remain unchanged. Pinned OTP ad05823719d77c8faee87348ea39513d4e2f99c5, erts/emulator/beam/erl_proc_sig_queue.c6383–6416, establishes this order. Matched opaque-closure support is not broadened by this correction.
- Start core shutdown and request every remaining actual cancellation outside the runtime monitor before awaiting core/physical drains. Preserve callback/core exceptions. Source669 holds two real Source-child input641 records and checks that an exact cancellation callback failure still permits actual child completion, unwind and641 removal. An isolated current test host with the preserved older Runtime.dll times out as expected; public-close recovery remains cleanup, not a passing old result.

## Queue starvation

Repeated regex failures were retirement failures: all96 complete values/stacks/raw bytes matched. A low-overhead diagnostic captured no active regex invocations,74 exits waiting on physical completion,514 ended/ready records among approximately576 live641 records, and73 child-bridge dependencies on null-context producers whose completion was still pending. Claim641 queues the bridge before the original producer closes; synchronous dependency waits can occupy workers needed by producer cleanup.

Execute now awaits both the real dependency and actual asynchronous payload disposal. It retains one CAS, own-VM atom scope, callback-context isolation, all error aggregation and actual Done/Finished. A forced asynchronous boundary prevents a Join fallback from executing cleanup inline under a caller monitor. Three component variants check success, dependency plus asynchronous-release faults, canceled dependencies and absence of early Done.

The same140-case Release prefix passed in13.391 seconds after the fix; an uninstrumented old-prefix attempt failed in23.484 seconds. This single observation is not a general benchmark. The separate53.016-second instrumented failure, earlier observer attempts and the initial Debug retry remain historical evidence, not final acceptance.

## Final evidence

The final20 C# sources pass exact Roslyn token-kind/text equality, including literals, after formatting. Two new fixture LF preparations are recorded separately from formatting. All temporary diagnostic hooks and classes were restored or removed.

Debug378/378 and Release378/378 each pass as explicit disjoint140+140+19+38+33+8 composites on unchanged per-profile assemblies, with no final retries. Both final TestAnalysis processes exit normally (7.688/7.781 seconds). Fresh execution of pinned original OTP matches all2,463 complete TestAnalysis outcomes/stacks in each profile. Fresh compile/execute of monitor645 matches all10 complete raw outcomes in EACH profile; managed cleanup covers atomics, counters, regex, binary and the explicitly named ordinary_reference_control. That control does not prove original native NIF physical layout or lifetime. No frame projection, outcome rewriting or whole-API promotion.

All8 source gates and12 early checks pass4,000 exact members and2,220 guards. The [new89-row descriptor](../part-541/current-source-freeze-final20.json) preserves historical72D89, E663 and296D descriptors. Final raw worktree/index C# bytes are checked separately before publication.

[Summary](summary.json), [archive receipt](archive.json), [raw evidence](evidence.zip):662 verified members,7,601,534 bytes, SHA2563C7B1E5F06D1196A15CE5EA3388173609EDF258B9F56022AE93D5A7BA5147B38. The archive retains authenticated failed-CI report/provenance, causal REDs, scheduler diagnostics, older-runtime control, source/style evidence and final captures.

Expected catalog9,432; local full-suite execution is not claimed. Counts708 registered/180 verified/504 pending/24 known differences, API/full-contract deltas0/0. Exact publication-head full CI, current review, merge and latest-main CI remain separate gates. Exceptional InitializationEnded cancellation without host Dispose and broader derived process_info/remote contracts are not closed here.
