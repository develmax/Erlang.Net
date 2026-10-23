# Part453 named-kernel original ETF audit and pre-boot lifecycle note

Passive read-only review of existing sources/receipts and artifact-only standard-library ETF parsing. No VM/native/build/Git/CI execution or product/shared edits. Audit target is exactly `artifacts/library453-kernel-permanent/342f6e1858af4215a63d40271a93e63f`, not a later successful replacement receipt.

## Original file bytes versus complete term agreement

Independently decoded original Resources/KernelBootOriginal453 values.etf/stacks.etf and the requested actual files. All144 complete rows agree with only map member ordering relaxed. Exactly138 rows are strict original-source ETF byte matches;6 rows differ solely in state-map pair order. The six are the same case `state`: run(default), run_stacks(default/0/1/2/8). All24 case IDs, all5 profile labels, list/tuple headers, every non-map leaf byte, original inputs, errors and complete frame fields remain unchanged. No reference bindings, frame stripping or broad reencoding normalization were used.

Original state map encoded key order:

`name, started, name_domain, name_type`

Actual encoded key order:

`name, name_domain, name_type, started`

Each key and value is otherwise byte-identical: name=managed453@localhost, started=static, name_domain=shortnames, name_type=static. The passive checker only allows pairing identical raw map keys and recursively equal complete values; leaf bytes/list and tuple headers must still match. Therefore this is evidence of semantic map equality with distinct actual source serialization, not138/144 compatible terms or144 strict original raw matches.

Both full files remain strict-original-bytes unequal. Original values SHA D1F6FA79713CD47F8DBB706662AE2F64D7D61FAB65E97402F1149D353EA29C28; actual values SHA5E3DFD84F8FC3A8A6EEA45AE2C30DC14B50779156C5CEC8798C1EF4474C9540D. Original stacks SHA C7E7C39BB7BEABD1C85F21F9476E9A527DA07296619F7958072EDCFAD05BCBD9; actual stacks SHA E7D1F7A0567BBCE589C0623960BAE0D931B367B9D9BC2BCF76CEEDF465FAFDA7.

The original target comparison receipt is correctly retained Passed=false, with all144 row comparisons true and Cleanup=true. Independent audit does not overwrite or relabel it successful. Its Runtime SHA is4AC4E537C77DBF71187B44CA18B5D7D1B46AA5FEBD77E8CF9224705801882B00. Root's later receipt should separately report whole typed equality, canonical reencoded row equality and strict original-source file/row counts. A canonical reencoding comparison alone cannot support strict source-byte claims.

## Pre-boot actor lifecycle: facts and plausible mechanism only

Saved build3 records outer test timeout at16.738seconds. Build4 records TaskCanceledException at StartOtpBootAsync line63 waiting for readiness, total64.362seconds; that receipt does not identify a stalled bootstrap function, waiter/request target or actor stack. Current code changes at least two relevant dimensions: worker image is published after boot; a short-lived prim_file:on_load owner is joined before boot instead of leaving a host actor alive. A subsequent successful boot therefore does not isolate which change mattered.

Current KernelBootOriginal453Checks explicitly spawns a temporary preloader, waits its result and actual process Completion, starts original boot, waits readiness, then publishes observer worker and creates RuntimeNodeFoundationHostActor. Current StartOtpBootAsync spawns real original init plus notification observer, and readiness waits on actual init reply/termination/cancellation; it does not simply require there be no other process. Original prim_file:on_load itself spawns a system janitor before load_nif (prim_file.erl117-121); the ephemeral host owner is not that continuing janitor and should not be described as joining all file processes.

A concrete conditional concern is the host actor's wait mechanism: RuntimeNodeFoundationHostActor.cs18 waits on .NET Channel.ReadAllAsync rather than Erlang ReceiveAsync/checkpoints. ProcessRuntime.GarbageCollection.cs24 queues target work through Mailbox.SendPending; synchronous GC request waits for target reply at lines43-45. If boot actually awaited such a request from the idle pre-boot host actor, the target's pending mailbox callback could remain undrained until another actor command. This is a plausible source-level mechanism, NOT a confirmed cause: no saved timeout receipt proves such a request was issued to that actor. Scheduler dispatch itself is continuation-based; merely keeping an async actor alive is not proof of physical scheduler-lane exhaustion. Similarly early source publication can affect boot loading/ownership, but no recorded blocked owner proves that here.

To establish causality later, keep an owned failure with actual pending request tag/recipient, target mailbox and active code/turn state, plus original init/code_server/purger wait stacks; vary persistent actor and observer-publication timing independently. This is a diagnostic follow-up, not required speculative product refactoring before accepting the already measured no-listener scope. Do not erase the two failed preparations or claim the lifecycle cause fixed from successful ordering alone.

## Boundaries

This is named original kernel boot with no distribution listener/EPMD and24 finite observer cases, not full TCP boot, distribution autoconnect, arbitrary applications or whole-module promotion. Newer build6 named test receipt currently records1/1 pass and a normal settled process exit; this audit does not recompute its unspecified output directory or preclaim any broader regression/CI gate.

Machine evidence: `kernel-map-order-audit.json`, SHA256 FB4016865D2FCB369584684D59D04EE2EF0E16674EFBC4D22BD4D4B20FDD6DD6.
