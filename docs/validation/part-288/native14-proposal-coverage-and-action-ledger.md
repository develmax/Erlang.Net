# Native14 direct permanent HOST regression proposal

Status: SOURCE ONLY, NOT COMPILED OR EXECUTED. This is a two-file additive candidate; Main and Git were not changed. No original OTP dispatch was made.

## Evidence binding

All expected native facts below come from actual terminal runs on published Part287 58d227ecef29d117fa091ab30b1e0928f969d2fa, not from SDK-produced observations.

- Payload11 run38677155447/job116233258790: `artifacts/native-audit-terminal287-38677155447-independent-guard.json`, SHA256 97B08294FB80A5CC5E7D54727DEC42191BC19360CF499CE9DC70F829AF5F9CCB.
- Monitor3 run38677160804/job116233275919: `artifacts/native-audit-terminal287-38677160804-independent-guard.json`, SHA256 7D1B63310B1AC997CAF26DA2669BEFC57FE4B3F1E7D4492FFF0CAE9C3523EA84.
- Previous legacy13/18 actual receive-tracer evidence: `artifacts/native-audit-trace284-independent-guard.json`, SHA256 66AD7129C55E4C821FBB5A2A15D677DD45158349CC28D8F31AA1810F3FECA4B1.
- Current permanent coverage and observed direct gaps: `artifacts/native-audit-native14-main-concordance.json`, SHA256 CB5B636F5FEDACAF62D9CF3E4BE5AE9ACDEC6E6B119038485D3AFC27F28AA519.

## Additive checks

| Candidate method | Actual native case and required direct assertions |
| --- | --- |
| CheckCanonicalTrace(25) | linked_trace_trap: genuine incoming LINK; public process_info verifies it; canonical nonzero `{2,payload_label,9,Remote,3}` preserved until actual Receive selects EXIT; selected label and serial `{3,9}` and complete previous token asserted. |
| CheckCanonicalTrace(27) | explicit_trace_trap: same actual selected payload token, independent explicit exit path. |
| Both canonical cases | Real suspend/resume acknowledgement; no report while suspended and public label/serial/receive snapshot unchanged before selection. Actual receive-tracer event checked with complete EXIT payload. Native payload11 captured token, not tracer; tracer expectation is parameterized legacy13/18 actual receive semantics, not a claim that payload11 recorded a tracer. |
| CheckMalformedControllerDeath(24) | linked_live_truncated: link remains active through deferred decode; malformed 83 payload; real controller Completion killed plus real public-monitor DOWN killed; receiver remains alive. |
| CheckMalformedControllerDeath(26) | explicit_live_truncated: same actual controller death, separate ordinary explicit opcode. |
| CheckMalformedControllerDeath(28) | live_unknown_ref_malformed: no matching origin reference, actual live watcher, malformed 83ff; actual controller Completion/DOWN killed. |
| CheckTerminatedOriginWatcher | dead_watcher_malformed: real origin monitor19 is established and output inspected; watcher genuinely terminates normally; real Completion/DOWN normal and outgoing demonitor20 with exact reference observed; malformed28 afterward drops and actual controller survives. No fabricated response from the dead watcher, no interpretation of native barrier timeout as a message. |
| CheckMissingLinkedTarget | dead_link_truncated: malformed24 to genuinely absent local PID; actual controller remains alive; no output. Existing permanent tests continue covering the other lazy-drop cases. |
| CheckOrdinaryExplicitKill | explicit_kill: ordinary26 kill bypasses trap_exit; actual receiver Completion killed and public-monitor DOWN killed; controller survives. |
| CheckTrailingReason | trailing_reason: actual raw ordinary26 frame containing complete complex reason followed by byte0; actual deferred decoding selects the entire expected EXIT reason and receiver/controller survive. This exercises the DecodeOne deferred path rather than only generic ETF decoding. |

The complex native payload reason is `{payload_reason,<<0,128,255>>,[a|binary_tail]}`. Native node names, remote PID roles (payload1, monitor11), remote creation1, label and token counters are retained. Local setnode requested creation1 is promoted by the SDK's existing local epoch rule to its active epoch; owned local PID/reference values are used throughout, rather than hardcoding captured native local PID IDs. Monitor regression uses the same malformed native bytes and lifecycle role; it does not compare physical native byte counters with SDK attempt counters.

## Implementation and cleanup

Only real RuntimeNodeFoundationHostActor instances, public runtime monitor/liveness APIs, raw DistributionInputAdapter ingress, actual outgoing buffers and module calls are used. No private state reflection, no new production introspection API, no changed existing expected values. A nested owned Session is allowed inside the checks class; the two namespace-level classes each have their own file.

Receiver/controller mailboxes are actually driven to apply deferred signals. Termination drain may catch cancellation/channel closure, but the following Completion and exact DOWN assertions remain mandatory and bounded. Origin watcher death is observed before the malformed packet. All actors/runtime are owned and disposed; the suspended receiver is resumed and its release gate completed in finally.

Initial candidate source-only review corrected nullable TakeControl generic inference and aligned negotiated remote creation with the actual original remote PID creation. No execution failure or measured result exists for this candidate. Source freeze records final raw LF file bytes; subsequent formatting must create a new source freeze and retain this one.

## Main registration and bounded validation plan

Append only this registration before the actual current Program execution loop, preserving all current entries and allocation/capacity checks:

```csharp
Test(Erlang.Tests.DistributionNativePayloadContractHostNames.Test, Erlang.Tests.DistributionNativePayloadContractHostChecks.Run);
```

Expected full count after adoption is 6570, not an observed result. Run a bounded actual target first, then the runtime/distribution and exact sequential-trace focus with the prior published registrations intact; inspect actual ordered names/counts. Format only these two owned files, freeze formatted bytes, rerun target/focus if semantic repair occurred. Parent owns Main promotion, complete pipeline, English changelog and publication. Native14 contract concordance is selected-case evidence, not whole erlang or whole distribution parity.