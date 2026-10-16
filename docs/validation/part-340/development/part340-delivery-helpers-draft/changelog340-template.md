## Part 340 — Connected proc_lib spawning and RPC dependencies

### Delivered behavior

- Register the six node-qualified proc_lib spawn/spawn_link/spawn_opt overloads over real connected distribution. Preserve initialization MFA, ancestry, group leader and link/monitor/alias lifecycle. Reuse owned output, cancellation and generation fencing.
- Add rpc:call/4,5 and erpc:call/4,5,execute_call/4 for real remote process-label queries. Preserve local caller execution for default infinite calls, exception mapping and queued-result/timeout resolution.
- Correct nodes/0,1,2 to query actual visible/hidden generations and connection metadata. Preserve proc_lib visibility checks. Correct shared apply/3 traversal to the pinned1023 argument register boundary and native improper-tail precedence; apply/2 remains a separate gap.

### Validation and scope

Actual completed local validation: **{{PASSED}} passed, zero failed, eleven integrations**. Registry:587 APIs across14 implemented modules; proc_lib39/39 registered exports. These figures describe tested API presence, not complete semantic or performance compatibility. The actual full guard is {{FULL_GUARD_SHA256}} and binds the frozen source/resource/package inputs.

All7105 previous ordered tests and576 registry rows/custom Limits are preserved. New portable source assertions, direct MFA effects and connected HOST controls are bound by the actual receipts; retain earlier nullable-build, diagnostic external-fun, channel-only test actor and DEMONITOR/EXIT-order failures as history.

Published338 original6216/0 plus fd8 preserves all6171 prior contracts and45 new lifecycle/limiter cases. Keep the predeclared map stdout-order strict refusal separate. Prior339 source-built original controller observations remain acquisition evidence until their explicit semantic comparison passes. Current340 original comparison awaits publication and exact-head execution.

Physical BEAM heap/stack values remain explicitly unavailable in the private crash bridge. Full logger background/overload/configuration/formatting, creator tuning, RPC/erpc export completeness, forward protocol options, physical TCP/VM scheduling and performance parity remain partial. Keep proc_lib status partially compatible despite39/39 API presence.
