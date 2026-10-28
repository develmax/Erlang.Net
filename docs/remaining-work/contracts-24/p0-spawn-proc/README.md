# P0 spawn/proc_lib source matrix (Part587)

Source-only extension of the parent's four-MFA/83-row inspection/system matrix. No code was changed, no build/CLR/Erlang/native VM/CI was executed, no git mutation or status promotion occurred. Source snapshots come from read-only git show c7fb01a and the exact OTP29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. Source-inputs.json binds every byte. The working feature tree can move without silently changing this audit base.

## Set accounting

These nineteen are eight erlang spawn_opt/2–5 and spawn_request/2–5 plus eleven proc_lib APIs: spawn_opt/2–5, start/start_link/start_monitor/5, init_p/3,5, hibernate/3, wake_up/3. Together with the four inspection/system MFAs and separate load_nif/2 they account for all24 Known contracts. The older batch585 shared-resource nineteen is a different intentional cohort: twelve erlang inspection/system/spawn plus seven proc startup; it did not accidentally omit lifecycle functions. Do not combine the two counts as if they were disjoint.

177 named source partitions are specifications, not executed tests and not a percentage of full compatibility. mfa-matrix.json links every target to applicable groups and exact source shapes. Conditional/inapplicable combinations require an explicit guard reason; they are never successes. Source clause heads include all relevant arities for context and are labeled accordingly. Actual invocation arity remains the row's MFA.

## Key source constraints

- OTP is_list guards accept nil or cons, including improper cons. Do not move proper-list validation into wrapper guards. Native spawn parser distinguishes badopt elements from badarg improper option tails; the tail result overrides earlier invalid elements. Earlier invalid elements stay sticky despite later valid duplicate settings.
- Defaults are actual VM controls at child creation, not the parent process's current flag state and not static CLR defaults. Heap rounding is the source class table. fullsweep_after source assignment is to Uint16; acquire wrap-boundary observations instead of assuming clamp.
- Remote spawn_opt noconnection/notsup branches invoke the actual local erts_internal:crasher fallback; a useless/local pid plus subsequent link/monitor fate is not interchangeable with a direct parent exception. Other error replies and fallback option failures have distinct source helper paths.
- Native spawn_opt/4 has no fabricated erlang.erl location. Source wrappers and request overloads have different inherited/info/cause paths. Use actual admitted original metadata branch provenance; never insert the observed line numbers into results.
- Atom monitor resets accumulated tuple monitor settings; tuple and atom link order must follow their actual source handling, not generic last-wins folklore. Async reply controls apply only to spawn_request; a reply/no absence requires a real causal child completion/barrier, not a timeout declared successful.
- Proc_lib startup forbids monitor options before adding its own monitor. lists:member/keyfind outcomes are part of source behavior (including arity3 tuple case_clause and improper options errors), and need full stacks. start and start_link consume monitor messages; start_monitor requeues real DOWN on failure. Ack/nack, timeout, linked EXIT and physical child death are distinct obligations.
- init_p stores ancestor/initial-call metadata before invoking the callback; fun_info failure lies outside the callback catch. hibernate destroys the old continuation; run it only in an owned child, capture real asleep/wake/down/physical-root transitions, and preserve all logger/report values.

## Reuse and observed evidence boundary

Baseline docs/validation/part-520/proof.json and README are snapshotted. Their successful finite evidence is315 named-target malformed options (15 wrappers x21 cases) using original proc_lib above our native runtime, in ordinary/private profiles. The earlier original330 and default15 outputs were explicitly not compatibility successes. Legacy C# proc_lib failed comparisons stay failed and separate. This packet does not rerun or retroactively promote those receipts.

Reuse source fixtures spawn_resources_core541, spawn_resources540, spawn_resources_default541 and contract_proclib509/contract_proc_subject509. The Part515 remote proposal already provides genuine target-local relay and real peer worker; it is source-only and supplies no remote passed evidence. Exact source/proposal bindings are in reused-artifacts.json. Do not create another collector or substitute a mock peer/native callback.

## Execution order and acceptance

1. Freeze a final implementation plus exact original owner images; retain the existing malformed315 block once for that final source, without recompiling/rerunning it after documentation-only edits.
2. Batch all valid resource policy and actual defaults partitions under the same admitted provider, with real child barriers for before/after snapshots. Implement missing heap/GC/kill/log/shared-binary domains before calling these API contracts complete; baseline Analyze validates some malformed input but Parse deliberately refuses every otherwise valid resource-policy option with NotSupportedException.
3. Run overloaded local wrappers and async request reply/link/monitor protocols. Observe complete values, all stack frames/metadata and real child cleanup. Aggregate independent failures; dependent steps after unsuccessful preparation remain gated.
4. Execute original proc_lib startup and lifecycle/logger/hibernate partitions using the same genuine compiled worker convention. Compare ordinary/private. Run the legacy managed backend as a separate owner lane so original BEAM success cannot conceal its frame metadata differences.
5. Reuse authenticated real net_kernel peers in both orientations: managed caller→OTP target and OTP caller→managed target. A connected successful sender alone does not prove receiver option enforcement. Include disconnect/reincarnation, no-peer and capability error partitions; no not_executed branch may close a contract.
6. Finish with source-bound whole raw/deterministic ETF and explicit typed identity/causal-law comparison that retains every original value/frame. Preserve original, ordinary and private records independently; never normalize the ETF or promote a record from partial/value-only coverage. Physical heap/counter/logger leaves require actual ownership/source domains, not a fabricated constant or CLR estimate.

All nineteen remain Known here. P1 atomics shutdown can publish separately; this matrix neither claims it solved producer/CLR-finalizer authority nor delays that scoped fix.
